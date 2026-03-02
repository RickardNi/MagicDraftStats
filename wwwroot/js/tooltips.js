window.MagicDraftStatsTooltips = (function () {
    const initialized = new WeakSet();

    function init(selector) {
        if (!window.bootstrap || !window.bootstrap.Tooltip) {
            return;
        }

        const elements = document.querySelectorAll(selector || '[data-bs-toggle="tooltip"]');
        elements.forEach(element => {
            if (initialized.has(element)) {
                return;
            }

            new bootstrap.Tooltip(element, {
                html: false,
                trigger: 'hover focus',
                placement: 'top',
                boundary: 'window'
            });

            initialized.add(element);
        });
    }

    return {
        init
    };
})();
