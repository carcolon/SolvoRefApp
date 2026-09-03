import homeMascot from '../../assets/images/home-mascot.png';
import successEyebrow from '../../assets/home-modals/success/referral-program.svg';
import successTitle from '../../assets/home-modals/success/success-stories-title.svg';
import successLeftHero from '../../assets/home-modals/success/left-hero.svg';
import successLeftCaption from '../../assets/home-modals/success/left-caption.svg';
import successMeetTitle from '../../assets/home-modals/success/meet-title.svg';
import successBarranquillaTitle from '../../assets/home-modals/success/barranquilla-title.svg';
import successBarranquillaWinners from '../../assets/home-modals/success/barranquilla-winners.svg';
import successMedellinTitle from '../../assets/home-modals/success/medellin-title.svg';
import successMedellinWinners from '../../assets/home-modals/success/medellin-winners.svg';
import communityStoryImage from '../../assets/home-modals/community/community-story.svg';
import { HOME_PROMO_BUILTINS } from '../../content/homePromoBuiltins';
import './HomePromoModal.css';

const REFERRAL_TERMS_URL = 'https://onesourcecorp.sharepoint.com/sites/SolvoFC_Marketing/Shared%20Documents/Forms/AllItems.aspx?id=%2Fsites%2FSolvoFC%5FMarketing%2FShared%20Documents%2FMarketing%20%2D%20Solvo%2FReferidos%20Global%2FTyC%20%2D%20FAQs%20%28Mayo%29%2FT%C3%A9rminos%20y%20condiciones%20%2D%20FAQs&p=true&ct=1787770962277&or=Teams%2DHL&ga=1&LOF=1';

const modalContent = Object.fromEntries(
    Object.entries(HOME_PROMO_BUILTINS).map(([key, value]) => [
        key,
        {
            eyebrow: 'Referral Program',
            title: value.title,
            subtitle:
                key === 'incentive'
                    ? 'For every referred candidate who gets hired, you receive USD 100 for helping grow our Wolfpack.'
                    : key === 'update'
                      ? 'We want you to feel confident and fully informed about everything happening in the program.'
                      : key === 'campaign'
                        ? 'Here you\'ll find priority roles that need to be filled quickly, representing a great opportunity to refer with higher impact. Some of these positions may include additional incentives or special benefits due to their urgency.'
                        : '',
        },
    ]),
);

