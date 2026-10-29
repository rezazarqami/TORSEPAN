import XCTest
@testable import Torsepan

final class NavigationPolicyTests: XCTestCase {
    func testOnlyHTTPSPortalIsTrusted() {
        for address in ["https://torsepan.liara.run/", "https://torsepan.liara.run/messages?x=1"] {
            XCTAssertTrue(NavigationPolicy.isPortal(URL(string: address)!))
        }
        for address in ["http://torsepan.liara.run/", "https://torsepan.liara.run.evil.test/",
                        "https://torsepan.liara.run:8443/", "https://user@torsepan.liara.run/",
                        "file:///tmp/page.html", "javascript:alert(1)"] {
            XCTAssertFalse(NavigationPolicy.isPortal(URL(string: address)!))
        }
    }
    func testExternalSchemesAreRestricted() {
        XCTAssertTrue(NavigationPolicy.mayOpenExternally(URL(string: "https://testflight.apple.com/join/example")!))
        XCTAssertFalse(NavigationPolicy.mayOpenExternally(URL(string: "javascript:alert(1)")!))
        XCTAssertFalse(NavigationPolicy.mayOpenExternally(URL(string: "file:///tmp/a")!))
    }
}
