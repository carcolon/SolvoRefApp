// src/hooks/useApi.js
import { toast } from 'react-toastify';
import { loginPath } from '../../Constants/Constants';
import { useAuth } from '../AuthContextComponent/AuthContext';
import {
    ensureApiBase,
    getApiBase,
    getAuthToken,
    getAuthorizationHeaders,
    getCsrfToken,
} from '../../config/api';

// AbortController GLOBAL compartido por todas las llamadas hechas con este hook
let globalAbortController = new AbortController();

function getGlobalSignal() {
    return globalAbortController.signal;
}

function cancelAllRequests() {
    try {
        globalAbortController.abort(); // aborta todas las peticiones en curso
    } catch (_) {
        // no-op
    } finally {
        // crea un nuevo controller para próximas solicitudes
        globalAbortController = new AbortController();
    }
}

export function useApi() {
    const { logout } = useAuth();

    // helper para invalidación centralizada
    const handleInvalidToken = () => {
        cancelAllRequests(); // aborta TODO
        try {
            toast.error('Token expired, please log in again.');
            logout();
        } finally {
            window.location.replace(loginPath); // evita ir atrás
        }
    };

    /**
     * request principal
     * @param {string} url
     * @param {object} options
     * @param {boolean} options.returnFullResponse -> si true, retorna el payload completo (incluye paginación)
     * @param {boolean} options.silentErrors -> si true, no muestra toast de error (opcional)
     */
    const request = async (url, options) => {
        const {
            method = 'GET',
            body,
            headers = {},
            returnFullResponse = false,
            silentErrors = false,
        } = options || {};

        const isFormData =
            typeof FormData !== 'undefined' && body instanceof FormData;

        const finalHeaders = { ...headers };
        const authHeaders = getAuthorizationHeaders();
        if (!['GET', 'HEAD', 'OPTIONS'].includes(method.toUpperCase())) {
            const csrfToken = getAuthToken() ? '' : await getCsrfToken();
            if (csrfToken) {
                finalHeaders['X-CSRF-Token'] = csrfToken;
            }
        }

        // Solo setear Content-Type si NO es FormData
        if (!isFormData) {
            finalHeaders['Content-Type'] =
                finalHeaders['Content-Type'] || 'application/json';
        }

        const fetchOptions = {
            method,
            headers: {
                ...authHeaders,
                ...finalHeaders,
            },
            signal: getGlobalSignal(),
            credentials: 'include',
        };

        // Adjuntar body solo si existe y el método lo permite
        if (body != null && method !== 'GET' && method !== 'HEAD') {
            fetchOptions.body = isFormData ? body : JSON.stringify(body);
        }

        try {
            const apiBase = await ensureApiBase();
            const targetUrl = url.replace(
                /^(https?:\/\/[^/]+)(?=\/api\/)/i,
                getApiBase() || apiBase,
            );
            const res = await fetch(targetUrl, fetchOptions);

            if (res.status === 401) {
                handleInvalidToken();
                return null;
            }

            let result = null;
            const contentType = res.headers.get('content-type') || '';

            if (contentType.includes('application/json')) {
                result = await res.json();
            } else {
                const text = await res.text();
                try {
                    result = JSON.parse(text);
                } catch {
                    result = {
                        success: res.ok,
                        data: null,
                        errors: [text || ''],
                    };
                }
            }

            // Manejo de éxito/errores estandarizado
            if (!result?.success) {
                if (!silentErrors) {
                    if (
                        Array.isArray(result?.errors) &&
                        result.errors.length > 0
                    ) {
                        toast.error(
                            result.errors[0] ||
                                'Ocurrió un error en la solicitud.',
                        );
                    } else {
                        toast.error('Ocurrió un error en la solicitud.');
                    }
                }
                return null;
            }

            // ✅ Aquí está el cambio clave:
            // Si quieres paginación y demás meta -> retorna todo el result
            if (returnFullResponse) return result;

            // Caso normal -> retorna solo data (compatibilidad)
            return result.data;
        } catch (error) {
            if (error?.name === 'AbortError') return null;
            if (!silentErrors)
                toast.error(error?.message || 'Error de red/servidor.');
            return null;
        }
    };

    return { request, cancelAllRequests, getGlobalSignal };
}
