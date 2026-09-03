import { Fragment, useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { gsap } from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';
import AnimatedPageShell from '../../components/AnimatedPageShell';
import { usePageMotion } from '../../animations/usePageMotion';
import ManagedContentModal from '../../components/ManagedContentModal/ManagedContentModal';
import PublicManagedCardDecor, { getManagedCardPresentation } from '../../components/ManagedHomeCard/PublicManagedCardDecor';
import { activePositionPath, viewReferrerPath } from '../../Constants/Constants';
import Modal from '../../components/Modal/Modal';
import HomePromoModal from '../../components/HomePromoModal/HomePromoModal';
import ReferralFooter from '../../components/ReferralFooter/ReferralFooter';
import flowReferCandidate from '../../assets/images/flow-refer-candidate.svg';
import flowCandidateAdvance from '../../assets/images/flow-candidate-advance.svg';
import flowGetHired from '../../assets/images/flow-get-hired.svg';
import flowReceiveIncentive from '../../assets/images/flow-receive-incentive.svg';
import flowArrow from '../../assets/home-flow/flow-arrow.svg';
import flowBackground from '../../assets/home-flow/flow-background.jpg';
import { FiChevronLeft, FiChevronRight } from 'react-icons/fi';
import { fetchContentCards } from '../../services/contentService';
import { CONTENT_SECTIONS, DEFAULT_HOME_CONTENT_CARDS, getContentIcon } from '../../content/homeContentConfig';
import './HomePage.css';

gsap.registerPlugin(ScrollTrigger);

const decodeHtmlEntities = (value = '') => {
    if (!value) {
        return '';
    }

    if (typeof document !== 'undefined') {
        const textarea = document.createElement('textarea');
        textarea.innerHTML = value;
        return textarea.value;
    }

    return value
        .replace(/&nbsp;/gi, ' ')
        .replace(/&amp;/gi, '&')
        .replace(/&quot;/gi, '"')
        .replace(/&#39;/gi, "'")
        .replace(/&lt;/gi, '<')
        .replace(/&gt;/gi, '>');
};

const stripHtml = (html = '') =>
    decodeHtmlEntities(html.replace(/<[^>]*>/g, ' ')).replace(/\s+/g, ' ').trim();

const mapManagedCardToHomeCard = (card) => ({
    ...card,
    badge: card.badgeText,
    body: stripHtml(card.descriptionHtml),
    cta: card.buttonText,
    date: card.dateText,
    variant: card.badgeVariant,
    icon: getContentIcon(card),
    modalKey: card.actionType === 'modal' ? card.actionValue : '',
    to: card.actionType === 'route' ? card.actionValue : '',
    href: card.actionType === 'url' ? card.actionValue : '',
    detailCard: card.actionType === 'detail' ? card : null,
    presentation: getManagedCardPresentation(card),
});

const spotlightCards = DEFAULT_HOME_CONTENT_CARDS
    .filter((card) => card.section === CONTENT_SECTIONS.spotlight)
    .sort((a, b) => (a.displayOrder || 0) - (b.displayOrder || 0))
    .map(mapManagedCardToHomeCard);

const newsCards = DEFAULT_HOME_CONTENT_CARDS
    .filter((card) => card.section === CONTENT_SECTIONS.programNews)
    .sort((a, b) => (a.displayOrder || 0) - (b.displayOrder || 0))
    .map(mapManagedCardToHomeCard);

const flowSteps = [
    {
        title: 'Refer a candidate',
        icon: flowReferCandidate,
    },
    {
        title: 'The candidate advance in the process',
        icon: flowCandidateAdvance,
    },
    {
        title: 'Get hired',
        icon: flowGetHired,
    },
    {
        title: 'You receive your incentive',
        icon: flowReceiveIncentive,
    },
];

const HomePage = () => {
    const pageRef = useRef(null);
    const flowConnectorHeadRefs = useRef([]);
    const newsPinRef = useRef(null);
    const newsViewportRef = useRef(null);
    const newsTrackRef = useRef(null);
    const newsCardRefs = useRef([]);
    const newsBackdropRefs = useRef([]);
    const newsScrollTriggerRef = useRef(null);
    const newsActiveIndexRef = useRef(0);
    const navigate = useNavigate();
    const [activeModal, setActiveModal] = useState(null);
    const [activeManagedCard, setActiveManagedCard] = useState(null);
    const [managedSpotlightCards, setManagedSpotlightCards] = useState(spotlightCards);
    const [managedNewsCards, setManagedNewsCards] = useState(newsCards);
    const memoizedSpotlight = useMemo(() => managedSpotlightCards, [managedSpotlightCards]);
    const memoizedNews = useMemo(() => managedNewsCards, [managedNewsCards]);
    const visibleSpotlightCards = useMemo(
        () => memoizedSpotlight.filter((card) => card.isPublished !== false),
        [memoizedSpotlight],
    );
    const featuredSpotlightCards = useMemo(() => visibleSpotlightCards.slice(0, 3), [visibleSpotlightCards]);
    const overflowSpotlightCards = useMemo(() => visibleSpotlightCards.slice(3), [visibleSpotlightCards]);
    const visibleNewsCards = useMemo(
        () => memoizedNews.filter((card) => card.isPublished !== false),
        [memoizedNews],
    );
    const memoizedFlow = useMemo(() => flowSteps, []);
    usePageMotion(pageRef, []);

    useEffect(() => {
        let isMounted = true;

        const loadManagedCards = async () => {
            try {
                const response = await fetchContentCards();
                const items = Array.isArray(response?.data) ? response.data : [];

                if (!isMounted || items.length === 0) {
                    return;
                }

                const spotlight = items
                    .filter((card) => card.section === CONTENT_SECTIONS.spotlight)
                    .sort((a, b) => (a.displayOrder || 0) - (b.displayOrder || 0))
                    .map(mapManagedCardToHomeCard);

                const programNews = items
                    .filter((card) => card.section === CONTENT_SECTIONS.programNews)
                    .sort((a, b) => (a.displayOrder || 0) - (b.displayOrder || 0))
                    .map(mapManagedCardToHomeCard);

                if (spotlight.length > 0) {
                    setManagedSpotlightCards(spotlight);
                }

                if (programNews.length > 0) {
                    setManagedNewsCards(programNews);
                }
            } catch {
                // Keep default Home cards when managed content is unavailable.
            }
        };

        loadManagedCards();

        return () => {
            isMounted = false;
        };
    }, []);

    useEffect(() => {
        const connectorHeads = flowConnectorHeadRefs.current.filter(Boolean);

        if (!connectorHeads.length) {
            return undefined;
        }

        const headTweens = connectorHeads.map((head, index) =>
            gsap.to(head, {
                x: 3,
                scale: 1.03,
                transformOrigin: 'center center',
                duration: 1.1,
                ease: 'sine.inOut',
                yoyo: true,
                repeat: -1,
                delay: 0.2 + index * 0.1,
            })
        );

        return () => {
            headTweens.forEach((item) => item.kill());
        };
    }, []);

    useEffect(() => {
        const blobs = newsBackdropRefs.current.filter(Boolean);

        if (!blobs.length) {
            return undefined;
        }

        const ctx = gsap.context(() => {
            blobs.forEach((blob, index) => {
                gsap.to(blob, {
                    x: index === 1 ? -24 : 24,
                    y: index === 0 ? 18 : index === 1 ? -14 : 12,
                    scale: index === 2 ? 1.08 : 1.14,
                    rotate: index === 1 ? -8 : 8,
                    duration: 6 + index,
                    ease: 'sine.inOut',
                    repeat: -1,
                    yoyo: true,
                });
            });
        }, pageRef);

        return () => ctx.revert();
    }, []);

    useLayoutEffect(() => {
        newsCardRefs.current = newsCardRefs.current.slice(0, visibleNewsCards.length);

        const pinSection = newsPinRef.current;
        const track = newsTrackRef.current;
        const cards = newsCardRefs.current.filter(Boolean);
        const viewport = newsViewportRef.current;

        if (!pinSection || !viewport || !track || cards.length === 0) {
            return undefined;
        }

        const mm = gsap.matchMedia();

        mm.add(
            {
                isDesktop: '(min-width: 1101px)',
                isMobile: '(max-width: 1100px)',
                reduceMotion: '(prefers-reduced-motion: reduce)',
            },
            (context) => {
                const { isDesktop, isMobile, reduceMotion } = context.conditions;
                const clearDesktopState = () => {
                    newsScrollTriggerRef.current = null;
                    newsActiveIndexRef.current = 0;
                    gsap.set(track, { clearProps: 'transform' });
                    gsap.set(cards, {
                        clearProps:
                            'opacity,scale,zIndex,pointerEvents,x,y,xPercent,yPercent,rotateY,rotateZ,filter,boxShadow',
                    });
                    gsap.set(track, { clearProps: 'paddingLeft,paddingRight' });
                    cards.forEach((card) => card.classList.remove('is-active'));
                };

                if (!isDesktop || isMobile || reduceMotion) {
                    clearDesktopState();
                    return undefined;
                }

                if (cards.length < 2) {
                    clearDesktopState();
                    cards[0]?.classList.add('is-active');
                    return undefined;
                }

                const ctx = gsap.context(() => {
                    let activeIndex = -1;
                    let maxTranslate = 0;
                    let snapPoints = [0];
                    let cardCenters = [];

                    const setActiveCard = (index) => {
                        if (index === activeIndex) {
                            return;
                        }

                        activeIndex = index;
                        newsActiveIndexRef.current = index;

                        cards.forEach((card, cardIndex) => {
                            const isActive = cardIndex === index;
                            card.classList.toggle('is-active', isActive);
                            gsap.to(card, {
                                scale: isActive ? 1 : 0.94,
                                opacity: isActive ? 1 : 0.5,
                                y: isActive ? 0 : 8,
                                filter: isActive ? 'blur(0px)' : 'blur(1px)',
                                boxShadow: isActive
                                    ? '0 28px 58px rgba(11, 33, 53, 0.18)'
                                    : '0 12px 24px rgba(11, 33, 53, 0.09)',
                                duration: 0.36,
                                ease: 'power2.out',
                                overwrite: 'auto',
                            });
                        });
                    };

                    const syncMetrics = () => {
                        const viewportWidth = viewport.clientWidth;
                        const viewportCenter = viewportWidth / 2;
                        const firstCard = cards[0];
                        const lastCard = cards[cards.length - 1];
                        const cardWidth = firstCard?.offsetWidth || 0;
                        const sidePadding = Math.max((viewportWidth - cardWidth) / 2, 0);

                        gsap.set(track, { paddingLeft: sidePadding, paddingRight: sidePadding, x: 0 });

                        cardCenters = cards.map((card) => card.offsetLeft + card.offsetWidth / 2);
                        maxTranslate = Math.max(track.scrollWidth - viewportWidth, 0);
                        snapPoints = cards.length > 1
                            ? cardCenters.map((center) => {
                                  const position = Math.max(0, Math.min(center - viewportCenter, maxTranslate));
                                  return maxTranslate === 0 ? 0 : position / maxTranslate;
                              })
                            : [0];

                        gsap.set(cards, { clearProps: 'x,y,xPercent,yPercent,rotateY,rotateZ,zIndex,pointerEvents' });
                        setActiveCard(0);
                    };

                    syncMetrics();

                    const tween = gsap.to(track, {
                        x: () => -maxTranslate,
                        ease: 'none',
                        paused: true,
                    });

                    const scrollTrigger = ScrollTrigger.create({
                        id: 'home-program-news-scroll',
                        trigger: pinSection,
                        animation: tween,
                        start: 'center center',
                        end: () => `+=${Math.max(maxTranslate * 1.15, viewport.clientWidth * 0.95)}`,
                        pin: true,
                        pinSpacing: true,
                        scrub: 0.9,
                        anticipatePin: 1,
                        invalidateOnRefresh: true,
                        snap: cards.length > 1
                            ? {
                                  snapTo: (value) => {
                                      let closest = snapPoints[0];
                                      let closestDistance = Math.abs(value - closest);
                                      snapPoints.forEach((point) => {
                                          const distance = Math.abs(value - point);
                                          if (distance < closestDistance) {
                                              closest = point;
                                              closestDistance = distance;
                                          }
                                      });
                                      return closest;
                                  },
                                  duration: { min: 0.16, max: 0.32 },
                                  ease: 'power2.out',
                              }
                            : false,
                        onRefreshInit: () => {
                            syncMetrics();
                            tween.invalidate();
                        },
                        onUpdate: (self) => {
                            const currentTranslate = self.progress * maxTranslate;
                            const viewportCenter = viewport.clientWidth / 2;
                            let closestIndex = 0;
                            let closestDistance = Number.POSITIVE_INFINITY;

                            cardCenters.forEach((center, index) => {
                                const distance = Math.abs(center - currentTranslate - viewportCenter);
                                if (distance < closestDistance) {
                                    closestDistance = distance;
                                    closestIndex = index;
                                }
                            });

                            setActiveCard(closestIndex);
                        },
                    });

                    newsScrollTriggerRef.current = scrollTrigger;
                    requestAnimationFrame(() => ScrollTrigger.refresh());
                }, pinSection);

                return () => {
                    clearDesktopState();
                    ctx.revert();
                };
            },
        );

        return () => mm.revert();
    }, [visibleNewsCards.length]);

    const handleCardAction = (card) => {
        if (card.detailCard) {
            setActiveManagedCard(card.detailCard);
            return;
        }

        if (card.modalKey) {
            setActiveModal(card.modalKey);
            return;
        }

        if (card.href) {
            window.open(card.href, '_blank', 'noopener,noreferrer');
            return;
        }

        if (card.to) {
            navigate(card.to);
        }
    };

    const handleNewsArrowClick = (direction) => {
        const cards = newsCardRefs.current.filter(Boolean);
        const track = newsTrackRef.current;
        const viewport = newsViewportRef.current;
        const scrollTrigger = newsScrollTriggerRef.current || ScrollTrigger.getById('home-program-news-scroll');

        if (cards.length < 2) {
            return;
        }

        const activeCardIndex = cards.findIndex((card) => card.classList.contains('is-active'));
        const currentIndex = activeCardIndex >= 0 ? activeCardIndex : newsActiveIndexRef.current;
        const nextIndex = Math.max(0, Math.min(currentIndex + direction, cards.length - 1));
        newsActiveIndexRef.current = nextIndex;

        if (scrollTrigger) {
            const progress = nextIndex / (cards.length - 1);
            const top = scrollTrigger.start + (scrollTrigger.end - scrollTrigger.start) * progress;

            window.scrollTo({
                top,
                behavior: 'smooth',
            });
            return;
        }

        if (!track || !viewport) {
            return;
        }

        const viewportCenter = viewport.clientWidth / 2;
        const targetCard = cards[nextIndex];
        const targetCenter = targetCard.offsetLeft + targetCard.offsetWidth / 2;
        const maxTranslate = Math.max(track.scrollWidth - viewport.clientWidth, 0);
        const nextTranslate = Math.max(0, Math.min(targetCenter - viewportCenter, maxTranslate));

        cards.forEach((card, index) => card.classList.toggle('is-active', index === nextIndex));
        gsap.to(track, {
            x: -nextTranslate,
            duration: 0.45,
            ease: 'power2.out',
        });
    };

    return (
        <AnimatedPageShell ref={pageRef} className="home-page-shell">
            <div className="home-page">
                <Modal
                    showModal={Boolean(activeModal)}
                    handleCloseClick={() => setActiveModal(null)}
                    size="xl"
                    centered
                    backdrop="static"
                    keyboard={false}
                >
                    <HomePromoModal modalKey={activeModal} />
                </Modal>
                <ManagedContentModal card={activeManagedCard} open={Boolean(activeManagedCard)} onClose={() => setActiveManagedCard(null)} />

                <section className="home-referral-stage">
                    <section className="home-showcase">
                        <div className="home-showcase-copy">
                            <p className="home-showcase-kicker" data-hero>
                                Referral Program
                            </p>
                            <h1 className="home-showcase-title" data-hero>
                                Recommend talent. Earn benefits.
                            </h1>
                            <p className="home-showcase-text" data-hero>
                                Stay informed about the program updates and participate actively with a
                                cleaner, more action-driven referral experience.
                            </p>
                            <div className="home-showcase-actions" data-hero>
                                <button
                                    type="button"
                                    className="home-primary-cta gsap-button"
                                    onClick={() => navigate(viewReferrerPath)}
                                >
                                    Refer Now
                                </button>
                            </div>
                        </div>
                    </section>

                    <section className="home-spotlight-grid home-spotlight-stage-grid">
                        {featuredSpotlightCards.map((card, index) => (
                            <article
                                key={card.id || `${card.title}-${index}`}
                                className={`home-spotlight-card home-spotlight-${card.variant} gsap-card`}
                                data-card
                                style={card.presentation?.surfaceStyle}
                            >
                                <PublicManagedCardDecor card={card} />
                                <div className="home-spotlight-icon-wrap">
                                    <img src={card.icon} alt="" aria-hidden="true" className="home-spotlight-icon" />
                                </div>
                                <div className="home-spotlight-copy">
                                    <h3 style={card.presentation?.titleStyle}>{card.title}</h3>
                                    <p style={card.presentation?.bodyStyle}>{card.body}</p>
                                </div>
                                <button
                                    type="button"
                                    className="home-inline-cta gsap-button"
                                    onClick={() => handleCardAction(card)}
                                    style={card.presentation?.buttonStyle}
                                >
                                    {card.cta}
                                    <FiChevronRight />
                                </button>
                            </article>
                        ))}
                    </section>

                    {overflowSpotlightCards.length > 0 && (
                        <section className="home-spotlight-grid home-spotlight-overflow-grid">
                            {overflowSpotlightCards.map((card, index) => (
                                <article
                                    key={card.id || `${card.title}-overflow-${index}`}
                                    className={`home-spotlight-card home-spotlight-${card.variant} gsap-card`}
                                    data-card
                                    style={card.presentation?.surfaceStyle}
                                >
                                    <PublicManagedCardDecor card={card} />
                                    <div className="home-spotlight-icon-wrap">
                                        <img src={card.icon} alt="" aria-hidden="true" className="home-spotlight-icon" />
                                    </div>
                                    <div className="home-spotlight-copy">
                                        <h3 style={card.presentation?.titleStyle}>{card.title}</h3>
                                        <p style={card.presentation?.bodyStyle}>{card.body}</p>
                                    </div>
                                    <button
                                        type="button"
                                        className="home-inline-cta gsap-button"
                                        onClick={() => handleCardAction(card)}
                                        style={card.presentation?.buttonStyle}
                                    >
                                        {card.cta}
                                        <FiChevronRight />
                                    </button>
                                </article>
                            ))}
                        </section>
                    )}
                </section>

                <section className="home-section-block home-news-block">
                    <div ref={newsPinRef} className="home-news-pin-wrap">
                        <div className="home-news-stage-shell">
                            <div className="home-news-backdrop" aria-hidden="true">
                                {[0, 1, 2].map((blobIndex) => (
                                    <span
                                        key={blobIndex}
                                        ref={(el) => {
                                            newsBackdropRefs.current[blobIndex] = el;
                                        }}
                                        className={`home-news-blob home-news-blob-${blobIndex + 1}`}
                                    />
                                ))}
                            </div>
                            <div className="home-news-stage-header">
                                <h2>Program News</h2>
                            </div>
                            <div className="home-news-carousel">
                                <button
                                    type="button"
                                    className="home-news-arrow home-news-arrow-left gsap-button"
                                    onClick={() => handleNewsArrowClick(-1)}
                                    aria-label="Previous program news"
                                >
                                    <FiChevronLeft aria-hidden="true" />
                                </button>
                                <div ref={newsViewportRef} className="home-news-viewport">
                                    <div ref={newsTrackRef} className="home-news-track">
                                        {visibleNewsCards.map((item, index) => (
                                            <article
                                                key={item.id || `${item.title}-${index}`}
                                                ref={(el) => {
                                                    newsCardRefs.current[index] = el;
                                                }}
                                                className="home-news-card home-news-slide"
                                                style={item.presentation?.surfaceStyle}
                                            >
                                                <PublicManagedCardDecor card={item} />
                                                <span
                                                    className={`home-news-badge home-news-${item.variant}`}
                                                    style={item.presentation?.badgeStyle}
                                                >
                                                    {item.badge}
                                                </span>
                                                <h3 className="home-news-card-title" style={item.presentation?.titleStyle}>{item.title}</h3>
                                                <p className="home-news-card-text" style={item.presentation?.bodyStyle}>{item.body}</p>
                                                <div className="home-news-footer">
                                                    <span style={item.presentation?.dateStyle}>{item.date}</span>
                                                    <button
                                                        className="home-inline-cta gsap-button"
                                                        type="button"
                                                        onClick={() => handleCardAction(item)}
                                                        style={item.presentation?.buttonStyle}
                                                    >
                                                        {item.cta || 'Read More'}
                                                        <FiChevronRight />
                                                    </button>
                                                </div>
                                            </article>
                                        ))}
                                    </div>
                                </div>
                                <button
                                    type="button"
                                    className="home-news-arrow home-news-arrow-right gsap-button"
                                    onClick={() => handleNewsArrowClick(1)}
                                    aria-label="Next program news"
                                >
                                    <FiChevronRight aria-hidden="true" />
                                </button>
                            </div>
                        </div>
                    </div>
                </section>

                <section className="home-section-block">
                    <div className="home-section-heading" data-reveal>
                        <h2>Here&apos;s how the program works</h2>
                    </div>
                    <div
                        className="home-flow-card"
                        data-card
                        style={{ backgroundImage: `url(${flowBackground})` }}
                    >
                        <div className="home-flow-grid">
                            {memoizedFlow.map((step, index) => (
                                <Fragment key={step.title}>
                                    <div className="home-flow-step">
                                        <div className="home-flow-icon">
                                            <img src={step.icon} alt="" className="home-flow-svg" aria-hidden="true" />
                                        </div>
                                        <p>{step.title}</p>
                                    </div>
                                    {index < memoizedFlow.length - 1 && (
                                        <div className="home-flow-connector" aria-hidden="true">
                                            <img
                                                ref={(el) => {
                                                    flowConnectorHeadRefs.current[index] = el;
                                                }}
                                                src={flowArrow}
                                                alt=""
                                                className="home-flow-arrow"
                                            />
                                        </div>
                                    )}
                                </Fragment>
                            ))}
                        </div>
                    </div>
                </section>
                <ReferralFooter />
            </div>
        </AnimatedPageShell>
    );
};

export default HomePage;
