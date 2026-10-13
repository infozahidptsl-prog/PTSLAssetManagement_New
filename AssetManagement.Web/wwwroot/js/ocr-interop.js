window.extractTextFromInput = async function(inputId) {
    const el = document.getElementById(inputId);
    if (!el || !el.files || el.files.length === 0) return null;
    const file = el.files[0];
    try {
        if (typeof Tesseract === 'undefined') {
            console.error("Tesseract.js is not loaded.");
            return null;
        }
        console.log("Starting Tesseract OCR...");
        const worker = await Tesseract.createWorker('eng');
        const ret = await worker.recognize(file);
        await worker.terminate();
        console.log("OCR completed.");
        return ret.data.text;
    } catch (err) {
        console.error("OCR Error:", err);
        return null;
    }
};

window.extractTextFromClipboard = async function() {
    try {
        const items = await navigator.clipboard.read();
        for (const item of items) {
            const imageTypes = item.types.filter(type => type.startsWith('image/'));
            if (imageTypes.length > 0) {
                const blob = await item.getType(imageTypes[0]);
                if (typeof Tesseract === 'undefined') return null;
                const worker = await Tesseract.createWorker('eng');
                const ret = await worker.recognize(blob);
                await worker.terminate();
                return ret.data.text;
            }
            const textTypes = item.types.filter(type => type === 'text/plain');
            if (textTypes.length > 0) {
                const blob = await item.getType('text/plain');
                return await blob.text();
            }
        }
    } catch (err) {
        console.error('Clipboard Error:', err);
    }
    return null;
};


window.extractTextFromDataUrl = async function(dataUrl) {
    try {
        if (typeof Tesseract === 'undefined') {
            console.error('Tesseract.js is not loaded.');
            return null;
        }
        console.log('Starting Tesseract OCR from Data URL...');
        const worker = await Tesseract.createWorker('eng');
        const ret = await worker.recognize(dataUrl);
        await worker.terminate();
        console.log('OCR completed.');
        return ret.data.text;
    } catch (err) {
        console.error('OCR Error:', err);
        return null;
    }
};

