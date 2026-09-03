import { useLayoutEffect, useRef } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import { gsap } from 'gsap';
import '../App.css';
import NavComponent from '../components/NavComponent/NavComponent';
import SideMenu from '../components/SideMenu/SideMenu';
import { useSideBar } from '../Context/SideBar/SideBarContext';
import {
    activePositionPath,
    HomePath,
    viewReferrerPath,
} from '../Constants/Constants';

export default function RootLayout() {
    const layoutRef = useRef(null);
    const location = useLocation();
    const { isMobile, open, setCurrentRoute } = useSideBar();

    useLayoutEffect(() => {
        const currentPath = location.pathname;
        const navigableRoutes = new Set([HomePath, activePositionPath, viewReferrerPath]);

        if (navigableRoutes.has(currentPath)) {
            setCurrentRoute(currentPath);
        }
    }, [location.pathname, setCurrentRoute]);

    useLayoutEffect(() => {
        const scope = layoutRef.current;
        if (!scope) {
            return undefined;
        }

        const ctx = gsap.context(() => {
            const sideMenu = scope.querySelector('.sideMenu');
            if (sideMenu) {
                gsap.fromTo(
                    sideMenu,
                    { x: -28, autoAlpha: 0 },
                    { x: 0, autoAlpha: 1, duration: 0.55, ease: 'power3.out' },
                );
            }

            const navBar = scope.querySelector('.nav.navbar.navbar-expand-lg');
            if (navBar) {
                gsap.fromTo(
                    navBar,
                    { y: -24, autoAlpha: 0 },
                    { y: 0, autoAlpha: 1, duration: 0.55, ease: 'power3.out', delay: 0.08 },
                );
            }
        }, scope);

        return () => ctx.revert();
    }, []);

    return (
        <div
            ref={layoutRef}
            className={`App layout-shell ${open ? 'layout-shell-sidebar-open' : 'layout-shell-sidebar-collapsed'} ${isMobile ? 'layout-shell-mobile' : ''}`}
        >
            <SideMenu />
            <div className="mainMenu">
                <NavComponent />
                <main className="app-main">
                    <Outlet />
                </main>
            </div>
        </div>
    );
}