const HomePromoModal = ({ modalKey }) => {
    if (!modalKey || !modalContent[modalKey]) {
        return null;
    }

    const termConditionUrl = REFERRAL_TERMS_URL;
    const content = modalContent[modalKey];

    if (modalKey === 'incentive') {
        return (
            <div className="home-promo-modal">
                <p className="home-promo-modal__eyebrow">{content.eyebrow}</p>
                <h2 className="home-promo-modal__title">{content.title}</h2>
                <p className="home-promo-modal__subtitle">{content.subtitle}</p>
                <div className="home-promo-modal__divider" />

                <div className="home-promo-modal__hero-layout">
                    <img src={homeMascot} alt="" aria-hidden="true" className="home-promo-modal__mascot" />
                    <div className="home-promo-modal__hero-copy">
                        <p>
                            And this month, we&apos;re taking it to the next level with <strong>Squad Goals</strong>:
                            build your team (2 to 4 people per country) and compete to be the squad with the most
                            successful referrals. Each member must contribute at least one to win.
                        </p>
                        <div className="home-promo-modal__pill">
                            The prize? A pizza party to celebrate your team&apos;s success.
                        </div>
                        <p className="home-promo-modal__footnote">
                            Available in Colombia, Argentina, Mexico, Guatemala, Honduras, and Peru. Terms &
                            Conditions apply.
                        </p>
                    </div>
                </div>
            </div>
        );
    }

    if (modalKey === 'success') {
        return (
            <div className="home-promo-modal home-promo-modal--wide home-promo-modal--success">
                <div className="home-promo-modal__success-header">
                    <img
                        src={successEyebrow}
                        alt="Referral Program"
                        className="home-promo-modal__success-eyebrow"
                    />
                    <img
                        src={successTitle}
                        alt="Success Stories & Testimonials"
                        className="home-promo-modal__success-title"
                    />
                </div>
                <div className="home-promo-modal__divider" />

                <div className="home-promo-modal__success-grid">
                    <aside className="home-promo-modal__story-side">
                        <div className="home-promo-modal__story-figure">
                            <img
                                src={successLeftHero}
                                alt="Referral winners collage"
                                className="home-promo-modal__success-image"
                            />
                        </div>
                        <img
                            src={successLeftCaption}
                            alt="In Mérida, during the month of February, we had 3 winners of USD 50 bonuses for referring"
                            className="home-promo-modal__success-caption"
                        />
                        <div className="home-promo-modal__success-side-copy">
                            <div className="home-promo-modal__success-side-divider" />
                            <p className="home-promo-modal__success-copy">
                                When we recognize effort, we celebrate together through the <strong>Referral Program:</strong>
                            </p>
                            <div className="home-promo-modal__success-stat">
                                <span>
                                    An average of <strong>USD 4,600 per month</strong> distributed in bonuses globally.
                                </span>
                            </div>
                            <div className="home-promo-modal__success-stat">
                                <span>
                                    More than <strong>USD 2,000 in incentives</strong> awarded during 2025.
                                </span>
                            </div>
                        </div>
                    </aside>

                    <div className="home-promo-modal__success-main">
                        <img
                            src={successMeetTitle}
                            alt="Meet our 2025 flagship award winners"
                            className="home-promo-modal__success-meet-title"
                        />
                        <section className="home-promo-modal__success-group">
                            <img
                                src={successBarranquillaTitle}
                                alt="Double passes for the El Último Baile concert by Silvestre Dangond in Barranquilla"
                                className="home-promo-modal__success-group-title"
                            />
                            <img
                                src={successBarranquillaWinners}
                                alt="Barranquilla winners"
                                className="home-promo-modal__success-winners"
                            />
                        </section>
                        <section className="home-promo-modal__success-group">
                            <img
                                src={successMedellinTitle}
                                alt="Winners of double passes for the Súper Concierto at Feria de las Flores in Medellín"
                                className="home-promo-modal__success-group-title"
                            />
                            <img
                                src={successMedellinWinners}
                                alt="Medellín winners"
                                className="home-promo-modal__success-winners"
                            />
                        </section>
                    </div>
                </div>
            </div>
        );
    }

    if (modalKey === 'update') {
        return (
            <div className="home-promo-modal">
                <p className="home-promo-modal__eyebrow">{content.eyebrow}</p>
                <h2 className="home-promo-modal__title">{content.title}</h2>
                <p className="home-promo-modal__subtitle">{content.subtitle}</p>
                <div className="home-promo-modal__divider" />

                <div className="home-promo-modal__hero-layout">
                    <img src={homeMascot} alt="" aria-hidden="true" className="home-promo-modal__mascot" />
                    <div className="home-promo-modal__hero-copy">
                        <p>
                            That&apos;s why we encourage you to explore the latest updates to our <strong>Terms & Conditions</strong>, get a clearer understanding of how the benefits work, and discover new opportunities designed for you.
                        </p>
                        <p>
                            Take a moment to review the program guidelines and payment process, so you can truly make the most of every incentive available.
                        </p>
                        <div className="home-promo-modal__action-row">
                            <strong>
                                The more you know, the easier it becomes to participate, maximize your rewards, and keep growing with us.
                            </strong>
                            <a
                                className={`home-promo-modal__cta ${!termConditionUrl ? 'is-disabled' : ''}`}
                                href={termConditionUrl || '#'}
                                target="_blank"
                                rel="noreferrer"
                                onClick={(event) => {
                                    if (!termConditionUrl) {
                                        event.preventDefault();
                                    }
                                }}
                            >
                                View Terms & Conditions
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        );
    }

    if (modalKey === 'community') {
        return (
            <div className="home-promo-modal home-promo-modal--narrow">
                <p className="home-promo-modal__eyebrow">{content.eyebrow}</p>
                <h2 className="home-promo-modal__title">{content.title}</h2>
                <div className="home-promo-modal__divider" />

                <div className="home-promo-modal__community-card">
                    <div className="home-promo-modal__community-figure">
                        <img
                            src={communityStoryImage}
                            alt="Community story"
                            className="home-promo-modal__community-mascot"
                        />
                    </div>
                    <blockquote>
                        &ldquo;I honestly didn&apos;t expect it-I wasn&apos;t counting on this extra income. Thanks to the Referral Program!&rdquo;
                    </blockquote>
                    <p>
                        <strong>Esteban L</strong>, ganador de nuestro sorteo de USD 1,000 por referir el año pasado.
                    </p>
                </div>
            </div>
        );
    }

    return (
        <div className="home-promo-modal home-promo-modal--narrow">
            <p className="home-promo-modal__eyebrow">{content.eyebrow}</p>
            <h2 className="home-promo-modal__title">{content.title}</h2>
            <p className="home-promo-modal__subtitle">{content.subtitle}</p>
            <div className="home-promo-modal__divider" />

            <div className="home-promo-modal__campaign-card">
                <img src={homeMascot} alt="" aria-hidden="true" className="home-promo-modal__campaign-mascot" />
                <div className="home-promo-modal__campaign-copy">
                    <h3>Soulver</h3>
                    <p>If you can sell in English... this is your place.</p>
                    <p>We are looking for a Sales Representative.</p>
                    <div className="home-promo-modal__campaign-columns">
                        <div>
                            <strong>Location:</strong>
                            <span>Córdoba Capital (On-site)</span>
                        </div>
                        <div>
                            <strong>Profile:</strong>
                            <ul>
                                <li>Advanced English (C1)</li>
                                <li>Sales experience</li>
                                <li>Commercial, persuasive, and results-driven profile</li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default HomePromoModal;
