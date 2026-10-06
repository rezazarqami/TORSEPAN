using TORSEPAN.Application.Auth.Commands.RefreshLogin;
using TORSEPAN.Infrastructure.Persistence.Repositories;
using System.Reflection;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TORSEPAN.API.Controllers;
using TORSEPAN.API.Security;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Infrastructure.Authentication;
using TORSEPAN.Infrastructure.Migrations;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Panel.Models;

internal static class ApiSmoke
{
 public static async Task RunAsync()
 {
  var connection=Environment.GetEnvironmentVariable("PROFILE_TEST_DB")??throw new Exception("Isolated database required.");
  var options=new DbContextOptionsBuilder<TORSEPANDbContext>().UseNpgsql(connection).Options;
  await using var db=new TORSEPANDbContext(options);await db.Database.EnsureCreatedAsync();
  var me=new User("profile-me","عضو اول");var other=new User("profile-other","عضو دوم");me.SetPassword("original-password");other.SetPassword("other-password");me.ChangeTitle("تیونر");db.Users.AddRange(me,other);await db.SaveChangesAsync();
  // Execute the actual additive migration over the previous Users schema with existing accounts.
  await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Users\" DROP COLUMN \"CredentialVersion\", DROP COLUMN \"AvatarPng\", DROP COLUMN \"AvatarVersion\"");
  await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"RefreshTokens\" DROP COLUMN \"CredentialVersion\"");
  var migration=new AddPersonalProfile();var builder=new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");typeof(AddPersonalProfile).GetMethod("Up",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(migration,[builder]);
  var generator=db.GetService<IMigrationsSqlGenerator>();foreach(var command in generator.Generate(builder.Operations,db.Model))await db.Database.ExecuteSqlRawAsync(command.CommandText);
  db.ChangeTracker.Clear();me=await db.Users.SingleAsync(x=>x.Id==me.Id);other=await db.Users.SingleAsync(x=>x.Id==other.Id);
  Check(me.CredentialVersion==0&&me.VerifyPassword("original-password")&&me.Title=="تیونر","migration preserves existing accounts and defaults old-token version to zero");
  typeof(User).GetProperty(nameof(User.PasswordHash))!.SetValue(me,"legacy-password");await db.SaveChangesAsync();
  Check(me.VerifyPassword("legacy-password")&&!me.VerifyPassword("wrong-password"),"legacy passwords remain usable without resetting accounts");
  MyProfileController As(User user,TORSEPANDbContext? context=null)=>new(context??db){ControllerContext=new(){HttpContext=new DefaultHttpContext{User=Principal(user.Id)}}};
  var profile=As(me);var otherVersion=other.CredentialVersion;
  Check(Value<OwnProfileDto>(await profile.Get(default)).Id==me.Id,"self profile is selected exclusively from the authenticated ID");
  Check(await profile.Credentials(new("profile-new","wrong",null,null),default) is BadRequestObjectResult&&me.UserName=="profile-me","incorrect current password cannot change account credentials");
  Check(await profile.Credentials(new("PROFILE-OTHER","legacy-password",null,null),default) is ConflictObjectResult,"case-insensitive duplicate username is rejected");
  Check(await profile.Credentials(new("profile-new","legacy-password","short","short"),default) is BadRequestObjectResult,"short new password is rejected");
  Check(await profile.Credentials(new("profile-new","legacy-password","new-password-123","different"),default) is BadRequestObjectResult,"password confirmation is validated on the server");
  var png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAGUlEQVR4nGNQ6ij/TwlmGDVg1IBRA4aLAQABwCAfTqQSnwAAAABJRU5ErkJggg==");var tagged=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAGXRFWHRMb2NhdGlvbgBQcml2YXRlIGxvY2F0aW9ugHrhrQAAABlJREFUeJxjUOoo/08JZhg1YNSAUQOGiwEAAcAgH06kEp8AAAAASUVORK5CYII=");
  Check(ProfilePng.Sanitize(tagged).SequenceEqual(png),"PNG metadata is removed while retaining valid pixels");
  Check(await profile.Avatar(new(Convert.ToBase64String(png)),default) is OkObjectResult&&me.AvatarVersion.HasValue,"validated own avatar is stored in the database");
  var image=await As(other).Image(me.Id,default) as FileContentResult;
  Check(image?.ContentType=="image/png"&&image.FileContents.SequenceEqual(png),"authenticated workshop member can retrieve a profile image");
  var roster=new NotificationsController(db){ControllerContext=As(other).ControllerContext};
  Check(Value<ConversationListDto>(await roster.Conversations(default)).Items.Single(x=>x.Id==me.Id).AvatarVersion==me.AvatarVersion,"chat roster exposes the current profile image revision");
  Check(await profile.Avatar(new(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("<svg onload='evil()'>"))),default) is BadRequestObjectResult,"SVG and spoofed avatar files are rejected");
  var corrupt=png.ToArray();corrupt[35]^=1;Check(await profile.Avatar(new(Convert.ToBase64String(corrupt)),default) is BadRequestObjectResult,"corrupt PNG chunks are rejected");
  var invalidPixels=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAC0lEQVR4nEtKTAEAAk8BKKJT8QkAAAAASUVORK5CYII=");Check(await profile.Avatar(new(Convert.ToBase64String(invalidPixels)),default) is BadRequestObjectResult,"valid headers without a decodable pixel stream are rejected");
  Check(await profile.Avatar(new(new string('A',180000)),default) is BadRequestObjectResult,"oversized image input is rejected before decoding");
  Check(await profile.Avatar(new(null),default) is OkObjectResult&&await profile.Image(me.Id,default) is NotFoundResult,"removing an avatar falls back to initials");
  var token=new RefreshToken(me.Id,"old-device",DateTime.UtcNow.AddDays(1));var otherToken=new RefreshToken(other.Id,"other-device",DateTime.UtcNow.AddDays(1));db.RefreshTokens.AddRange(token,otherToken);await db.SaveChangesAsync();
  Check(await profile.Credentials(new("profile-new","legacy-password","new-password-123","new-password-123"),default) is OkObjectResult,"ordinary members can change their own username and password together");
  Check(me.VerifyPassword("new-password-123")&&!me.VerifyPassword("legacy-password")&&!me.PasswordHash.Contains("new-password-123"),"new passwords are salted hashes and old passwords no longer work");
  Check(token.Revoked&&!otherToken.Revoked&&other.CredentialVersion==otherVersion&&other.UserName=="profile-other","account change revokes only the owner's refresh sessions and preserves other accounts");
  Check(await TokenAllowed(db,me.Id,0)==false&&await TokenAllowed(db,me.Id,me.CredentialVersion),"old access tokens are invalidated and newly issued versions are accepted");
  Check(!await TokenAllowed(db,me.Id,null)&&await TokenAllowed(db,other.Id,null),"legacy tokens are accepted only for unchanged accounts");
  var jwt=new JwtService(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Jwt:Key",new string('k',64)},{"Jwt:Issuer","profile-fixture"},{"Jwt:Audience","profile-fixture"}}).Build());
  Check(new JwtSecurityTokenHandler().ReadJwtToken(jwt.GenerateAccessToken(me.Id,me.UserName,me.FullName,me.Title,["Tuner"],me.CredentialVersion)).Claims.Single(x=>x.Type=="credential_version").Value==me.CredentialVersion.ToString(),"JWT issuance includes the current account credential version");
  var refreshRepo=new RefreshTokenRepository(db);var userRepo=new UserRepository(db);
  var unit=new UnitOfWork(context:db,users:userRepo,roles:null!,userRoles:null!,refreshTokens:refreshRepo,handpans:null!,handpanAssemblies:null!,bowls:null!,materials:null!,scales:null!,productionEvents:null!);
  var refreshHandler=new RefreshLoginCommandHandler(refreshRepo,userRepo,jwt,unit);
  db.RefreshTokens.Add(new RefreshToken(me.Id,"raced-old-session",DateTime.UtcNow.AddDays(1),0));await db.SaveChangesAsync();
  var rejected=false;try{await refreshHandler.Handle(new RefreshLoginCommand("raced-old-session"),default);}catch(UnauthorizedAccessException){rejected=true;}
  Check(rejected,"a refresh token inserted by an old concurrent session cannot bypass credential rotation");
  db.RefreshTokens.Add(new RefreshToken(me.Id,"current-version-session",DateTime.UtcNow.AddDays(1),me.CredentialVersion));await db.SaveChangesAsync();
  var renewed=await refreshHandler.Handle(new RefreshLoginCommand("current-version-session"),default);
  Check(new JwtSecurityTokenHandler().ReadJwtToken(renewed.AccessToken).Claims.Single(x=>x.Type=="credential_version").Value==me.CredentialVersion.ToString(),"valid refresh renews the current credential version");
  var hash=me.PasswordHash;Check(await profile.Credentials(new(me.UserName,"new-password-123",null,null),default) is OkObjectResult&&me.PasswordHash==hash,"no-op save does not rotate sessions or passwords");
  Check(await profile.Credentials(new("profile-final","new-password-123",null,null),default) is OkObjectResult&&me.VerifyPassword("new-password-123"),"username-only change preserves the actual password");
  await using var parallel=new TORSEPANDbContext(options);var stale=await parallel.Users.SingleAsync(x=>x.Id==me.Id);
  await profile.Credentials(new(me.UserName,"new-password-123","last-password-123","last-password-123"),default);
  Check(await As(stale,parallel).Credentials(new("racing-edit","new-password-123",null,null),default) is ConflictObjectResult,"concurrent credential edits cannot overwrite newer account credentials");
  Check(typeof(MyProfileController).GetCustomAttribute<AuthorizeAttribute>() is not null&&typeof(MyProfileController).GetMethod("Credentials")!.GetCustomAttribute<EnableRateLimitingAttribute>() is not null,"profile requires authentication and credential changes are rate-limited");
 }
 private static ClaimsPrincipal Principal(Guid id,int? version=null)=>new(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,id.ToString())}.Concat(version.HasValue?[new Claim("credential_version",version.ToString()!)]:[]),"fixture"));
 private static async Task<bool> TokenAllowed(TORSEPANDbContext db,Guid id,int? version)
 {
  var services=new ServiceCollection().AddSingleton(db).BuildServiceProvider();var http=new DefaultHttpContext{RequestServices=services};
  var context=new TokenValidatedContext(http,new AuthenticationScheme("Bearer",null,typeof(JwtBearerHandler)),new JwtBearerOptions()){Principal=Principal(id,version)};
  await AccountTokenValidator.ValidateAsync(context);return context.Result?.Failure is null;
 }
 private static T Value<T>(IActionResult result)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(((OkObjectResult)result).Value),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
 private static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
}
