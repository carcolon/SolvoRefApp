import { createContext, useContext, useEffect, useState } from 'react';
import {
    activePositionPath,
    HomePath,
    viewReferrerPath,
} from '../../Constants/Constants';

const SideBarContext = createContext(null);

const validRoutes = new Set([HomePath, activePositionPath, viewReferrerPath]);

const getInitialRoute = () => {
    if (typeof window === 'undefined') {
        return HomePath;
    }

    const currentPath = window.location.pathname;
    return validRoutes.has(currentPath) ? currentPath : HomePath;
};

const getInitialIsMobile = () => {
    if (typeof window === 'undefined') {
        return false;
    }

    return window.innerWidth <= 900;
};

export const useSideBar = () => {
    return useContext(SideBarContext);
};

export const SideBarProvider = ({ children }) => {
    const [isMobile, setIsMobile] = useState(getInitialIsMobile);
    const [open, setOpen] = useState(() => !getInitialIsMobile());
    const [currentRoute, setCurrentRoute] = useState(getInitialRoute);

    useEffect(() => {
        if (typeof window === 'undefined') {
            return undefined;
        }

        const mediaQuery = window.matchMedia('(max-width: 900px)');

        const handleViewportChange = (event) => {
            const nextIsMobile = event.matches;
            setIsMobile(nextIsMobile);
            setOpen(!nextIsMobile);
        };

        handleViewportChange(mediaQuery);

        if (typeof mediaQuery.addEventListener === 'function') {
            mediaQuery.addEventListener('change', handleViewportChange);
            return () => mediaQuery.removeEventListener('change', handleViewportChange);
        }

        mediaQuery.addListener(handleViewportChange);
        return () => mediaQuery.removeListener(handleViewportChange);
    }, []);

    const value = {
        open,
        setOpen,
        isMobile,
        currentRoute,
        setCurrentRoute,
    };

    return (
        <SideBarContext.Provider value={value}>
            {children}
        </SideBarContext.Provider>
    );
};
