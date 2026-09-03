import { useEffect, useRef, useState } from 'react';
import confetti from 'canvas-confetti';
import { useParams } from 'react-router-dom';
import { referralApi } from '../../Constants/Constants';
import { useApi } from '../../components/CustomHook/UseApi';
import FormReferidos from '../../components/ReferidoComponent/FormReferidos';
import Spinner from '../../components/Spinner/Spinner';
import './PublicReferralPage.css';

function PublicReferralPage() {
    const { token } = useParams();
    const { request } = useApi();
    const confettiCanvasRef = useRef(null);
    const [isLoading, setIsLoading] = useState(true);
    const [referrerName, setReferrerName] = useState('');
    const [isValidLink, setIsValidLink] = useState(false);
    const [submitted, setSubmitted] = useState(false);

    useEffect(() => {
        const validateLink = async () => {
            setIsLoading(true);
            const response = await request(
                `${process.env.REACT_APP_API}/api/${referralApi}/public/${token}`,
                { silentErrors: true },
            );

            setIsValidLink(response != null);
            setReferrerName(response?.referrerName || '');
            setIsLoading(false);
        };

        validateLink();
    }, [token]);

    useEffect(() => {
        if (!submitted) {
            return undefined;
        }

        const shouldReduceMotion = window.matchMedia(
            '(prefers-reduced-motion: reduce)',
        ).matches;

        if (shouldReduceMotion) {
            return undefined;
        }

        if (!confettiCanvasRef.current) {
            return undefined;
        }

        const fireConfetti = confetti.create(confettiCanvasRef.current, {
            resize: true,
            useWorker: false,
        });

        const confettiOptions = {
            particleCount: 90,
            spread: 68,
            startVelocity: 42,
            ticks: 220,
            gravity: 0.9,
            scalar: 0.9,
            colors: ['#ff7a2f', '#20bfd0', '#48c56a', '#f4d83f', '#ff5d82'],
        };

        fireConfetti({
            ...confettiOptions,
            angle: 60,
            origin: { x: 0.12, y: 0.35 },
        });
        fireConfetti({
            ...confettiOptions,
            angle: 120,
            origin: { x: 0.88, y: 0.35 },
        });

        const centerBurst = window.setTimeout(() => {
            fireConfetti({
                particleCount: 70,
                spread: 92,
                startVelocity: 34,
                ticks: 190,
                gravity: 0.8,
                scalar: 0.8,
                origin: { x: 0.5, y: 0.26 },
                colors: ['#7be3bd', '#27c86f', '#20bfd0', '#ff7a2f'],
            });
        }, 220);

        return () => {
            window.clearTimeout(centerBurst);
            fireConfetti.reset();
        };
    }, [submitted]);

    return (
        <div className="public-referral-page">
            <Spinner isLoading={isLoading} />
            <main className="public-referral-shell">
                {isValidLink && !submitted ? (
                    <>
                        <section className="public-referral-heading">
                            <p>Solvo Referral Program</p>
                            <h1>Complete your application</h1>
                            {referrerName ? (
                                <span>Shared by {referrerName}</span>
                            ) : null}
                        </section>
                        <section className="public-referral-form">
                            <FormReferidos
                                isPublic
                                referralToken={token}
                                setHandleClose={(status) => {
                                    if (!status) {
                                        setSubmitted(true);
                                    }
                                }}
                            />
                        </section>
                    </>
                ) : null}

                {isValidLink && submitted ? (
                    <section className="public-referral-state public-referral-success">
                        <canvas
                            ref={confettiCanvasRef}
                            className="success-confetti-canvas"
                            aria-hidden="true"
                        />
                        <div className="success-card">
                            <div className="success-copy">
                                <div className="success-mini-mark" aria-hidden="true">
                                    <span className="success-star success-star-blue" />
                                    <span className="success-star success-star-gold" />
                                    <div className="success-small-check">
                                        <svg viewBox="0 0 48 48" focusable="false">
                                            <path d="M19.3 31.2 11.4 23.3 8.8 25.9 19.3 36.4 40.2 15.5 37.6 12.9z" />
                                        </svg>
                                    </div>
                                    <span className="success-dot" />
                                </div>
                                <h1>
                                    <span>Referral</span>
                                    <span>submitted</span>
                                </h1>
                                <span className="success-title-line" />
                                <div className="success-message">
                                    <p>
                                        At Solvo, we love discovering new talent and connecting with people who can find a great professional opportunity with us.
                                    </p>
                                    <p>Our team will review the information.</p>
                                    <p>
                                        Thank you for trusting Solvo and for sharing this new opportunity with us!
                                    </p>
                                </div>
                            </div>
                            <div className="success-illustration" aria-hidden="true">
                                <span className="success-star success-hero-star-blue" />
                                <span className="success-star success-hero-star-teal" />
                                <span className="success-star success-hero-star-gold" />
                                <span className="success-hero-dot" />
                                <div className="success-check-wrap">
                                    <div className="success-check-pulse success-check-pulse-one" />
                                    <div className="success-check-pulse success-check-pulse-two" />
                                    <div className="success-check">
                                        <svg viewBox="0 0 48 48" focusable="false">
                                            <path d="M19.3 31.2 11.4 23.3 8.8 25.9 19.3 36.4 40.2 15.5 37.6 12.9z" />
                                        </svg>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </section>
                ) : null}

                {!isLoading && !isValidLink ? (
                    <section className="public-referral-state">
                        <h1>This referral link is not available</h1>
                        <p>Please ask the person who shared it to generate a new link.</p>
                    </section>
                ) : null}
            </main>
        </div>
    );
}

export default PublicReferralPage;
