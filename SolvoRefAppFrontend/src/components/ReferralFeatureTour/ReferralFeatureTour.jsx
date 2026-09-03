import { useCallback, useEffect, useMemo, useState } from 'react';
import { FiArrowLeft, FiArrowRight, FiCheck, FiCopy, FiRefreshCw, FiX } from 'react-icons/fi';
import './ReferralFeatureTour.css';

const TOUR_STORAGE_KEY = 'solvo-referral-link-tour-v1';

function getTargetRect(selector) {
    const target = document.querySelector(selector);
    if (!target) {
        return null;
    }

    const rect = target.getBoundingClientRect();
    return {
        top: rect.top,
        left: rect.left,
        width: rect.width,
        height: rect.height,
    };
}

function getTargetPreview(selector) {
    const target = document.querySelector(selector);
    if (!target) {
        return null;
    }

    if (target.classList.contains('status-group')) {
        const image = target.querySelector('img');
        return {
            type: 'status',
            label: target.querySelector('p')?.textContent?.trim() || '',
            count: target.querySelector('h4')?.textContent?.trim() || '',
            imageSrc: image?.getAttribute('src') || '',
            imageAlt: image?.getAttribute('alt') || '',
        };
    }

    return {
        type: target.tagName.toLowerCase() === 'button' ? 'button' : 'generic',
        text: target.textContent?.trim() || '',
        className: target.className || '',
        tagName: target.tagName.toLowerCase(),
    };
}

function getTooltipPosition(rect) {
    const isMobile = window.innerWidth <= 768;
    const margin = isMobile ? 12 : 18;
    const estimatedHeight = isMobile ? 350 : 270;
    const tooltipWidth = Math.min(isMobile ? 360 : 380, window.innerWidth - (isMobile ? 24 : 32));
    const placeBelow = rect.top + rect.height + estimatedHeight + margin < window.innerHeight;
    const top = placeBelow
        ? rect.top + rect.height + margin
        : Math.max(12, rect.top - estimatedHeight - margin);
    const left = Math.min(
        Math.max(isMobile ? 12 : 16, rect.left + rect.width / 2 - tooltipWidth / 2),
        window.innerWidth - tooltipWidth - (isMobile ? 12 : 16),
    );

    return {
        top,
        left,
        width: tooltipWidth,
        placement: placeBelow ? 'below' : 'above',
    };
}

