window.imageCropperInterop = {
    cropperInstance: null,

    initCropper: function (imageElementId, aspectRatio) {
        var image = document.getElementById(imageElementId);
        if (!image) {
            console.error('Image element not found:', imageElementId);
            return false;
        }

        if (typeof Cropper === 'undefined') {
            console.error('Cropper library is not loaded on the window object.');
            return false;
        }

        if (this.cropperInstance) {
            try {
                this.cropperInstance.destroy();
            } catch (e) {
                console.warn('Error destroying existing cropper:', e);
            }
            this.cropperInstance = null;
        }

        var ratio = aspectRatio > 0 ? aspectRatio : (238 / 240); // Default 0.992 (profile card ratio)

        var self = this;
        function createCropper() {
            if (self.cropperInstance) {
                try {
                    self.cropperInstance.destroy();
                } catch (e) { }
                self.cropperInstance = null;
            }

            try {
                self.cropperInstance = new Cropper(image, {
                    aspectRatio: ratio,
                    viewMode: 1,
                    dragMode: 'move',
                    autoCropArea: 0.92,
                    responsive: true,
                    restore: false,
                    checkCrossOrigin: false,
                    guides: true,
                    center: true,
                    highlight: false,
                    cropBoxMovable: true,
                    cropBoxResizable: true,
                    toggleDragModeOnDblclick: false,
                    background: true
                });
            } catch (err) {
                console.error('Error instantiating Cropper:', err);
            }
        }

        if (image.complete && image.naturalWidth > 0) {
            createCropper();
        } else {
            image.onload = function () {
                createCropper();
            };
        }

        return true;
    },

    setAspectRatio: function (ratio) {
        if (this.cropperInstance) {
            var targetRatio = ratio > 0 ? ratio : NaN;
            this.cropperInstance.setAspectRatio(targetRatio);
        }
    },

    zoom: function (delta) {
        if (this.cropperInstance) {
            this.cropperInstance.zoom(delta);
        }
    },

    setZoom: function (scale) {
        if (this.cropperInstance) {
            this.cropperInstance.zoomTo(scale);
        }
    },

    rotate: function (degree) {
        if (this.cropperInstance) {
            this.cropperInstance.rotate(degree);
        }
    },

    scaleX: function () {
        if (this.cropperInstance) {
            var data = this.cropperInstance.getData();
            this.cropperInstance.scaleX(data.scaleX === -1 ? 1 : -1);
        }
    },

    scaleY: function () {
        if (this.cropperInstance) {
            var data = this.cropperInstance.getData();
            this.cropperInstance.scaleY(data.scaleY === -1 ? 1 : -1);
        }
    },

    reset: function () {
        if (this.cropperInstance) {
            this.cropperInstance.reset();
        }
    },

    getCroppedImage: function (targetMinDim, targetMaxDim, quality) {
        var img = document.getElementById('cropper-target-image');
        if (!this.cropperInstance) {
            if (img && img.src) {
                return img.src;
            }
            return null;
        }

        try {
            var data = this.cropperInstance.getData(true);
            var cropW = Math.max(1, Math.round(data.width));
            var cropH = Math.max(1, Math.round(data.height));
            var ar = cropW / cropH;

            // Target dimensions for razor-sharp rendering on Retina / high-DPI displays
            // Default minimum dimension: 1024px (ensures output is never low-res or blurry)
            // Default maximum dimension: 2400px (preserves full photographic detail without excess size)
            var minBase = targetMinDim || 1024;
            var maxBase = targetMaxDim || 2400;

            var outW, outH;

            if (ar >= 1) {
                // Landscape or square: width is base
                outW = Math.max(minBase, Math.min(maxBase, cropW));
                outH = Math.max(1, Math.round(outW / ar));
            } else {
                // Portrait: height is base
                outH = Math.max(minBase, Math.min(maxBase, cropH));
                outW = Math.max(1, Math.round(outH * ar));
            }

            var canvas = this.cropperInstance.getCroppedCanvas({
                width: outW,
                height: outH,
                imageSmoothingEnabled: true,
                imageSmoothingQuality: 'high',
                fillColor: '#ffffff'
            });

            if (!canvas) {
                if (img && img.src) return img.src;
                return null;
            }

            // If the source crop was smaller than output canvas (upscaled), apply subtle unsharp mask sharpening
            // to eliminate any softness and give crisp eyes, hair, and edges
            if (cropW < outW * 0.85 || cropH < outH * 0.85) {
                try {
                    this.sharpenCanvas(canvas, 0.20);
                } catch (sharpErr) {
                    console.warn('Canvas sharpening skipped:', sharpErr);
                }
            }

            return canvas.toDataURL('image/jpeg', quality || 0.95);
        } catch (e) {
            console.error('Error generating cropped canvas:', e);
            if (img && img.src) return img.src;
            return null;
        }
    },

    // Subtle 3x3 unsharp mask / convolution filter to enhance edge crispness when upscaled
    sharpenCanvas: function (canvas, amount) {
        var ctx = canvas.getContext('2d');
        if (!ctx) return;
        var w = canvas.width;
        var h = canvas.height;
        var imgData = ctx.getImageData(0, 0, w, h);
        var src = imgData.data;
        var copy = new Uint8ClampedArray(src);
        var weight = amount || 0.20;

        // 3x3 kernel:
        // [  0,   -w,    0 ]
        // [ -w, 1+4w,   -w ]
        // [  0,   -w,    0 ]
        var centerWeight = 1 + 4 * weight;

        for (var y = 1; y < h - 1; y++) {
            var rowOffset = y * w * 4;
            var prevRowOffset = (y - 1) * w * 4;
            var nextRowOffset = (y + 1) * w * 4;

            for (var x = 1; x < w - 1; x++) {
                var idx = rowOffset + x * 4;
                var topIdx = prevRowOffset + x * 4;
                var btmIdx = nextRowOffset + x * 4;
                var lftIdx = rowOffset + (x - 1) * 4;
                var rgtIdx = rowOffset + (x + 1) * 4;

                // Red
                var r = copy[idx] * centerWeight - (copy[topIdx] + copy[btmIdx] + copy[lftIdx] + copy[rgtIdx]) * weight;
                src[idx] = r < 0 ? 0 : (r > 255 ? 255 : r);

                // Green
                var g = copy[idx + 1] * centerWeight - (copy[topIdx + 1] + copy[btmIdx + 1] + copy[lftIdx + 1] + copy[rgtIdx + 1]) * weight;
                src[idx + 1] = g < 0 ? 0 : (g > 255 ? 255 : g);

                // Blue
                var b = copy[idx + 2] * centerWeight - (copy[topIdx + 2] + copy[btmIdx + 2] + copy[lftIdx + 2] + copy[rgtIdx + 2]) * weight;
                src[idx + 2] = b < 0 ? 0 : (b > 255 ? 255 : b);
            }
        }
        ctx.putImageData(imgData, 0, 0);
    },

    destroy: function () {
        if (this.cropperInstance) {
            try {
                this.cropperInstance.destroy();
            } catch (e) {
                console.warn('Error destroying cropper:', e);
            }
            this.cropperInstance = null;
        }
    }
};
