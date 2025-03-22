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

    // Developer Info Toggle Handlers
    windowObj.toggleDeveloperInfo = function (buttonElement) {
        const container = buttonElement.closest(".developer-info-container");
        if (!container) return;

        const overlay = container.querySelector(".developer-info-popup-overlay"),
            popup = container.querySelector(".developer-info-popup");

        if (overlay && popup) {
            overlay.classList.toggle("show");
            popup.classList.toggle("show");
            buttonElement.setAttribute("aria-expanded", overlay.classList.contains("show"));
        }
    };

    windowObj.closeDeveloperInfo = function (overlayElement) {
        const container = overlayElement.closest(".developer-info-container");
        if (!container) return;

        const overlay = container.querySelector(".developer-info-popup-overlay"),
            popup = container.querySelector(".developer-info-popup"),
            button = container.querySelector(".info-toggle");

        if (overlay && popup) {
            overlay.classList.remove("show");
            popup.classList.remove("show");
            if (button) button.setAttribute("aria-expanded", "false");
        }
    };

    const loadingImages = new Map();
    let observer;

    const loadImage = (img) => {
        if (loadingImages.has(img)) return;
        loadingImages.set(img, true);

        const { srcAttr, srcsetAttr, loadedClass } = config;
        let src = img.getAttribute(srcAttr),
            srcset = img.getAttribute(srcsetAttr);

        if (img.parentNode.tagName === "PICTURE") {
            img.parentNode.querySelectorAll("source").forEach((source) => {
                if (source.hasAttribute(srcsetAttr)) {
                    source.srcset = source.getAttribute(srcsetAttr);
                    source.removeAttribute(srcsetAttr);
                }
            });
        }

        if (src) img.src = src;
        if (srcset) img.srcset = srcset;

        img.onload = () => {
            img.classList.add(loadedClass);
            img.classList.remove("placeholder");
            loadingImages.delete(img);
        };

        img.onerror = () => {
            console.error("Failed to load image:", img);
            loadingImages.delete(img);
        };

        img.removeAttribute(srcAttr);
        img.removeAttribute(srcsetAttr);
    };

    const initObserver = () => {
        if (!observer && "IntersectionObserver" in windowObj) {
            observer = new IntersectionObserver((entries) => {
                entries.forEach(({ isIntersecting, target }) => {
                    if (isIntersecting) {
                        loadImage(target);
                        observer.unobserve(target);
                    }
                });
            }, { rootMargin: config.rootMargin, threshold: config.threshold });
        }
        return observer;
    };

    const observeImages = () => {

        const lazyImages = documentObj.querySelectorAll(`.${config.lazyClass}[loading="lazy"]:not([data-lazy-observed])`);
        if (lazyImages.length < 1) return;

        if (observer) {
            lazyImages.forEach((img) => {
                img.setAttribute("data-lazy-observed", "true");
                observer.observe(img);
            });
        }
    };

    windowObj.BlazorLazyLoad = function (img) {
        if (img && img.classList.contains(config.lazyClass) && img.getAttribute("loading") === "lazy" && !img.dataset.lazyObserved) {
            // Ensure observer is initialized if it's not yet
            if (!observer) {
                observer = initObserver();
            }
            img.setAttribute("data-lazy-observed", "true");
            observer.observe(img);
        }
    };

    documentObj.addEventListener("DOMContentLoaded", () => {
        observer = initObserver();
        observeImages();
    }, { once: true });

    if (windowObj.Blazor) {
        Blazor.addEventListener("enhancedload", observeImages);
    }

})(window, document);