function ReferralFeatureTour({ runSignal = 0 }) {
    const steps = useMemo(() => [
        {
            selector: '[data-tour="copy-link"]',
            eyebrow: 'New sharing flow',
            title: 'Share your personal referral link',
            body: 'Copy one reusable link and send it directly to a candidate. When they submit the public form, the referral stays tied to you.',
            icon: FiCopy,
        },
        {
            selector: '[data-tour="new-referral"]',
            eyebrow: 'Current flow stays',
            title: 'Create referrals manually when you need to',
            body: 'The existing New Referral flow is still available for cases where you prefer to fill out the candidate information yourself.',
            icon: FiRefreshCw,
        },
        {
            selector: '[data-tour="referral-status"]',
            eyebrow: 'Track progress',
            title: 'Follow every referral from the same dashboard',
            body: 'Public-link referrals and manually created referrals land in the same list, with the same statuses and detail view.',
            icon: FiCheck,
        },
    ], []);

    const [isOpen, setIsOpen] = useState(false);
    const [stepIndex, setStepIndex] = useState(0);
    const [targetRect, setTargetRect] = useState(null);
    const [targetPreview, setTargetPreview] = useState(null);

    const currentStep = steps[stepIndex];
    const Icon = currentStep?.icon || FiCheck;
    const tooltipPosition = targetRect ? getTooltipPosition(targetRect) : null;

    const closeTour = useCallback((persist = true) => {
        setIsOpen(false);
        if (persist) {
            localStorage.setItem(TOUR_STORAGE_KEY, 'seen');
        }
    }, []);

    const measureStep = useCallback(() => {
        if (!isOpen || !currentStep) {
            return;
        }

        const target = document.querySelector(currentStep.selector);
        if (!target) {
            setTargetRect(null);
            return;
        }

        target.scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'center' });
        window.setTimeout(() => {
            setTargetRect(getTargetRect(currentStep.selector));
            setTargetPreview(getTargetPreview(currentStep.selector));
        }, 260);
    }, [currentStep, isOpen]);

    useEffect(() => {
        if (localStorage.getItem(TOUR_STORAGE_KEY) === 'seen') {
            return;
        }

        const timeout = window.setTimeout(() => {
            setIsOpen(true);
            setStepIndex(0);
        }, 900);

        return () => window.clearTimeout(timeout);
    }, []);

    useEffect(() => {
        if (runSignal === 0) {
            return;
        }

        setIsOpen(true);
        setStepIndex(0);
        localStorage.removeItem(TOUR_STORAGE_KEY);
    }, [runSignal]);

    useEffect(() => {
        measureStep();
    }, [measureStep, stepIndex]);

    useEffect(() => {
        if (!isOpen) {
            return undefined;
        }

        const handleUpdate = () => {
            setTargetRect(getTargetRect(currentStep.selector));
            setTargetPreview(getTargetPreview(currentStep.selector));
        };
        window.addEventListener('resize', handleUpdate);
        window.addEventListener('scroll', handleUpdate, true);

        return () => {
            window.removeEventListener('resize', handleUpdate);
            window.removeEventListener('scroll', handleUpdate, true);
        };
    }, [currentStep, isOpen]);

    useEffect(() => {
        if (!isOpen) {
            return undefined;
        }

        const handleKeyDown = (event) => {
            if (event.key === 'Escape') {
                closeTour();
            }
        };

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [closeTour, isOpen]);

    const renderTargetPreview = () => {
        if (!targetPreview) {
            return null;
        }

        if (targetPreview.type === 'status') {
            return (
                <div className="referral-tour__status-preview">
                    {targetPreview.imageSrc ? (
                        <img src={targetPreview.imageSrc} alt={targetPreview.imageAlt} />
                    ) : null}
                    <div>
                        <p>{targetPreview.label}</p>
                        <strong>{targetPreview.count}</strong>
                    </div>
                </div>
            );
        }

        if (targetPreview.type === 'button') {
            return (
                <button
                    type="button"
                    className={targetPreview.className}
                    tabIndex={-1}
                >
                    {targetPreview.text}
                </button>
            );
        }

        return (
            <div className={targetPreview.className}>
                {targetPreview.text}
            </div>
        );
    };

    if (!isOpen || !currentStep || !targetRect || !tooltipPosition) {
        return null;
    }

    const paddedRect = {
        top: Math.max(10, targetRect.top - 10),
        left: Math.max(10, targetRect.left - 10),
        width: targetRect.width + 20,
        height: targetRect.height + 20,
    };
    const isLastStep = stepIndex === steps.length - 1;

    return (
        <div className="referral-tour" role="dialog" aria-modal="true" aria-label="Referral link feature tour">
            <div className="referral-tour__scrim" />
            <div
                className="referral-tour__spotlight"
                style={{
                    top: paddedRect.top,
                    left: paddedRect.left,
                    width: paddedRect.width,
                    height: paddedRect.height,
                }}
            />
            {targetPreview ? (
                <div
                    className="referral-tour__target-preview"
                    style={{
                        top: targetRect.top,
                        left: targetRect.left,
                        width: targetRect.width,
                        height: targetRect.height,
                    }}
                    aria-hidden="true"
                >
                    {renderTargetPreview()}
                </div>
            ) : null}
            <div
                className={`referral-tour__card referral-tour__card--${tooltipPosition.placement}`}
                style={{
                    top: tooltipPosition.top,
                    left: tooltipPosition.left,
                    width: tooltipPosition.width,
                }}
            >
                <button
                    className="referral-tour__close"
                    type="button"
                    aria-label="Close tour"
                    onClick={() => closeTour()}
                >
                    <FiX />
                </button>
                <div className="referral-tour__icon">
                    <Icon />
                </div>
                <p className="referral-tour__eyebrow">{currentStep.eyebrow}</p>
                <h3>{currentStep.title}</h3>
                <p className="referral-tour__body">{currentStep.body}</p>
                <div className="referral-tour__progress" aria-hidden="true">
                    {steps.map((step, index) => (
                        <span
                            key={step.selector}
                            className={index === stepIndex ? 'active' : ''}
                        />
                    ))}
                </div>
                <div className="referral-tour__actions">
                    <button
                        type="button"
                        className="referral-tour__ghost"
                        onClick={() => closeTour()}
                    >
                        Skip
                    </button>
                    <div className="referral-tour__nav">
                        <button
                            type="button"
                            className="referral-tour__circle"
                            disabled={stepIndex === 0}
                            aria-label="Previous tour step"
                            onClick={() => setStepIndex((prev) => Math.max(0, prev - 1))}
                        >
                            <FiArrowLeft />
                        </button>
                        <button
                            type="button"
                            className="referral-tour__primary"
                            onClick={() => {
                                if (isLastStep) {
                                    closeTour();
                                    return;
                                }

                                setStepIndex((prev) => prev + 1);
                            }}
                        >
                            {isLastStep ? 'Finish' : 'Next'}
                            {!isLastStep ? <FiArrowRight /> : <FiCheck />}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default ReferralFeatureTour;
