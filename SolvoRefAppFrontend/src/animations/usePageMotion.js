import { useLayoutEffect } from 'react';
import { gsap } from 'gsap';

export function usePageMotion(scopeRef, dependencies = []) {
    useLayoutEffect(() => {
        const scope = scopeRef.current;
        if (!scope) {
            return undefined;
        }

        const ctx = gsap.context(() => {
            const tl = gsap.timeline({ defaults: { ease: 'power3.out' } });
            const hero = scope.querySelectorAll('[data-hero]');
            const reveal = scope.querySelectorAll('[data-reveal]');
            const cards = scope.querySelectorAll('[data-card]');
            const floaters = scope.querySelectorAll('[data-float]');

            tl.fromTo(
                scope,
                { autoAlpha: 0 },
                { autoAlpha: 1, duration: 0.01 },
            );

            if (hero.length) {
                tl.fromTo(
                    hero,
                    { y: 36, autoAlpha: 0 },
                    { y: 0, autoAlpha: 1, duration: 0.8, stagger: 0.08 },
                    0,
                );
            }

            if (reveal.length) {
                tl.fromTo(
                    reveal,
                    { y: 22, autoAlpha: 0 },
                    { y: 0, autoAlpha: 1, duration: 0.65, stagger: 0.06 },
                    hero.length ? 0.15 : 0,
                );
            }

            if (cards.length) {
                tl.fromTo(
                    cards,
                    { y: 28, scale: 0.98, autoAlpha: 0 },
                    {
                        y: 0,
                        scale: 1,
                        autoAlpha: 1,
                        duration: 0.75,
                        stagger: 0.08,
                    },
                    hero.length || reveal.length ? 0.22 : 0,
                );
            }

            floaters.forEach((item, index) => {
                gsap.to(item, {
                    y: index % 2 === 0 ? -10 : 10,
                    duration: 2.4 + index * 0.2,
                    repeat: -1,
                    yoyo: true,
                    ease: 'sine.inOut',
                });
            });
        }, scope);

        return () => ctx.revert();
    }, [scopeRef, ...dependencies]);
}
