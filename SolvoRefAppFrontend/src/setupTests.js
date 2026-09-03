import '@testing-library/jest-dom/vitest';

if (!window.matchMedia) {
    window.matchMedia = query => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: () => {},
        removeListener: () => {},
        addEventListener: () => {},
        removeEventListener: () => {},
        dispatchEvent: () => false,
    });
}

if (!window.requestAnimationFrame) {
    window.requestAnimationFrame = callback => window.setTimeout(callback, 0);
}

if (!window.cancelAnimationFrame) {
    window.cancelAnimationFrame = id => window.clearTimeout(id);
}

if (!globalThis.requestAnimationFrame) {
    globalThis.requestAnimationFrame = window.requestAnimationFrame;
}

if (!globalThis.cancelAnimationFrame) {
    globalThis.cancelAnimationFrame = window.cancelAnimationFrame;
}
