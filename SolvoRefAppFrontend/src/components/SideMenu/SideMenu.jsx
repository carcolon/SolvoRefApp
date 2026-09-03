import { useLayoutEffect, useRef } from 'react';
import { gsap } from 'gsap';
import './sideMenu.css';
import logView from '../../assets/images/referralImg.png';
import hamburgerMenu from '../../assets/images/hamburger.svg';
import { useSideBar } from '../../Context/SideBar/SideBarContext';
import homePageIcon from '../../assets/images/Home Page.svg';
import referralIcon from '../../assets/images/Omnichannel.svg';
import checkIcon from '../../assets/images/Check Mark.svg';
import {
    activePositionPath,
    HomePath,
    viewReferrerPath,
} from '../../Constants/Constants';
import MenuItem from '../MenuItem/MenuItem';

const SideMenu = () => {
    const sideMenuRef = useRef(null);
    const { open, setOpen, isMobile, currentRoute, setCurrentRoute } = useSideBar();
    const menu = [
        {
            icon: homePageIcon,
            iconAlt: 'Home page icon',
            text: 'Home',
            showText: open,
            to: HomePath,
        },
        {
            icon: referralIcon,
            iconAlt: 'Active positions Icon',
            text: 'Active Positions',
            showText: open,
            to: activePositionPath,
        },
        {
            icon: checkIcon,
            iconAlt: 'Referral Icon',
            text: 'My Referrals',
            showText: open,
            to: viewReferrerPath,
        },
    ];

    useLayoutEffect(() => {
        const scope = sideMenuRef.current;
        if (!scope) {
            return undefined;
        }

        const ctx = gsap.context(() => {
            gsap.to(scope, {
                width: open ? (isMobile ? 304 : 254) : 92,
                duration: 0.34,
                ease: 'power2.inOut',
            });

            const logo = scope.querySelector('.logoPrincipalMenu');
            if (logo) {
                gsap.to(logo, {
                    autoAlpha: open ? 1 : 0,
                    y: open ? 0 : -8,
                    duration: 0.24,
                    ease: 'power2.out',
                });
            }

            const menuItems = gsap.utils.toArray('.menuItemContainer .itemContainer', scope);
            if (menuItems.length > 0) {
                gsap.fromTo(
                    menuItems,
                    { x: -14, autoAlpha: 0 },
                    { x: 0, autoAlpha: 1, duration: 0.45, stagger: 0.06, ease: 'power3.out' },
                );
            }
        }, scope);

        return () => ctx.revert();
    }, [isMobile, open]);

    const handleMenuToggle = () => {
        setOpen(!open);
    };

    const handleMenuItemClick = (route) => {
        setCurrentRoute(route);
        if (isMobile) {
            setOpen(false);
        }
    };

    if (isMobile && !open) {
        return (
            <button
                type="button"
                className="sideMenuLauncher glass-panel gsap-button"
                onClick={handleMenuToggle}
                aria-label="Open navigation menu"
            >
                <img className="principalMenuHamburger" src={hamburgerMenu} alt="Open menu" />
            </button>
        );
    }

    return (
        <>
            {isMobile && open ? <button type="button" className="sideMenuBackdrop" onClick={handleMenuToggle} /> : null}
            <aside
                ref={sideMenuRef}
                className={`sideMenu glass-panel ${!open ? 'sideMenuCLose' : ''} ${isMobile ? 'sideMenuMobile' : ''}`}
            >
                <div className="imagenContainer">
                    {open && <img className="logoPrincipalMenu" src={logView} alt="Referral logo" />}
                    <img
                        className="principalMenuHamburger gsap-button"
                        src={hamburgerMenu}
                        alt="Toggle menu"
                        onClick={handleMenuToggle}
                    />
                </div>
                <div className="menuItemContainer">
                    {menu.map((x) => (
                        <MenuItem
                            key={x.to}
                            icon={x.icon}
                            iconAlt={x.iconAlt}
                            showText={x.showText}
                            text={x.text}
                            to={x.to}
                            isSelected={currentRoute === x.to}
                            onHandlerLink={() => handleMenuItemClick(x.to)}
                        />
                    ))}
                </div>
            </aside>
        </>
    );
};

export default SideMenu;
