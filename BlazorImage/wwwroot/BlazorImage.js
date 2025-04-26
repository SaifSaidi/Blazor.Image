(function (windowObj, documentObj) {
    "use strict";

    const config = {
        lazyClass: "_placeholder_lazy_load",
        loadedClass: "blazorlazyloaded",
        srcAttr: "data-src",
        srcsetAttr: "data-srcset",
        observedAttr: "data-lazy-observed",
        rootMargin: "200px 0px",
        threshold: 0.1,
        devInfoContainerClass: ".developer-info-container",
        devInfoOverlayClass: ".developer-info-popup-overlay",
        devInfoPopupClass: ".developer-info-popup",
        devInfoButtonClass: ".info-toggle",
        devInfoShowClass: "show",
    };

    function getDeveloperInfoElements(triggerElement) {
        const container = triggerElement.closest(config.devInfoContainerClass);
        if (!container) return null;
        return {
            container,
            overlay: container.querySelector(config.devInfoOverlayClass),
            popup: container.querySelector(config.devInfoPopupClass),
            button: container.querySelector(config.devInfoButtonClass)
        };
    }

    windowObj.toggleDeveloperInfo = function (buttonElement) {
        if (!buttonElement) return;
        const elements = getDeveloperInfoElements(buttonElement);
        if (!elements?.overlay || !elements?.popup) return;
        const isExpanded = elements.overlay.classList.toggle(config.devInfoShowClass);
        elements.popup.classList.toggle(config.devInfoShowClass);
        buttonElement.setAttribute("aria-expanded", isExpanded.toString());
    };

    windowObj.closeDeveloperInfo = function (overlayElement) {
        if (!overlayElement) return;
        const elements = getDeveloperInfoElements(overlayElement);
        if (!elements?.overlay || !elements?.popup) return;
        elements.overlay.classList.remove(config.devInfoShowClass);
        elements.popup.classList.remove(config.devInfoShowClass);
        elements.button?.setAttribute("aria-expanded", "false");
    };

    const loadingImages = new WeakMap();
    let observer = null;

    const loadImage = (img) => {
        if (img.classList.contains(config.loadedClass) || loadingImages.has(img)) {
            return;
        }

        loadingImages.set(img, true);

        const { srcAttr, srcsetAttr, loadedClass, observedAttr } = config;
        const src = img.getAttribute(srcAttr);
        const srcset = img.getAttribute(srcsetAttr);

        const parent = img.parentElement;
        if (parent?.tagName === "PICTURE") {
            parent.querySelectorAll(`source[${srcsetAttr}]`).forEach(source => {
                source.srcset = source.getAttribute(srcsetAttr);
                source.removeAttribute(srcsetAttr);
            });
        }

        const handleLoad = () => {
            img.classList.add(loadedClass);
            cleanUp();
        };

        const handleError = () => {
            img.classList.add('lazyload-error');
            cleanUp();
        };

        const cleanUp = () => {
            loadingImages.delete(img);
            img.removeAttribute(srcAttr);
            img.removeAttribute(srcsetAttr);
            img.removeAttribute(observedAttr);
            img.removeEventListener('load', handleLoad);
            img.removeEventListener('error', handleError);
        }

        img.addEventListener('load', handleLoad);
        img.addEventListener('error', handleError);

        if (src) img.src = src;
        if (srcset) img.srcset = srcset;

        if (img.complete) {
            handleLoad();
        }
    };

    const initObserver = () => {
        if (observer || !("IntersectionObserver" in windowObj)) {
            return observer;
        }
        observer = new IntersectionObserver(
            (entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        const target = entry.target;
                        loadImage(target);
                        observer.unobserve(target);
                    }
                });
            },
            { rootMargin: config.rootMargin, threshold: config.threshold }
        );
        return observer;
    };

    const observeImages = (rootElement = documentObj) => {
        const currentObserver = initObserver();

        if (!currentObserver) {
            console.warn("IntersectionObserver not supported. Lazy loading via observer disabled.");
            return;
        }

        const selector = `img.${config.lazyClass}[loading="lazy"]:not([${config.observedAttr}])`;
        const lazyImages = rootElement.querySelectorAll(selector);

        if (lazyImages.length === 0) return;

        const lazyImagesArray = Array.from(lazyImages);

        for (const img of lazyImagesArray) {
            if (!img.classList.contains(config.loadedClass)) {
                img.setAttribute(config.observedAttr, "true");
                currentObserver.observe(img);
            }
        }
    };

    windowObj.BlazorLazyLoad = function (imgElement) {
        if (!(imgElement instanceof Element)) {
            console.warn("BlazorLazyLoad called with invalid element:", imgElement);
            return;
        }

        if (
            imgElement.classList.contains(config.lazyClass) &&
            imgElement.getAttribute("loading") === "lazy" &&
            !imgElement.hasAttribute(config.observedAttr) &&
            !imgElement.classList.contains(config.loadedClass)
        ) {
            const currentObserver = initObserver();
            if (currentObserver) {
                imgElement.setAttribute(config.observedAttr, "true");
                currentObserver.observe(imgElement);
            } else {
                console.warn("Observer not available for BlazorLazyLoad, loading image directly.", imgElement);
                loadImage(imgElement);
            }
        }
    };

    const onDOMReady = () => observeImages();

    if (documentObj.readyState === "loading") {
        documentObj.addEventListener("DOMContentLoaded", onDOMReady, { once: true, passive: true });
    }

    windowObj.Blazor.addEventListener("enhancedload", () => {
        onDOMReady();
    });
})(window, document);
