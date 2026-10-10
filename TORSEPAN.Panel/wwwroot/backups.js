window.torsepanBackups = {
    download(url) {
        // A same-origin attachment URL works with browser downloads and the APK's DownloadManager.
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.rel = 'noreferrer';
        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();
    }
};
