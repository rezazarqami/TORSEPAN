import UIKit
import WebKit

final class PortalController: UIViewController, WKNavigationDelegate, WKUIDelegate, WKDownloadDelegate {
    private var browser: WKWebView!
    private let progress = UIProgressView(progressViewStyle: .default)
    private var observation: NSKeyValueObservation?
    private var downloadedFiles: [ObjectIdentifier: URL] = [:]
    private var showingError = false

    override func viewDidLoad() {
        super.viewDidLoad()
        view.backgroundColor = UIColor(red: 0.02, green: 0.12, blue: 0.26, alpha: 1)
        let configuration = WKWebViewConfiguration()
        configuration.websiteDataStore = .default()
        browser = WKWebView(frame: .zero, configuration: configuration)
        browser.navigationDelegate = self
        browser.uiDelegate = self
        browser.allowsBackForwardNavigationGestures = true
        let refresh = UIRefreshControl()
        refresh.addTarget(self, action: #selector(reload), for: .valueChanged)
        browser.scrollView.refreshControl = refresh
        browser.translatesAutoresizingMaskIntoConstraints = false
        progress.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(browser)
        view.addSubview(progress)
        NSLayoutConstraint.activate([
            browser.topAnchor.constraint(equalTo: view.safeAreaLayoutGuide.topAnchor),
            browser.bottomAnchor.constraint(equalTo: view.safeAreaLayoutGuide.bottomAnchor),
            browser.leadingAnchor.constraint(equalTo: view.leadingAnchor),
            browser.trailingAnchor.constraint(equalTo: view.trailingAnchor),
            progress.topAnchor.constraint(equalTo: browser.topAnchor),
            progress.leadingAnchor.constraint(equalTo: view.leadingAnchor),
            progress.trailingAnchor.constraint(equalTo: view.trailingAnchor)
        ])
        observation = browser.observe(\.estimatedProgress, options: [.new]) { [weak self] web, _ in
            self?.progress.progress = Float(web.estimatedProgress)
            self?.progress.isHidden = web.estimatedProgress >= 1
        }
        browser.load(URLRequest(url: NavigationPolicy.home))
    }

    @objc private func reload() {
        if browser.url == nil { browser.load(URLRequest(url: NavigationPolicy.home)) }
        else { browser.reload() }
    }

    func webView(_ webView: WKWebView, decidePolicyFor action: WKNavigationAction,
                 decisionHandler: @escaping (WKNavigationActionPolicy) -> Void) {
        guard let url = action.request.url else { decisionHandler(.cancel); return }
        if NavigationPolicy.isPortal(url) {
            if action.targetFrame == nil {
                webView.load(action.request)
                decisionHandler(.cancel)
            } else { decisionHandler(action.shouldPerformDownload ? .download : .allow) }
        } else if url.scheme == "blob", let current = webView.url, NavigationPolicy.isPortal(current) {
            decisionHandler(action.shouldPerformDownload ? .download : .allow)
        } else {
            decisionHandler(.cancel)
            if action.navigationType == .linkActivated && NavigationPolicy.mayOpenExternally(url) {
                UIApplication.shared.open(url)
            }
        }
    }

    func webView(_ webView: WKWebView, decidePolicyFor response: WKNavigationResponse,
                 decisionHandler: @escaping (WKNavigationResponsePolicy) -> Void) {
        let attachment = (response.response as? HTTPURLResponse)?
            .value(forHTTPHeaderField: "Content-Disposition")?.lowercased().contains("attachment") == true
        decisionHandler(!response.canShowMIMEType || attachment ? .download : .allow)
    }

    func webView(_ webView: WKWebView, navigationAction: WKNavigationAction, didBecome download: WKDownload) {
        download.delegate = self
    }
    func webView(_ webView: WKWebView, navigationResponse: WKNavigationResponse, didBecome download: WKDownload) {
        download.delegate = self
    }
    func download(_ download: WKDownload, decideDestinationUsing response: URLResponse,
                  suggestedFilename: String, completionHandler: @escaping (URL?) -> Void) {
        do {
            let folder = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
            try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
            let filename = (suggestedFilename as NSString).lastPathComponent
            let url = folder.appendingPathComponent(filename.isEmpty ? "Torsepan-file" : filename)
            downloadedFiles[ObjectIdentifier(download)] = url
            completionHandler(url)
        } catch { completionHandler(nil); showError() }
    }
    func downloadDidFinish(_ download: WKDownload) {
        guard let url = downloadedFiles.removeValue(forKey: ObjectIdentifier(download)) else { return }
        let share = UIActivityViewController(activityItems: [url], applicationActivities: nil)
        share.popoverPresentationController?.sourceView = view
        share.popoverPresentationController?.sourceRect = CGRect(x: view.bounds.midX, y: view.bounds.midY, width: 1, height: 1)
        share.completionWithItemsHandler = { _, _, _, _ in
            try? FileManager.default.removeItem(at: url.deletingLastPathComponent())
        }
        present(share, animated: true)
    }
    func download(_ download: WKDownload, didFailWithError error: Error, resumeData: Data?) {
        if let url = downloadedFiles.removeValue(forKey: ObjectIdentifier(download)) {
            try? FileManager.default.removeItem(at: url.deletingLastPathComponent())
        }
        showError()
    }
    func webView(_ webView: WKWebView, didFinish navigation: WKNavigation!) {
        webView.scrollView.refreshControl?.endRefreshing()
    }
    func webView(_ webView: WKWebView, didFailProvisionalNavigation navigation: WKNavigation!, withError error: Error) {
        navigationFailed(error)
    }
    func webView(_ webView: WKWebView, didFail navigation: WKNavigation!, withError error: Error) {
        navigationFailed(error)
    }
    private func navigationFailed(_ error: Error) {
        browser.scrollView.refreshControl?.endRefreshing()
        if (error as NSError).code != NSURLErrorCancelled { showError() }
    }
    private func showError() {
        guard !showingError else { return }
        showingError = true
        let alert = UIAlertController(title: "اتصال برقرار نشد", message: "اینترنت را بررسی کنید و دوباره تلاش کنید.", preferredStyle: .alert)
        alert.addAction(UIAlertAction(title: "تلاش دوباره", style: .default) { [weak self] _ in
            self?.showingError = false
            self?.reload()
        })
        alert.addAction(UIAlertAction(title: "بستن", style: .cancel) { [weak self] _ in self?.showingError = false })
        present(alert, animated: true)
    }
}
