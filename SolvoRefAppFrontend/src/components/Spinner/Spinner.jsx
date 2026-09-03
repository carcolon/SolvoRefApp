import { useLayoutEffect, useRef } from 'react';
import { gsap } from 'gsap';
import './Spinner.css';

const Spinner = ({
    isLoading = true,
    title = 'Referral Program',
    message = '',
}) => {
    const loaderRef = useRef(null);
    const iconRef = useRef(null);
    const glowRef = useRef(null);

    useLayoutEffect(() => {
        if (!isLoading || !loaderRef.current) {
            return undefined;
        }

        const ctx = gsap.context(() => {
            gsap.fromTo(
                loaderRef.current,
                { autoAlpha: 0 },
                { autoAlpha: 1, duration: 0.2, ease: 'power2.out' },
            );

            gsap.to(iconRef.current, {
                rotate: 360,
                duration: 1.6,
                ease: 'none',
                repeat: -1,
                transformOrigin: '50% 50%',
            });

            gsap.to(glowRef.current, {
                scale: 1.12,
                opacity: 0.65,
                duration: 0.9,
                repeat: -1,
                yoyo: true,
                ease: 'power1.inOut',
            });
        }, loaderRef);

        return () => ctx.revert();
    }, [isLoading]);

    if (!isLoading) {
        return null;
    }

    return (
        <div
            ref={loaderRef}
            data-testid="spinnerContainer"
            className="app-loader-overlay"
            role="status"
            aria-live="polite"
        >
            <div className="app-loader-shell">
                <div ref={glowRef} className="app-loader-glow" />
                <img
                    ref={iconRef}
                    src="/Recurso-5.ico"
                    alt="Loading"
                    className="app-loader-icon"
                />
                {title ? <h2 className="app-loader-title">{title}</h2> : null}
                {message ? <p className="app-loader-message">{message}</p> : null}
            </div>
        </div>
    );
};

export default Spinner;
