window.DeckVisualTooltip = (function () {
    const previewId = 'deck-visual-hover-preview';

    function ensurePreviewElement() {
        let preview = document.getElementById(previewId);
        if (preview) {
            return preview;
        }

        preview = document.createElement('div');
        preview.id = previewId;
        preview.setAttribute('aria-hidden', 'true');

        const image = document.createElement('img');
        image.alt = '';
        image.style.width = '100%';
        image.style.display = 'block';
        image.style.borderRadius = '0.5rem';
        preview.appendChild(image);

        preview.style.position = 'fixed';
        preview.style.display = 'none';
        preview.style.width = '300px';
        preview.style.zIndex = '2000';
        preview.style.pointerEvents = 'none';
        preview.style.background = 'var(--bs-body-bg, #fff)';
        preview.style.borderRadius = '0.5rem';
        preview.style.boxShadow = '0 .5rem 1rem rgba(0, 0, 0, .2)';

        document.body.appendChild(preview);
        return preview;
    }

    function position(preview, clientX, clientY) {
        const margin = 16;
        const viewportWidth = window.innerWidth;
        const viewportHeight = window.innerHeight;
        const width = viewportWidth <= 768 ? 240 : 300;
        preview.style.width = `${width}px`;
        const tooltipWidth = preview.offsetWidth || 300;
        const tooltipHeight = preview.offsetHeight || 420;

        const showOnLeft = clientX > (viewportWidth / 2);
        let left = showOnLeft ? clientX - tooltipWidth - margin : clientX + margin;
        let top = clientY - Math.round(tooltipHeight / 4);

        left = Math.max(8, Math.min(left, viewportWidth - tooltipWidth - 8));
        top = Math.max(8, Math.min(top, viewportHeight - tooltipHeight - 8));

        preview.style.left = `${left}px`;
        preview.style.top = `${top}px`;
    }

    function resolveCard(event, element) {
        if (element && element.getAttribute) {
            return element;
        }

        if (event && event.currentTarget && event.currentTarget.getAttribute) {
            return event.currentTarget;
        }

        if (event && event.target && event.target.closest) {
            return event.target.closest('[data-preview-url]');
        }

        return null;
    }

    function updateFromEvent(event, element) {
        const card = resolveCard(event, element);
        if (!card) {
            return;
        }

        const url = card.getAttribute('data-preview-url');
        if (!url) {
            return;
        }

        const alt = card.getAttribute('data-preview-alt') || '';
        const preview = ensurePreviewElement();
        const image = preview.querySelector('img');

        if (!image) {
            return;
        }

        if (image.src !== url) {
            image.src = url;
        }

        image.alt = alt;
        preview.style.display = 'block';
        position(preview, event?.clientX ?? 0, event?.clientY ?? 0);
    }

    function hide() {
        const preview = document.getElementById(previewId);
        if (preview) {
            preview.style.display = 'none';
        }
    }

    return {
        show: updateFromEvent,
        move: updateFromEvent,
        hide
    };
})();
