import Modal from '../ModalCopmponent/ModalLogger';
import { useLayoutEffect, useRef, useState } from 'react';
import { gsap } from 'gsap';
import { FiCheckCircle } from 'react-icons/fi';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../AuthContextComponent/AuthContext';
import logviw from '../../assets/images/referralImg.png';
import estrella from '../../assets/images/estrella.png';
import './NavComponent.css';
import { getInitials } from '../CostantsComponent/getInitials';
import { useSideBar } from '../../Context/SideBar/SideBarContext';
import { adminContentPath, solvoPartnersPath, viewReferrerPath } from '../../Constants/Constants';

function NavComponent() {
    const navRef = useRef(null);
    const profileRef = useRef(null);
    const { userData, isAuthenticated, logout } = useAuth();
    const { isMobile, open } = useSideBar();
    const navigate = useNavigate();
    const [isOpen, setIsOpen] = useState(false);
    const userRoles = Array.isArray(userData?.roles) ? userData.roles : [];
    const isAdmin = userRoles.some((role) => String(role).toLowerCase() === 'admin');
    const isSolvoPartner = userData?.isSolvoPartner === true;

    useLayoutEffect(() => {
        const ctx = gsap.context(() => {
            gsap.fromTo(
                '.logoPincipal, .img2, .nav-referral-button',
                { y: -14, autoAlpha: 0 },
                { y: 0, autoAlpha: 1, duration: 0.55, stagger: 0.08, ease: 'power3.out' },
            );
        }, navRef);

        return () => ctx.revert();
    }, []);

    useLayoutEffect(() => {
        const profile = profileRef.current;
        if (!profile) {
            return undefined;
        }

        const handleMove = (event) => {
            const rect = profile.getBoundingClientRect();
            const x = event.clientX - rect.left - rect.width / 2;
            const y = event.clientY - rect.top - rect.height / 2;
            gsap.to(profile, {
                x: x * 0.08,
                y: y * 0.08,
                duration: 0.25,
                ease: 'power2.out',
            });
        };

        const reset = () => {
            gsap.to(profile, { x: 0, y: 0, duration: 0.3, ease: 'power2.out' });
        };

        profile.addEventListener('mousemove', handleMove);
        profile.addEventListener('mouseleave', reset);

        return () => {
            profile.removeEventListener('mousemove', handleMove);
            profile.removeEventListener('mouseleave', reset);
        };
    }, []);

    const handleOpenNewReferral = () => {
        navigate(viewReferrerPath, {
            state: {
                openNewReferral: true,
                nonce: Date.now(),
            },
        });
    };

    const handleOpenAdmin = () => {
        if (isMobile) {
            setIsOpen(false);
            window.alert('Please use a desktop PC to access the Admin Panel.');
            return;
        }

        setIsOpen(false);
        navigate(adminContentPath);
    };

    const handleOpenSolvoPartners = () => {
        if (isMobile) {
            setIsOpen(false);
            window.alert('Please use a desktop PC to access Solvo Partners.');
            return;
        }

        setIsOpen(false);
        navigate(solvoPartnersPath);
    };

    return (
        <div ref={navRef}>
            <nav className={`nav navbar navbar-expand-lg bg-body-white glass-panel ${open && !isMobile ? 'sideClose' : ''}`}>
                {(!open || isMobile) && <img className="logoPincipal" src={logviw} alt="Referral logo" />}
                {isAuthenticated && isSolvoPartner ? (
                    <div className="solvo-partner-top-badge" title="Verified Solvo Partner">
                        <span className="solvo-partner-top-badge__icon">
                            <FiCheckCircle />
                        </span>
                        <span className="solvo-partner-top-badge__text">Solvo Partner</span>
                    </div>
                ) : null}
                <div className="nav-actions">
                    <button
                        type="button"
                        className="nav-referral-button gsap-button"
                        onClick={handleOpenNewReferral}
                    >
                        New Referral
                    </button>
                    <div ref={profileRef} className="img2 star-container gsap-button" onClick={() => setIsOpen(true)}>
                        <img className="estrella" src={estrella} alt="Icono de estrella" />
                        <div className="perfilAbreviature">{userData ? getInitials(userData.name) : ''}</div>
                    </div>
                </div>
            </nav>
            <Modal isOpen={isOpen} onClose={() => setIsOpen(false)}>
                <div className={`card-mb-3 gsap-card ${isMobile ? 'card-mb-3-mobile' : ''}`}>
                    <div className="card-header">
                        <div style={{ display: 'flex', flexDirection: 'column' }}>
                            {isAuthenticated ? (
                                <>
                                    <p style={{ fontSize: '16px', margin: 0 }}>
                                        <b>{(userData && userData.name) || 'Usuario Desconocido'}</b>
                                    </p>
                                    <p
                                        style={{
                                            fontSize: '13px',
                                            margin: 0,
                                            color: '#6c757d',
                                        }}
                                    >
                                        {userData && userData.email}
                                    </p>
                                </>
                            ) : (
                                <>
                                    <p style={{ fontSize: '20px', margin: 0 }}>
                                        <b>No autenticado</b>
                                    </p>
                                    <p
                                        style={{
                                            fontSize: '14px',
                                            margin: 0,
                                            color: '#6c757d',
                                        }}
                                    >
                                        Inicie sesión para ver los datos.
                                    </p>
                                </>
                            )}
                        </div>
                        <p onClick={() => setIsOpen(false)} className="X">
                            X
                        </p>
                    </div>

                    <div className="card-body">
                        {isAuthenticated && isAdmin ? (
                            <>
                                <button type="button" className="profile-action profile-action--primary" onClick={handleOpenAdmin}>
                                    Admin Panel
                                </button>
                                <button type="button" className="profile-action" onClick={handleOpenSolvoPartners}>
                                    Solvo Partners
                                </button>
                            </>
                        ) : null}
                        <p onClick={logout} className="card-text log" style={{ cursor: 'pointer' }}>
                            LOG OUT
                        </p>
                    </div>
                </div>
            </Modal>
        </div>
    );
}
export default NavComponent;
