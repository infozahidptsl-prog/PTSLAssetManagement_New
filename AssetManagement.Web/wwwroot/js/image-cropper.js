window.imageCropperInterop = {
    cropperInstance: null,

    initCropper: function (imageElementId, aspectRatio) {
        var image = document.getElementById(imageElementId);
        if (!image) {
            console.error('Image element not found:', imageElementId);
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

        var ratio = aspectRatio > 0 ? aspectRatio : (238 / 176); // Default 1.352 (profile card ratio)

        // Wait until image is loaded if not already
        var self = this;
        function createCropper() {
            if (self.cropperInstance) {
                self.cropperInstance.destroy();
            }
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

    getCroppedImage: function (maxWidth, maxHeight, quality) {
        if (!this.cropperInstance) {
            return null;
        }

        var canvas = this.cropperInstance.getCroppedCanvas({
            maxWidth: maxWidth || 800,
            maxHeight: maxHeight || 800,
            imageSmoothingEnabled: true,
            imageSmoothingQuality: 'high'
        });

        if (!canvas) {
            return null;
        }

        return canvas.toDataURL('image/jpeg', quality || 0.92);
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
