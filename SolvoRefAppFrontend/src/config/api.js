const LOCAL_FRONTEND_HOSTS = new Set(['localhost', '127.0.0.1']);
const DEV_FRONTEND_BASE = 'https://pruebasolvoreferralapp.solvoglobal.com';
const MAIN_FRONTEND_BASE = 'https://solvoreferralapp.solvoglobal.com';
const LEGACY_FRONTEND_HOSTS = new Map([
    ['red-river-03149200f.2.azurestaticapps.net', DEV_FRONTEND_BASE],
    ['yellow-pond-0c44ce80f.2.azurestaticapps.net', MAIN_FRONTEND_BASE],
    ['pruebasolvoreferalapp.solvoglobal.com', DEV_FRONTEND_BASE],
    ['solvoreferalapp.solvoglobal.com', MAIN_FRONTEND_BASE],
]);
const DEV_FRONTEND_HOSTS = new Set([
    'pruebasolvoreferralapp.solvoglobal.com',
]);

const trimTrailingSlash = value => (value || '').replace(/\/+$/, '');

const LOCAL_API_BASE = trimTrailingSlash(
    process.env.REACT_APP_LOCAL_API || 'http://localhost:5274',
);

const CLOUD_API_BASE = trimTrailingSlash(
    process.env.REACT_APP_CLOUD_API ||
        process.env.REACT_APP_API_CLOUD ||
        'https://sol-ref-api-dtb6dpftdsema2gt.eastus2-01.azurewebsites.net',
);

const CONFIGURED_API_BASE = trimTrailingSlash(process.env.REACT_APP_API || '');
const CANONICAL_FRONTEND_BASE = trimTrailingSlash(
    process.env.REACT_APP_FRONTEND_URL ||
        DEV_FRONTEND_BASE,
);

const isLocalFrontend =
    typeof window !== 'undefined' &&
    LOCAL_FRONTEND_HOSTS.has(window.location.hostname);

let resolvedApiBase = isLocalFrontend
    ? LOCAL_API_BASE
    : (
        typeof window !== 'undefined' && DEV_FRONTEND_HOSTS.has(window.location.hostname)
            ? CLOUD_API_BASE
            : CONFIGURED_API_BASE || CLOUD_API_BASE
    );

let apiResolutionPromise = null;
let csrfTokenPromise = null;
let csrfToken = '';
const AUTH_TOKEN_STORAGE_KEY = 'solvo_ref_auth_token';

function normalizePath(path) {
    if (!path) return '';
    return path.startsWith('/') ? path : `/${path}`;
}

function withTimeout(ms = 1500) {
    const controller = new AbortController();
    const timeoutId = window.setTimeout(() => controller.abort(), ms);

    return {
        signal: controller.signal,
        clear: () => window.clearTimeout(timeoutId),
    };
}

async function canReachApi(apiBase) {
    const { signal, clear } = withTimeout();

    try {
        const response = await fetch(`${apiBase}/api/auth/me`, {
            method: 'GET',
            credentials: 'include',
            signal,
        });

        return response != null;
    } catch (_) {
        return false;
    } finally {
        clear();
    }
}

export async function ensureApiBase() {
    if (!isLocalFrontend) {
        resolvedApiBase =
            typeof window !== 'undefined' && DEV_FRONTEND_HOSTS.has(window.location.hostname)
                ? CLOUD_API_BASE
                : CONFIGURED_API_BASE || CLOUD_API_BASE;
        return resolvedApiBase;
    }

    if (resolvedApiBase === CLOUD_API_BASE) {
        return resolvedApiBase;
    }

    if (!apiResolutionPromise) {
        apiResolutionPromise = (async () => {
            const localIsReachable = await canReachApi(LOCAL_API_BASE);
            resolvedApiBase = localIsReachable ? LOCAL_API_BASE : CLOUD_API_BASE;
            return resolvedApiBase;
        })();
    }

    return apiResolutionPromise;
}

export function getApiBase() {
    return resolvedApiBase;
}

