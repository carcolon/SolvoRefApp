import './Login.css';
import 'bootstrap/dist/css/bootstrap.min.css';
import { useAuth } from '../AuthContextComponent/AuthContext';
import logoReferral from '../../assets/images/referralImg.png';
import blueStart from '../../assets/images/BlueStar.png';
import { useNavigate } from 'react-router-dom';
import { useEffect, useRef } from 'react';
import Spinner from '../Spinner/Spinner';
import AnimatedPageShell from '../AnimatedPageShell';
import { usePageMotion } from '../../animations/usePageMotion';
import { HomePath } from '../../Constants/Constants';
import { getFrontendRedirectUri } from '../../config/api';

const getEnv = key => {
    if (typeof import.meta !== 'undefined' && import.meta.env?.[key]) {
        return import.meta.env[key];
    }

    if (typeof process !== 'undefined' && process.env?.[key]) {
        return process.env[key];
    }

    return '';
};

function Login() {
    const pageRef = useRef(null);
    usePageMotion(pageRef, []);
    const { isAuthenticated, loading, setLoading, authError, clearAuthError } = useAuth();
    const navigate = useNavigate();

    useEffect(() => {
        if (isAuthenticated) {
            navigate(HomePath, { replace: true });
        }
    }, [isAuthenticated, navigate]);

    const handleLogin = async () => {
        clearAuthError();
        setLoading(true);

        const clientId = getEnv('REACT_APP_CLIENT_ID') || getEnv('VITE_CLIENT_ID');
        const authorityTenant =
            getEnv('REACT_APP_AUTHORITY_TENANT') ||
            getEnv('VITE_AUTHORITY_TENANT') ||
            getEnv('REACT_APP_TENANT_ID') ||
            getEnv('VITE_TENANT_ID') ||
            'organizations';
        const redirectUri = getFrontendRedirectUri();

        const url =
            `https://login.microsoftonline.com/${authorityTenant}/oauth2/v2.0/authorize` +
            `?client_id=${clientId}` +
            `&response_type=code` +
            `&redirect_uri=${encodeURIComponent(redirectUri)}` +
            `&response_mode=query` +
            `&scope=${encodeURIComponent('openid profile email https://graph.microsoft.com/User.Read')}`;

        window.location.href = url;
    };

    return (
        <AnimatedPageShell ref={pageRef}>
            <Spinner isLoading={loading} />
            <div className="fullImg">
                <div className="content glass-panel">
                    <img
                        src={logoReferral}
                        className="logoLoginReferral"
                        alt="logo referral"
                        data-hero
                    />
                    <img
                        src={blueStart}
                        className="blueStart1"
                        alt="blue start 1"
                        data-float
                    />
                    <img
                        src={blueStart}
                        className="blueStart2"
                        alt="blue start 2"
                        data-float
                    />

                    <p className="parrafo2" data-hero>
                        Welcome to a new era of referrals.
                        <br /> Discover how easy it is to connect and earn.
                    </p>
                    <p className="parrafo" data-hero>
                        Sign in with your Microsoft account.
                    </p>
                    {authError ? (
                        <div className="auth-error" role="alert" data-hero>
                            {authError}
                        </div>
                    ) : null}

                    <div className="button-wrapper" data-hero>
                        <button className="b-outline gsap-button" onClick={handleLogin}>
                            Sign in
                        </button>
                    </div>
                </div>
            </div>
        </AnimatedPageShell>
    );
}

export default Login;
