window.quillInterop = {
    init: function (containerId, dotNetHelper, initialValue) {
        var quill = new Quill('#' + containerId, {
            theme: 'snow',
            placeholder: 'Write the about text here...',
            modules: {
                toolbar: [
                    ['bold', 'italic', 'underline', 'strike'],
                    [{ 'list': 'ordered'}, { 'list': 'bullet' }],
                    [{ 'align': [] }],
                    ['clean']
                ]
            }
        });
        
        if (initialValue) {
            // Auto-heal legacy text and broken HTML
            let healedValue = initialValue;
            
            if (!healedValue.includes('<p>') && !healedValue.includes('<br>')) {
                // Completely legacy text - convert double newlines to paragraphs
                let paragraphs = healedValue.split(/\r?\n\s*\r?\n/);
                healedValue = paragraphs.map(p => {
                    return '<p>' + p.replace(/\r?\n/g, ' ') + '</p>';
                }).join('');
            } else {
                // Already HTML, but might have broken <p> tags from bad saves.
                // If a paragraph ends with a normal word character and the next starts, merge them.
                healedValue = healedValue.replace(/([a-zA-Z0-9,;-])\s*<\/p>\s*<p>/g, "$1 ");
            }
            
            quill.root.innerHTML = healedValue;
        }

        quill.on('text-change', function () {
            dotNetHelper.invokeMethodAsync('OnContentChanged', quill.root.innerHTML);
        });
    }
};
