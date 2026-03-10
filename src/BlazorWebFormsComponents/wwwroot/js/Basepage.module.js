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

// === PageStyleSheet Registry ===
// Reference-counting registry for stylesheet lifecycle management.
// CSS persists until NO PageStyleSheet component references it.

/**
 * Stylesheet lifecycle registry.
 * Manages reference counting for dynamic CSS loading/unloading.
 */
const stylesheetRegistry = {
    // href -> Set<componentId>
    refs: new Map(),
    // href -> HTMLLinkElement
    links: new Map(),
    // Cleanup timer handle for debouncing
    cleanupTimer: null,

    /**
     * Normalize href to absolute URL for consistent ref counting.
     * @param {string} href - The href to normalize
     * @returns {string} Absolute URL
     */
    normalizeHref(href) {
        if (!href) return href;
        // Use URL constructor to resolve relative paths
        try {
            return new URL(href, document.baseURI).href;
        } catch {
            return href;
        }
    },

    /**
     * Register a component's reference to a stylesheet.
     * Creates the <link> element if it doesn't exist, or adopts existing one.
     * @param {string} componentId - Unique ID of the registering component
     * @param {string} href - URL of the stylesheet
     * @param {string|null} media - Optional media query
     * @param {string|null} integrity - Optional SRI hash
     * @param {string|null} crossOrigin - Optional crossorigin attribute
     */
    register(componentId, href, media, integrity, crossOrigin) {
        const normalizedHref = this.normalizeHref(href);
        
        // Get or create ref set
        if (!this.refs.has(normalizedHref)) {
            this.refs.set(normalizedHref, new Set());
        }
        this.refs.get(normalizedHref).add(componentId);

        // Check if we already track a link for this href
        let link = this.links.get(normalizedHref);
        if (!link) {
            // Try to find existing link in DOM (from SSR static render)
            link = document.querySelector(`link[href="${href}"]`) ||
                   document.querySelector(`link[href="${normalizedHref}"]`);
            
            if (link) {
                // Adopt existing static link
                this.links.set(normalizedHref, link);
                console.debug(`[BWFC] Adopted existing stylesheet: ${href}`);
            } else {
                // Create new link element
                link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = href;
                if (media) link.media = media;
                if (integrity) link.integrity = integrity;
                if (crossOrigin) link.crossOrigin = crossOrigin;
                document.head.appendChild(link);
                this.links.set(normalizedHref, link);
                console.debug(`[BWFC] Loaded stylesheet: ${href}`);
            }
        } else {
            console.debug(`[BWFC] Stylesheet ${href} already managed, added ref from ${componentId}`);
        }
    },

    /**
     * Unregister a component's reference to a stylesheet.
     * Schedules cleanup with debounce to handle navigation transitions.
     * @param {string} componentId - Unique ID of the unregistering component
     * @param {string} href - URL of the stylesheet
     */
    unregister(componentId, href) {
        const normalizedHref = this.normalizeHref(href);
        const refs = this.refs.get(normalizedHref);
        
        if (refs) {
            refs.delete(componentId);
            console.debug(`[BWFC] Unregistered ${componentId} from ${href}, ${refs.size} refs remaining`);
            
            if (refs.size === 0) {
                this.refs.delete(normalizedHref);
            }
        }

        // Schedule cleanup with debounce (100ms allows new page to register)
        this.scheduleCleanup();
    },

    /**
     * Schedule orphan cleanup after navigation settles.
     * Uses 100ms debounce to handle rapid unregister/register cycles.
     */
    scheduleCleanup() {
        clearTimeout(this.cleanupTimer);
        this.cleanupTimer = setTimeout(() => {
            this.cleanupOrphans();
        }, 100);
    },

    /**
     * Remove stylesheets with no component references.
     */
    cleanupOrphans() {
        for (const [href, link] of this.links.entries()) {
            const refs = this.refs.get(href);
            if (!refs || refs.size === 0) {
                link.remove();
                this.links.delete(href);
                console.debug(`[BWFC] Unloaded orphan stylesheet: ${href}`);
            }
        }
    },

    /**
     * Adopt an existing static <link> element (from SSR) into the registry.
     * Call this after hydration for links rendered during prerender.
     * @param {string} linkId - ID of the existing link element
     * @param {string} componentId - Component ID to register
     * @param {string} href - URL of the stylesheet
     */
    adoptStaticLink(linkId, componentId, href) {
        const normalizedHref = this.normalizeHref(href);
        const link = document.getElementById(linkId);
        
        if (link) {
            // Add to registry
            if (!this.refs.has(normalizedHref)) {
                this.refs.set(normalizedHref, new Set());
            }
            this.refs.get(normalizedHref).add(componentId);
            this.links.set(normalizedHref, link);
            console.debug(`[BWFC] Adopted static link ${linkId} for ${href}`);
        } else {
            // Link not found, fall back to regular register
            console.debug(`[BWFC] Static link ${linkId} not found, creating new`);
            this.register(componentId, href, null, null, null);
        }
    }
};

// Export registry functions for module use
export function registerStyleSheet(componentId, href, media, integrity, crossOrigin) {
    stylesheetRegistry.register(componentId, href, media, integrity, crossOrigin);
}

export function unregisterStyleSheet(componentId, href) {
    stylesheetRegistry.unregister(componentId, href);
}

export function adoptStyleSheet(linkId, componentId, href) {
    stylesheetRegistry.adoptStaticLink(linkId, componentId, href);
}

// === Legacy PageStyleSheet support (deprecated, kept for backward compatibility) ===

/**
 * @deprecated Use registerStyleSheet instead
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
 * @deprecated Use unregisterStyleSheet instead
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
    // Expose stylesheet functions (both legacy and registry)
    window.bwfc.loadStyleSheet = loadStyleSheet;
    window.bwfc.unloadStyleSheet = unloadStyleSheet;
    window.bwfc.stylesheetRegistry = stylesheetRegistry;
    window.bwfc.registerStyleSheet = registerStyleSheet;
    window.bwfc.unregisterStyleSheet = unregisterStyleSheet;
    window.bwfc.adoptStyleSheet = adoptStyleSheet;
}
