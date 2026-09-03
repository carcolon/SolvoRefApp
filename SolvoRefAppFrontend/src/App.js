import { RouterProvider } from 'react-router-dom';
import { useEffect, useRef } from 'react';
import { router } from './Router/Router';
import { ToastContainer } from 'react-toastify';
import { AuthProvider } from './components/AuthContextComponent/AuthContext';
import { SideBarProvider } from './Context/SideBar/SideBarContext';
import { useInteractiveMotion } from './animations/useInteractiveMotion';
import { redirectToCanonicalFrontendIfNeeded } from './config/api';

export default function App() {
    const appRef = useRef(null);
    useInteractiveMotion(appRef, []);

    useEffect(() => {
        redirectToCanonicalFrontendIfNeeded();
    }, []);

    return (
        <AuthProvider>
            <SideBarProvider>
                <div ref={appRef}>
                <ToastContainer />
                <RouterProvider router={router} />
                </div>
            </SideBarProvider>
        </AuthProvider>
    );
}
