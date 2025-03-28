/**
 * @jest-environment jsdom
*/
import '@testing-library/jest-dom'
import '../wwwroot/BlazorImage';

describe('Blazor Lazy Load Script', () => {
    let button, overlay, popup, container, image, picture, source;

    beforeEach(() => {
        document.body.innerHTML = `
            <div class="developer-info-container">
                <button class="info-toggle">Toggle</button>
                <div class="developer-info-popup-overlay"></div>
                <div class="developer-info-popup"></div>
            </div>
            <img class="_blazor_lazy_load placeholder" data-src="image.jpg" data-srcset="image-1x.jpg 1x, image-2x.jpg 2x" loading="lazy">
            <picture>
                <source data-srcset="source-1x.jpg 1x, source-2x.jpg 2x">
                <img class="_blazor_lazy_load placeholder" data-src="image2.jpg" loading="lazy">
            </picture>
        `;
        button = document.querySelector('.info-toggle');
        overlay = document.querySelector('.developer-info-popup-overlay');
        popup = document.querySelector('.developer-info-popup');
        container = document.querySelector('.developer-info-container');
        image = document.querySelector('img._blazor_lazy_load');
        picture = document.querySelector('picture');
        source = picture.querySelector('source');

        // Mock IntersectionObserver
        global.IntersectionObserver = class {
            constructor(cb) {
                this.cb = cb;
            }
            observe(element) {
                this.cb([{ isIntersecting: true, target: element }]);
            }
            unobserve() { }
            disconnect() { }
        };
    });

    describe('toggleDeveloperInfo', () => {
        test('toggles overlay and popup visibility and aria-expanded', () => {
            window.toggleDeveloperInfo(button);
            expect(overlay).toHaveClass('show');
            expect(popup).toHaveClass('show');
            expect(button).toHaveAttribute('aria-expanded', 'true');

            window.toggleDeveloperInfo(button);
            expect(overlay).not.toHaveClass('show');
            expect(popup).not.toHaveClass('show');
            expect(button).toHaveAttribute('aria-expanded', 'false');
        });

        test('does nothing if button has no container', () => {
            const orphanBtn = document.createElement('button');
            expect(() => window.toggleDeveloperInfo(orphanBtn)).not.toThrow();
        });
    });

    describe('closeDeveloperInfo', () => {
        test('removes show classes and sets aria-expanded to false', () => {
            overlay.classList.add('show');
            popup.classList.add('show');
            button.setAttribute('aria-expanded', 'true');

            window.closeDeveloperInfo(overlay);

            expect(overlay).not.toHaveClass('show');
            expect(popup).not.toHaveClass('show');
            expect(button).toHaveAttribute('aria-expanded', 'false');
        });

        test('does nothing if overlay has no container', () => {
            const orphanOverlay = document.createElement('div');
            orphanOverlay.classList.add('developer-info-popup-overlay');
            expect(() => window.closeDeveloperInfo(orphanOverlay)).not.toThrow();
        });
    });

    describe('BlazorLazyLoad image handling', () => {
        test('loads an image and removes placeholder', () => {
            window.BlazorLazyLoad(image);

            // Simulate image load event
            image.dispatchEvent(new Event('load'));

            expect(image.src).toContain('image.jpg');
            expect(image).toHaveClass('blazorlazyloaded');
            expect(image).not.toHaveClass('placeholder');
        });

        test('loads picture sources with data-srcset', () => {
            const pictureImg = picture.querySelector('img');
            window.BlazorLazyLoad(pictureImg);

            pictureImg.dispatchEvent(new Event('load'));

            expect(pictureImg.src).toContain('image2.jpg');
            expect(source.srcset).toContain('source-1x.jpg');
            expect(pictureImg).toHaveClass('blazorlazyloaded');
        });

        test('logs error and cleans up on image load failure', () => {
            console.error = jest.fn(); // Mock console.error

            window.BlazorLazyLoad(image);
            image.dispatchEvent(new Event('error'));

            expect(console.error).toHaveBeenCalledWith('Failed to load image:', image);
        });
    });

    describe('DOMContentLoaded simulation', () => {
        test('initializes observer and observes images', () => {
            // Clear and re-add the listener simulation
            document.body.innerHTML += `
                <img class="_blazor_lazy_load placeholder" data-src="image3.jpg" loading="lazy">
            `;

            const domContentEvent = new Event('DOMContentLoaded');
            document.dispatchEvent(domContentEvent);

            const images = document.querySelectorAll('img._blazor_lazy_load');
            images.forEach((img) => {
                expect(img.dataset.lazyObserved).toBe('true');
            });
        });
    });
});