import { createBrowserRouter, Navigate } from 'react-router-dom';

import RootLayout from '../Layout/RootLayout';
import Log from '../pages/loginPage/Log';
import ViewPage from '../pages/ViewReferalPage/View';
import DataView from '../components/ConsumerReferidosComponent/DataViewReferal';

import { AdminRoute, ProtectedRoute, PublicRoute } from './ProtectedRoute';
import {
    adminContentPath,
    loginPath,
    viewReferrerPath,
    activePositionPath,
    HomePath,
    publicReferralPath,
    solvoPartnersPath,
} from '../Constants/Constants';
import HomePage from '../pages/HomePage/HomePage';
import ActivePositionPage from '../pages/ActivePositionPage/ActivePositionPage';
import AdminContentPage from '../pages/AdminContentPage/AdminContentPage';
import PublicReferralPage from '../pages/PublicReferralPage/PublicReferralPage';
import SolvoPartnersPage from '../pages/SolvoPartnersPage/SolvoPartnersPage';

export const router = createBrowserRouter([
    // ✅ Público: Login
    {
        path: loginPath,
        element: (
            <PublicRoute>
                <Log />
            </PublicRoute>
        ),
    },
    {
        path: publicReferralPath,
        element: <PublicReferralPage />,
    },
    {
        path: '*',
        element: (
            <div>
                <h1>404: Página no encontrada</h1>
                <p>Verifica la URL y la configuración de rutas.</p>
            </div>
        ),
    },
    {
        path: HomePath,
        element: <RootLayout />,
        children: [
            {
                element: <ProtectedRoute redirectTo={loginPath} />,
                children: [
                    {
                        index: true, // equivale a path: '/'
                        element: <HomePage />,
                    },
                    {
                        path: viewReferrerPath,
                        element: <ViewPage />,
                    },
                    {
                        path: activePositionPath,
                        element: <ActivePositionPage />,
                    },
                    {
                        path: adminContentPath,
                        element: (
                            <AdminRoute>
                                <AdminContentPage />
                            </AdminRoute>
                        ),
                    },
                    {
                        path: solvoPartnersPath,
                        element: (
                            <AdminRoute>
                                <SolvoPartnersPage />
                            </AdminRoute>
                        ),
                    },
                ],
            },
        ],
    },
]);
