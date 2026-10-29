# Torsepan iOS

Native SwiftUI/WKWebView client for the existing HTTPS panel. Requires iOS/iPadOS
16.4+. Uses persistent WebKit cookies/storage, back gestures, pull to refresh,
system photo/file selection, and a share sheet for downloads (including PDF/blob
links). Only the exact panel HTTPS origin stays inside the app; external HTTPS,
mail and phone links open via iOS after a user tap. TLS trust is never overridden.

This initial client is online-only. Android's offline database/synchronization
and FCM notifications are not ported. Native background notifications require a
separate APNs integration and entitlement. Browser Web Push is not native iOS
push. Do not advertise notification parity with Android for this release.

## Build and test

On a Mac with Xcode and XcodeGen:

```sh
brew install xcodegen
cd ios
xcodegen generate
open Torsepan.xcodeproj
```

Select the Torsepan scheme and run tests on an iPhone simulator. GitHub Actions
also runs policy tests and compiles for devices without signing. That build is
validation only: it is NOT an IPA download or an installable iPhone release.
Generated Xcode projects and build directories are intentionally not committed.

## Signed release (required before activating download)

1. Use the owner's Apple Developer Program team in Xcode Signing & Capabilities.
   Confirm/register `ir.torsepan.mobile`, or change it to the owner's registered
   identifier in project.yml and regenerate. No signing credentials belong here.
2. Create an App Store Connect record, supply the required privacy information,
   support/privacy URLs and review login. Review the app icon and test iPhone and
   iPad on real devices: login persistence, navigation, photo uploads, report and
   backup exports, sign-out, reconnect and layout. TestFlight/App Store approval
   is external and is not guaranteed by successful compilation.
3. Increment CURRENT_PROJECT_VERSION for each upload. Select a generic iOS
   device, Product > Archive, then Distribute App > App Store Connect. Validate
   and upload the signed archive from Xcode using the owner's team.
4. In App Store Connect, configure TestFlight testers; for a public invitation,
   complete external beta review and enable the public group invitation link.
   Alternatively publish the reviewed App Store release.
5. Only after confirming that users can install the actual release, set the
   panel's `AppDownloads__IosInstallUrl` environment setting to the real
   `https://testflight.apple.com/join/...` or `https://apps.apple.com/.../id...`
   URL. The same key `AppDownloads:IosInstallUrl` is available in appsettings.json
   for a reviewed source-based release. Empty/invalid URLs keep the card pending.
   Local offline Android configuration intentionally stays pending.
6. Liara's existing GitHub integration remains the deployment owner. Verify the
   live `/app-downloads` card and install on an iPhone before announcing release.

Never put an unsigned `.ipa`, simulator build, fake invitation or placeholder
URL on the download button. TestFlight invitations are beta distribution, not a
permanent App Store release. No certificate/profile or Apple account was
available or added when this source was prepared.
