import {
    getAuthToken,
    getAuthorizationHeaders,
    getCsrfToken,
    resolveApiUrl,
} from '../config/api';

async function buildHeaders(headers = {}, unsafe = false) {
    const finalHeaders = {
        ...getAuthorizationHeaders(),
        ...headers,
    };

    if (unsafe && !getAuthToken()) {
        const csrfToken = await getCsrfToken();
        if (csrfToken) {
            finalHeaders['X-CSRF-Token'] = csrfToken;
        }
    }

    return finalHeaders;
}

async function parseApiResponse(response, fallbackMessage) {
    const contentType = response.headers.get('content-type') || '';
    let payload = null;

    try {
        if (contentType.includes('application/json')) {
            payload = await response.json();
        } else {
            const text = await response.text();
            payload = { success: response.ok, errors: text ? [text] : [fallbackMessage] };
        }
    } catch {
        payload = { success: response.ok, errors: [fallbackMessage] };
    }

    if (!response.ok && (!payload?.errors || !payload.errors.length)) {
        payload = { ...(payload || {}), success: false, errors: [fallbackMessage] };
    }

    return payload;
}

export async function fetchContentCards(section, admin = false) {
    const path = admin
        ? `/api/content/admin/home-cards${section ? `?section=${encodeURIComponent(section)}` : ''}`
        : `/api/content/home-cards${section ? `?section=${encodeURIComponent(section)}` : ''}`;

    const response = await fetch(await resolveApiUrl(path), {
        method: 'GET',
        credentials: 'include',
        headers: await buildHeaders(),
    });

    return parseApiResponse(response, 'Could not load cards.');
}

export async function saveContentCard(id, payload) {
    const method = id ? 'PUT' : 'POST';
    const path = id ? `/api/content/admin/home-cards/${id}` : '/api/content/admin/home-cards';
    const response = await fetch(await resolveApiUrl(path), {
        method,
        credentials: 'include',
        headers: await buildHeaders({
            'Content-Type': 'application/json',
        }, true),
        body: JSON.stringify(payload),
    });

    return parseApiResponse(response, 'Could not save the card.');
}

export async function deleteContentCard(id) {
    const response = await fetch(await resolveApiUrl(`/api/content/admin/home-cards/${id}`), {
        method: 'DELETE',
        credentials: 'include',
        headers: await buildHeaders({}, true),
    });

    return parseApiResponse(response, 'Could not delete the card.');
}

export async function uploadContentImage(file) {
    const body = new FormData();
    body.append('file', file);

    const response = await fetch(await resolveApiUrl('/api/content/admin/upload-image'), {
        method: 'POST',
        credentials: 'include',
        headers: await buildHeaders({}, true),
        body,
    });

    return parseApiResponse(response, 'Could not upload the image.');
}

export async function fetchAdminUsers() {
    const response = await fetch(await resolveApiUrl('/api/admin/users'), {
        method: 'GET',
        credentials: 'include',
        headers: await buildHeaders(),
    });

    return parseApiResponse(response, 'Could not load admin users.');
}

export async function createAdminUser(email) {
    const response = await fetch(await resolveApiUrl('/api/admin/users'), {
        method: 'POST',
        credentials: 'include',
        headers: await buildHeaders({
            'Content-Type': 'application/json',
        }, true),
        body: JSON.stringify({ email }),
    });

    return parseApiResponse(response, 'Could not create admin user.');
}

export async function activateAdminUser(id) {
    const response = await fetch(await resolveApiUrl(`/api/admin/users/${id}/activate`), {
        method: 'POST',
        credentials: 'include',
        headers: await buildHeaders({}, true),
    });

    return parseApiResponse(response, 'Could not activate admin user.');
}

export async function deactivateAdminUser(id) {
    const response = await fetch(await resolveApiUrl(`/api/admin/users/${id}/deactivate`), {
        method: 'POST',
        credentials: 'include',
        headers: await buildHeaders({}, true),
    });

    return parseApiResponse(response, 'Could not deactivate admin user.');
}

export async function removeAdminUser(id) {
    const response = await fetch(await resolveApiUrl(`/api/admin/users/${id}`), {
        method: 'DELETE',
        credentials: 'include',
        headers: await buildHeaders({}, true),
    });

    return parseApiResponse(response, 'Could not remove admin user.');
}

export async function fetchSolvoPartners() {
    const response = await fetch(await resolveApiUrl('/api/admin/solvo-partners'), {
        method: 'GET',
        credentials: 'include',
        headers: await buildHeaders(),
    });

    return parseApiResponse(response, 'Could not load Solvo Partners.');
}

export async function createSolvoPartner(email) {
    const response = await fetch(await resolveApiUrl('/api/admin/solvo-partners'), {
        method: 'POST',
        credentials: 'include',
        headers: await buildHeaders({
            'Content-Type': 'application/json',
        }, true),
        body: JSON.stringify({ email }),
    });

    return parseApiResponse(response, 'Could not create Solvo Partner.');
}

export async function activateSolvoPartner(id) {
    const response = await fetch(await resolveApiUrl(`/api/admin/solvo-partners/${id}/activate`), {
        method: 'POST',
        credentials: 'include',
        headers: await buildHeaders({}, true),
    });

    return parseApiResponse(response, 'Could not activate Solvo Partner.');
}

export async function deactivateSolvoPartner(id) {
    const response = await fetch(await resolveApiUrl(`/api/admin/solvo-partners/${id}/deactivate`), {
        method: 'POST',
        credentials: 'include',
        headers: await buildHeaders({}, true),
    });

    return parseApiResponse(response, 'Could not deactivate Solvo Partner.');
}

export async function removeSolvoPartner(id) {
    const response = await fetch(await resolveApiUrl(`/api/admin/solvo-partners/${id}`), {
        method: 'DELETE',
        credentials: 'include',
        headers: await buildHeaders({}, true),
    });

    return parseApiResponse(response, 'Could not remove Solvo Partner.');
}
