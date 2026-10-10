using System.Reflection;
using TORSEPAN.API.Controllers;
using TORSEPAN.Domain.Entities;
var validate=typeof(HandpanPhotosController).GetMethod("IsImage",BindingFlags.Static|BindingFlags.NonPublic)!;
bool Valid(byte[] data,string type)=>(bool)validate.Invoke(null,[data,(long)data.Length,type])!;
void Check(bool result,string name){if(!result)throw new Exception(name);Console.WriteLine("PASS "+name);}
byte[] jpeg=[0xff,0xd8,0xff,0xe0,1,2,0xff,0xd9];
var webp=System.Text.Encoding.ASCII.GetBytes("RIFFabcdWEBPdata");
Check(Valid(jpeg,"image/jpeg")&&Valid(webp,"image/webp"),"API accepts signatures for JPEG fallback and existing WebP");
Check(!Valid(jpeg,"image/webp")&&!Valid(webp,"image/jpeg"),"API rejects mismatched declared image types");
Check(!Valid([],"image/jpeg")&&!Valid([0xff,0xd8,0xff],"image/jpeg")&&!Valid(jpeg[..^1],"image/jpeg"),"API rejects empty or truncated images");
Check(!Valid(webp,"text/html"),"API does not accept unrelated content types");
var photo=new HandpanPhoto(Guid.NewGuid(),jpeg,jpeg,"image/jpeg",Guid.NewGuid());
Check(photo.ContentType=="image/jpeg", "fallback photo retains its actual MIME type for later display");
