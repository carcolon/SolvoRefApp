import { useLayoutEffect } from 'react';
import { gsap } from 'gsap';

export function useModalMotion(scopeRef, isOpen) {
    useLayoutEffect(() => {
        const scope = scopeRef.current;
        if (!scope || !isOpen) {
            return undefined;
        }

        const ctx = gsap.context(() => {
            const overlay = scope.querySelector('[data-modal-overlay]');
            const panel = scope.querySelector('[data-modal-panel]');

            if (overlay) {
                gsap.fromTo(
                    overlay,
                    { autoAlpha: 0 },
                    { autoAlpha: 1, duration: 0.22, ease: 'power2.out' },
                );
            }

            if (panel) {
                gsap.fromTo(
                    panel,
                    { y: 32, autoAlpha: 0, scale: 0.98 },
                    { y: 0, autoAlpha: 1, scale: 1, duration: 0.35, ease: 'power3.out' },
                );
            }
        }, scope);

        return () => ctx.revert();
    }, [scopeRef, isOpen]);
}
