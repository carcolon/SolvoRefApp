import { useLayoutEffect } from 'react';
import { gsap } from 'gsap';

const BUTTON_SELECTOR = '.gsap-button, .btn-submit, .btn-referral, .button1, .b-outline, .principalMenuHamburger, .pView';
const CARD_SELECTOR = '.gsap-card, .position-card, .getReferals, .status-group, .card-mb-3, .home-panel, .home-highlight-card';

export function useInteractiveMotion(scopeRef, dependencies = []) {
    useLayoutEffect(() => {
        const scope = scopeRef?.current ?? document.body;
        const listeners = [];

        const bindHover = (elements, enterConfig, leaveConfig) => {
            elements.forEach((element) => {
                gsap.set(element, { transformPerspective: 800, transformOrigin: 'center center' });

                const handleEnter = () => gsap.to(element, enterConfig);
                const handleLeave = () => gsap.to(element, leaveConfig);

                element.addEventListener('mouseenter', handleEnter);
                element.addEventListener('mouseleave', handleLeave);
                element.addEventListener('focus', handleEnter);
                element.addEventListener('blur', handleLeave);

                listeners.push(() => {
                    element.removeEventListener('mouseenter', handleEnter);
                    element.removeEventListener('mouseleave', handleLeave);
                    element.removeEventListener('focus', handleEnter);
                    element.removeEventListener('blur', handleLeave);
                });
            });
        };

        bindHover(
            gsap.utils.toArray(BUTTON_SELECTOR, scope),
            {
                y: -3,
                scale: 1.02,
                boxShadow: '0 16px 32px rgba(11, 33, 53, 0.18)',
                duration: 0.22,
                ease: 'power2.out',
            },
            {
                y: 0,
                scale: 1,
                boxShadow: '0 0 0 rgba(11, 33, 53, 0)',
                duration: 0.22,
                ease: 'power2.out',
            },
        );

        bindHover(
            gsap.utils.toArray(CARD_SELECTOR, scope),
            {
                y: -8,
                rotateX: 1.5,
                scale: 1.01,
                boxShadow: '0 24px 48px rgba(11, 33, 53, 0.14)',
                duration: 0.28,
                ease: 'power2.out',
            },
            {
                y: 0,
                rotateX: 0,
                scale: 1,
                boxShadow: '0 10px 24px rgba(11, 33, 53, 0.08)',
                duration: 0.28,
                ease: 'power2.out',
            },
        );

        return () => {
            listeners.forEach((dispose) => dispose());
        };
    }, [scopeRef, ...dependencies]);
}
