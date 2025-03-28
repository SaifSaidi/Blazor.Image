(function (windowObj, documentObj) {
    "use strict";

    const config = {
        lazyClass: "_blazor_lazy_load",
        loadedClass: "blazorlazyloaded",
        srcAttr: "data-src",
        srcsetAttr: "data-srcset",
        rootMargin: "200px 0px",
        threshold: 0.1,
    };

    // Cache DOM selectors and reduce lookups
    const selectors = {
        getOverlay: container => container.querySelector(".developer-info-popup-overlay"),
        getPopup: container => container.querySelector(".developer-info-popup"),
        getButton: container => container.querySelector(".info-toggle")
    };

    // Developer Info Toggle Handlers
    windowObj.toggleDeveloperInfo = function (buttonElement) {
        const container = buttonElement.closest(".developer-info-container");
        if (!container) return;

        const overlay = selectors.getOverlay(container),
            popup = selectors.getPopup(container);

        if (overlay && popup) {
            const isExpanded = overlay.classList.toggle("show");
            popup.classList.toggle("show");
            buttonElement.setAttribute("aria-expanded", isExpanded);
        }
    };

    windowObj.closeDeveloperInfo = function (overlayElement) {
        const container = overlayElement.closest(".developer-info-container");
        if (!container) return;

        const overlay = selectors.getOverlay(container),
            popup = selectors.getPopup(container),
            button = selectors.getButton(container);

        if (overlay && popup) {
            overlay.classList.remove("show");
            popup.classList.remove("show");
            if (button) button.setAttribute("aria-expanded", "false");
        }
    };
 
     const loadingImages = new WeakMap();
    let observer;

    const loadImage = (img) => {
        if (loadingImages.has(img) || img.classList.contains(config.loadedClass)) return;

        loadingImages.set(img, true);

        const { srcAttr, srcsetAttr, loadedClass } = config;
        const src = img.getAttribute(srcAttr),
            srcset = img.getAttribute(srcsetAttr);

        // Handle picture element sources
        if (img.parentNode.tagName === "PICTURE") {
            const sources = img.parentNode.querySelectorAll("source");
            for (let i = 0; i < sources.length; i++) {
                const source = sources[i];
                if (source.hasAttribute(srcsetAttr)) {
                     
                    source.srcset = source.getAttribute(srcsetAttr);
                    source.removeAttribute(srcsetAttr);
                }
            }
        }

        // Set image attributes
        if (src) img.src = src;
        if (srcset) img.srcset = srcset;

        // Handle load events
        img.onload = () => { 
            img.classList.add(loadedClass);
            loadingImages.delete(img);
        };

        img.onerror = () => {
            console.error("Failed to load image:", img.src);
            loadingImages.delete(img);
        };

        // Clean up attributes
        img.removeAttribute(srcAttr);
        img.removeAttribute(srcsetAttr);
    };

    const initObserver = () => {
        if (!observer && "IntersectionObserver" in windowObj) {
            observer = new IntersectionObserver((entries) => {
                for (let i = 0; i < entries.length; i++) {
                    const { isIntersecting, target } = entries[i];
                    if (isIntersecting) {
                        loadImage(target);
                        observer.unobserve(target);
                    }
                }
            }, { rootMargin: config.rootMargin, threshold: config.threshold });
        }
        return observer;
    };

    const observeImages = () => {
        const lazyImages = documentObj.querySelectorAll(`.${config.lazyClass}[loading="lazy"]:not([data-lazy-observed])`);
        if (lazyImages.length < 1) return;

        if (!observer) {
            observer = initObserver();
        }

        for (let i = 0; i < lazyImages.length; i++) {
            const img = lazyImages[i];
            // Only observe if not already loaded
            if (!img.classList.contains(config.loadedClass)) {
                img.setAttribute("data-lazy-observed", "true");
                observer.observe(img);
            }
        }
    };

    windowObj.BlazorLazyLoad = function (img) {
        if (img &&
            img.classList.contains(config.lazyClass) &&
            img.getAttribute("loading") === "lazy" &&
            !img.dataset.lazyObserved) {

            // Ensure observer is initialized
            if (!observer) {
                observer = initObserver();
            }

            img.setAttribute("data-lazy-observed", "true");
            observer.observe(img);
        }
    };

    // Use passive event listener for better performance
    documentObj.addEventListener("DOMContentLoaded", () => {
        observer = initObserver();
        observeImages();
    }, { once: true, passive: true });

    // Handle Blazor specific events
    if (windowObj.Blazor) {
        Blazor.addEventListener("enhancedload", observeImages);
    }

})(window, document);