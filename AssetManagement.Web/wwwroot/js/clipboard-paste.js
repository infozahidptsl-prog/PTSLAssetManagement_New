window.clipboardPasteHelper = {
    registeredDotNet: null,
    
    initPaste: function(dotNetRef) {
        window.clipboardPasteHelper.registeredDotNet = dotNetRef;
        document.removeEventListener('paste', window.clipboardPasteHelper.onPaste);
        document.addEventListener('paste', window.clipboardPasteHelper.onPaste);
    },

    disposePaste: function() {
        document.removeEventListener('paste', window.clipboardPasteHelper.onPaste);
        window.clipboardPasteHelper.registeredDotNet = null;
    },

    onPaste: function(e) {
        if (!window.clipboardPasteHelper.registeredDotNet) return;
        
        // If pasting inside a text input or textarea, let the default text paste happen unless it contains an image file
        var target = e.target;
        var isTextInput = target && (target.tagName === 'INPUT' && (target.type === 'text' || target.type === 'number' || target.type === 'date') || target.tagName === 'TEXTAREA');

        if (e.clipboardData && e.clipboardData.items) {
            var items = e.clipboardData.items;
            for (var i = 0; i < items.length; i++) {
                if (items[i].type.indexOf('image') !== -1) {
                    e.preventDefault();
                    var blob = items[i].getAsFile();
                    if (blob) {
                        var reader = new FileReader();
                        reader.onload = function(evt) {
                            var base64 = evt.target.result.split(',')[1];
                            var filename = "clipboard_" + new Date().getTime() + ".png";
                            window.clipboardPasteHelper.registeredDotNet.invokeMethodAsync('OnClipboardImagePasted', filename, blob.type, base64);
                        };
                        reader.readAsDataURL(blob);
                    }
                    return;
                }
            }
        }
    }
};