export function redirectToCanonicalFrontendIfNeeded() {
    if (typeof window === 'undefined') {
        return false;
    }

    const { hostname, pathname, search, hash } = window.location;
    if (LOCAL_FRONTEND_HOSTS.has(hostname) || !LEGACY_FRONTEND_HOSTS.has(hostname)) {
        return false;
    }

    const canonicalBase = trimTrailingSlash(
        process.env.REACT_APP_FRONTEND_URL ||
            LEGACY_FRONTEND_HOSTS.get(hostname) ||
            CANONICAL_FRONTEND_BASE,
    );

    window.location.replace(
        `${canonicalBase}${pathname}${search}${hash}`,
    );
    return true;
}

export function getFrontendRedirectUri() {
    const frontendBase = trimTrailingSlash(
        process.env.REACT_APP_FRONTEND_URL ||
            (typeof window !== 'undefined' ? window.location.origin : CANONICAL_FRONTEND_BASE),
    );

    return `${frontendBase}/`;
}

export function resolveContentAssetUrl(url) {
    if (!url) {
        return '';
    }

    const trimmedUrl = String(url).trim();
    if (!trimmedUrl) {
        return '';
    }

    if (trimmedUrl.startsWith('data:') || trimmedUrl.startsWith('blob:')) {
        return trimmedUrl;
    }

    const activeBase =
        getApiBase() ||
        (isLocalFrontend ? LOCAL_API_BASE : CONFIGURED_API_BASE || CLOUD_API_BASE);

    const uploadPath = trimmedUrl.replace(/^\/+/, '');
    if (uploadPath.startsWith('uploads/content/')) {
        return `${activeBase}/${uploadPath}`;
    }

    if (uploadPath.startsWith('api/content/assets/')) {
        return `${activeBase}/${uploadPath}`;
    }

    try {
        const parsed = new URL(trimmedUrl);
        if (parsed.host.includes('.blob.core.windows.net')) {
            const segments = parsed.pathname.split('/').filter(Boolean);
            if (segments.length >= 2 && segments[0].toLowerCase() === segments[1].toLowerCase()) {
                parsed.pathname = `/${[segments[0], ...segments.slice(2)].join('/')}`;
                return parsed.toString();
            }
        }

        if (parsed.pathname.startsWith('/uploads/content/')) {
            return `${activeBase}${parsed.pathname}`;
        }
    } catch (_) {
        return trimmedUrl;
    }

    return trimmedUrl;
}
export function buildApiUrl(path, apiBase = resolvedApiBase) {
    return `${apiBase}${normalizePath(path)}`;
}

export async function resolveApiUrl(path) {
    const apiBase = await ensureApiBase();
    return buildApiUrl(path, apiBase);
}

export function clearCsrfToken() {
    csrfToken = '';
    csrfTokenPromise = null;
}

export function getAuthToken() {
    if (typeof window === 'undefined') {
        return '';
    }

    try {
        return window.sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY) || '';
    } catch (_) {
        return '';
    }
}

export function setAuthToken(token) {
    if (typeof window === 'undefined') {
        return;
    }

    try {
        if (token) {
            window.sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, token);
        } else {
            window.sessionStorage.removeItem(AUTH_TOKEN_STORAGE_KEY);
        }
    } catch (_) {
        // no-op
    }
}

export function clearAuthToken() {
    setAuthToken('');
}

export function getAuthorizationHeaders() {
    const token = getAuthToken();
    return token ? { Authorization: `Bearer ${token}` } : {};
}

export async function getCsrfToken() {
    if (csrfToken) {
        return csrfToken;
    }

    if (!csrfTokenPromise) {
        csrfTokenPromise = (async () => {
            const response = await fetch(await resolveApiUrl('/api/auth/csrf'), {
                method: 'GET',
                credentials: 'include',
            });

            if (!response.ok) {
                clearCsrfToken();
                return '';
            }

            const payload = await response.json();
            csrfToken = payload?.data?.csrf || payload?.data?.token || '';
            return csrfToken;
        })();
    }

    return csrfTokenPromise;
}
