// ES Module version of Basepage.js
// This can be imported automatically by components

export function setTitle(title) {
    document.title = title;
}

export function getTitle() {
    return document.title;
}

export function onAfterRender() {
    console.debug("Running Window.load function");
    formatClientClick();
}

export function addScriptElement(location, callback) {
    var el = document.createElement("script");
    el.setAttribute("src", location);
    document.head.appendChild(el);

    if (callback != null) {
        el.addEventListener("load", function () { eval(callback); });
    }
}

function formatClientClick() {
    var elementsToReplace = document.querySelectorAll("*[onclientclick]");
    for (var el of elementsToReplace) {
        if (!el.getAttribute("data-onclientclick")) {
            console.debug(el.getAttribute("onclientclick"));
            el.addEventListener('click', function (e) { eval(e.target.getAttribute('onclientclick')) });
            el.setAttribute("data-onclientclick", "1");
        }
    }
}

// === PageStyleSheet support ===

/**
 * Dynamically loads a stylesheet into the document head.
 * @param {string} id - Unique ID for the link element
 * @param {string} href - URL of the stylesheet
 * @param {string|null} media - Optional media query
 * @param {string|null} integrity - Optional SRI hash
 * @param {string|null} crossOrigin - Optional crossorigin attribute
 */
export function loadStyleSheet(id, href, media, integrity, crossOrigin) {
    // Check if already loaded (idempotent)
    if (document.getElementById(id)) {
        console.debug(`[BWFC] Stylesheet ${id} already loaded`);
        return;
    }
    
    const link = document.createElement('link');
    link.id = id;
    link.rel = 'stylesheet';
    link.href = href;
    
    if (media) link.media = media;
    if (integrity) link.integrity = integrity;
    if (crossOrigin) link.crossOrigin = crossOrigin;
    
    document.head.appendChild(link);
    console.debug(`[BWFC] Loaded stylesheet: ${href} (id: ${id})`);
}

/**
 * Removes a previously loaded stylesheet from the document head.
 * @param {string} id - ID of the link element to remove
 */
export function unloadStyleSheet(id) {
    const link = document.getElementById(id);
    if (link) {
        const href = link.href;
        link.remove();
        console.debug(`[BWFC] Unloaded stylesheet: ${href} (id: ${id})`);
    }
}

// Also expose on window for backward compatibility
if (typeof window !== 'undefined') {
    window.bwfc = window.bwfc ?? {};
    window.bwfc.Page = {
        setTitle,
        getTitle,
        OnAfterRender: onAfterRender,
        AddScriptElement: addScriptElement
    };
    // Expose stylesheet functions too
    window.bwfc.loadStyleSheet = loadStyleSheet;
    window.bwfc.unloadStyleSheet = unloadStyleSheet;
}
