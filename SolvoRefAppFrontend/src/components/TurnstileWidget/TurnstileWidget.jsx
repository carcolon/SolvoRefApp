import { useEffect, useRef } from 'react';

const TURNSTILE_SCRIPT_ID = 'cloudflare-turnstile-script';

function loadTurnstileScript() {
    if (typeof window === 'undefined') {
        return Promise.resolve();
    }

    if (window.turnstile) {
        return Promise.resolve();
    }

    const existingScript = document.getElementById(TURNSTILE_SCRIPT_ID);
    if (existingScript) {
        return new Promise((resolve, reject) => {
            existingScript.addEventListener('load', resolve, { once: true });
            existingScript.addEventListener('error', reject, { once: true });
        });
    }

    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.id = TURNSTILE_SCRIPT_ID;
        script.src = 'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit';
        script.async = true;
        script.defer = true;
        script.onload = resolve;
        script.onerror = reject;
        document.body.appendChild(script);
    });
}

function TurnstileWidget({ siteKey, onVerify, onExpire }) {
    const containerRef = useRef(null);
    const widgetIdRef = useRef(null);

    useEffect(() => {
        let disposed = false;

        if (!siteKey || !containerRef.current) {
            return undefined;
        }

        loadTurnstileScript().then(() => {
            if (disposed || !window.turnstile || !containerRef.current) {
                return;
            }

            widgetIdRef.current = window.turnstile.render(containerRef.current, {
                sitekey: siteKey,
                callback: token => onVerify?.(token),
                'expired-callback': () => onExpire?.(),
                'error-callback': () => onExpire?.(),
            });
        });

        return () => {
            disposed = true;
            if (window.turnstile && widgetIdRef.current) {
                window.turnstile.remove(widgetIdRef.current);
            }
            widgetIdRef.current = null;
        };
    }, [siteKey, onVerify, onExpire]);

    if (!siteKey) {
        return (
            <div className="turnstile-missing">
                Captcha is not configured.
            </div>
        );
    }

    return <div ref={containerRef} className="turnstile-widget" />;
}

export default TurnstileWidget;
