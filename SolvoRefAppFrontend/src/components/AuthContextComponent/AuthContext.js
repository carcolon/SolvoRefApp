import React, {
    createContext,
    useCallback,
    useContext,
    useEffect,
    useRef,
    useState,
} from 'react';
import { loginPath } from '../../Constants/Constants';
import {
    clearAuthToken,
    clearCsrfToken,
    getAuthorizationHeaders,
    getAuthToken,
    getCsrfToken,
    getFrontendRedirectUri,
    resolveApiUrl,
    setAuthToken,
} from '../../config/api';

const AuthContext = createContext(null);
const AUTHORIZATION_ERROR_MESSAGE = 'You are not authorized to Access the Referral App';
const SESSION_REFRESH_THROTTLE_MS = 5 * 60 * 1000;

export const useAuth = () => {
    return useContext(AuthContext);
};

export const AuthProvider = ({ children }) => {
    const [loading, setLoading] = useState(true);
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [userData, setUserData] = useState(null);
    const [authError, setAuthError] = useState(null);
    const lastActivityRefreshRef = useRef(0);
    const activityRefreshInFlightRef = useRef(false);

    const clearSession = useCallback(() => {
        clearCsrfToken();
        clearAuthToken();
        setIsAuthenticated(false);
        setUserData(null);
    }, []);

    const fetchProfile = useCallback(async () => {
        const response = await fetch(await resolveApiUrl('/api/auth/me'), {
            method: 'GET',
            credentials: 'include',
            headers: {
                ...getAuthorizationHeaders(),
            },
        });

        if (response.status === 403) {
            setAuthError(AUTHORIZATION_ERROR_MESSAGE);
            return null;
        }

        if (!response.ok) {
            return null;
        }

        const payload = await response.json();
        if (!payload?.success || !payload?.data) {
            return null;
        }

        return payload.data;
    }, []);

    const hydrateSession = useCallback(
        async () => {
            try {
                const profile = await fetchProfile();
                if (!profile) {
                    clearSession();
                    return false;
                }

                setIsAuthenticated(true);
                setUserData(profile);
                setAuthError(null);
                return true;
            } catch (_) {
                clearSession();
                return false;
            }
        },
        [clearSession, fetchProfile],
    );

    const refreshSession = useCallback(async () => {
        if (activityRefreshInFlightRef.current) {
            return true;
        }

        activityRefreshInFlightRef.current = true;
        try {
            const authHeaders = getAuthorizationHeaders();
            const csrfToken = getAuthToken() ? '' : await getCsrfToken();
            const response = await fetch(await resolveApiUrl('/api/auth/refresh'), {
                method: 'POST',
                credentials: 'include',
                headers: {
                    ...authHeaders,
                    ...(csrfToken ? { 'X-CSRF-Token': csrfToken } : {}),
                },
            });

            if (!response.ok) {
                clearSession();
                window.location.replace(loginPath);
                return false;
            }

            try {
                const payload = await response.json();
                const refreshedToken = payload?.data?.accessToken || payload?.data?.token || '';
                if (refreshedToken) {
                    setAuthToken(refreshedToken);
                }
            } catch (_) {
                // no-op
            }

            lastActivityRefreshRef.current = Date.now();
            clearCsrfToken();
            return true;
        } catch (_) {
            return false;
        } finally {
            activityRefreshInFlightRef.current = false;
        }
    }, [clearSession]);

    const exchangeCode = useCallback(
        async code => {
            const response = await fetch(await resolveApiUrl('/api/auth/microsoft/exchange'), {
                method: 'POST',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    code,
                    redirectUri: getFrontendRedirectUri(),
                }),
            });

            if (!response.ok) {
                if (response.status === 403) {
                    try {
                        const payload = await response.json();
                        setAuthError(payload?.errors?.[0] || AUTHORIZATION_ERROR_MESSAGE);
                    } catch (_) {
                        setAuthError(AUTHORIZATION_ERROR_MESSAGE);
                    }
                }

                return null;
            }

            const payload = await response.json();
            const token = payload?.data?.accessToken || payload?.data?.token || '';
            if (token) {
                setAuthToken(token);
            }

            setAuthError(null);
            return payload?.success === true;
        },
        [],
    );

    const logout = useCallback(async () => {
        try {
            const authHeaders = getAuthorizationHeaders();
            const csrfToken = getAuthToken() ? '' : await getCsrfToken();
            await fetch(await resolveApiUrl('/api/auth/logout'), {
                method: 'POST',
                credentials: 'include',
                headers: {
                    ...authHeaders,
                    ...(csrfToken ? { 'X-CSRF-Token': csrfToken } : {}),
                },
            });
        } catch (_) {
            // no-op
        }

        clearSession();
        window.location.replace(loginPath);
    }, [clearSession]);

    const login = useCallback(async () => {
        return await hydrateSession();
    }, [hydrateSession]);

    useEffect(() => {
        let mounted = true;

        const bootstrap = async () => {
            try {
                const url = new URL(window.location.href);
                const code = url.searchParams.get('code');

                if (code) {
                    const exchanged = await exchangeCode(code);
                    url.searchParams.delete('code');
                    url.searchParams.delete('session_state');
                    window.history.replaceState({}, document.title, `${url.pathname}${url.search}${url.hash}`);

                    if (mounted) {
                        if (exchanged) {
                            await hydrateSession();
                        } else {
                            clearSession();
                        }
                    }
                } else if (mounted) {
                    await hydrateSession();
                }
            } finally {
                if (mounted) {
                    setLoading(false);
                }
            }
        };

        bootstrap();

        return () => {
            mounted = false;
        };
    }, [clearSession, exchangeCode, hydrateSession]);

    useEffect(() => {
        if (!isAuthenticated) {
            return undefined;
        }

        const handleUserActivity = () => {
            const now = Date.now();
            if (now - lastActivityRefreshRef.current < SESSION_REFRESH_THROTTLE_MS) {
                return;
            }

            refreshSession();
        };

        const activityEvents = [
            'pointerdown',
            'keydown',
            'scroll',
            'wheel',
            'touchstart',
        ];

        activityEvents.forEach(eventName => {
            window.addEventListener(eventName, handleUserActivity, {
                passive: true,
            });
        });

        return () => {
            activityEvents.forEach(eventName => {
                window.removeEventListener(eventName, handleUserActivity);
            });
        };
    }, [isAuthenticated, refreshSession]);

    const value = {
        isAuthenticated,
        userData,
        login,
        logout,
        loading,
        setLoading,
        authError,
        clearAuthError: () => setAuthError(null),
    };

    return (
        <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
    );
};
