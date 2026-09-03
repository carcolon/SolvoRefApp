import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../components/AuthContextComponent/AuthContext';
import { HomePath } from '../Constants/Constants';
import Spinner from '../components/Spinner/Spinner';

function hasAdminRole(userData) {
    const roles = Array.isArray(userData?.roles) ? userData.roles : [];
    return roles.some((role) => String(role).toLowerCase() === 'admin');
}

export function ProtectedRoute({ children, redirectTo = '/' }) {
    const { isAuthenticated, loading } = useAuth();

    if (loading) return <Spinner isLoading={true} />;

    if (!isAuthenticated) return <Navigate to={redirectTo} replace />;

    return children ? children : <Outlet />;
}

export function PublicRoute({ children, redirectTo = HomePath }) {
    const { isAuthenticated, loading } = useAuth();

    if (loading) return <Spinner isLoading={true} />;

    if (isAuthenticated) return <Navigate to={redirectTo} replace />;

    return children ? children : <Outlet />;
}

export function AdminRoute({ children, redirectTo = HomePath }) {
    const { isAuthenticated, loading, userData } = useAuth();

    if (loading) return <Spinner isLoading={true} />;

    if (!isAuthenticated) return <Navigate to={redirectTo} replace />;

    if (!hasAdminRole(userData)) return <Navigate to={HomePath} replace />;

    return children ? children : <Outlet />;
}
