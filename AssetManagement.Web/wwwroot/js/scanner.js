let html5QrCode = null;
let isScannerRunning = false;

window.startScanner = async function (elementId, dotNetHelper) {
    // If there is an existing instance, try to stop and clear it
    if (html5QrCode) {
        if (isScannerRunning) {
            try {
                await html5QrCode.stop();
            } catch (err) {
                console.warn("Could not stop existing scanner:", err);
            }
            isScannerRunning = false;
        }
        try {
            html5QrCode.clear();
        } catch (err) { }
        html5QrCode = null;
    }

    try {
        html5QrCode = new Html5Qrcode(elementId);
        await html5QrCode.start(
            { facingMode: "environment" },
            {
                fps: 10,
                qrbox: { width: 250, height: 250 }
            },
            (decodedText, decodedResult) => {
                if (html5QrCode && isScannerRunning) {
                    try {
                        html5QrCode.pause(true);
                    } catch (e) { }
                }
                dotNetHelper.invokeMethodAsync('OnScanResult', decodedText);
            },
            (errorMessage) => {
                // ignore continuous frame parse errors
            }
        );
        isScannerRunning = true;
    } catch (err) {
        isScannerRunning = false;
        if (html5QrCode) {
            try { html5QrCode.clear(); } catch (e) { }
            html5QrCode = null;
        }
        console.error("Failed to start scanner:", err);
        dotNetHelper.invokeMethodAsync('OnScanError', err.toString());
    }
};

window.stopScanner = async function () {
    if (html5QrCode) {
        if (isScannerRunning) {
            try {
                await html5QrCode.stop();
            } catch (err) {
                console.warn("Failed to stop scanner:", err);
            }
            isScannerRunning = false;
        }
        try {
            html5QrCode.clear();
        } catch (e) { }
        html5QrCode = null;
    }
};

window.resumeScanner = function () {
    if (html5QrCode && isScannerRunning) {
        try {
            html5QrCode.resume();
        } catch (e) { }
    }
};
