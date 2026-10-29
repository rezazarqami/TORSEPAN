using TORSEPAN.Panel.Models;

foreach (var invalid in new string?[] { null, "", "https://example.com/a.ipa",
    "javascript:alert(1)", "http://testflight.apple.com/join/abc123",
    "https://testflight.apple.com.evil.test/join/abc123", "https://testflight.apple.com/",
    "https://user@testflight.apple.com/join/abc123", "https://testflight.apple.com:8443/join/abc123",
    "https://apps.apple.com/app/example", "https://testflight.apple.com/join/abc#x" })
    if (IosDistributionLink.Validate(invalid) is not null) throw new Exception($"Accepted invalid release link: {invalid}");
foreach (var valid in new[] { "https://testflight.apple.com/join/abc123", "https://apps.apple.com/us/app/torsepan/id123456789" })
    if (IosDistributionLink.Validate(valid) is null) throw new Exception($"Rejected release link: {valid}");
Console.WriteLine("iOS distribution links passed.");
