const refreshPageAtInterval = (time, { seconds = true, minutes = false, hours = false } = {}) => {
    let interval = time * 1000; // Default is seconds

    if (minutes) {
        interval = time * 60 * 1000; // Convert to milliseconds
    } else if (hours) {
        interval = time * 60 * 60 * 1000; // Convert to milliseconds
    }

    setInterval(() => {
        location.reload(); // Reload the page
    }, interval);
};


const request = async (url, { method = 'GET', body = null, headers = { 'Content-Type': 'application/json' }, stringifyBody = true } = {}) => {

    const res = await fetch(url, {
        method,
        headers,
        ...(body && { body: stringifyBody ? JSON.stringify(body) : body })
    });

    if (!res.ok) throw new Error(`Error: ${res.status} ${res.statusText}`);

    const contentType = res.headers.get('content-type') || '';

    const hasBody = contentType.includes('application/json');

    return hasBody ? await res.json() : null;
};


const catchError = async promise => {
    try {
        const data = await promise;
        return [data, null];
    } catch (error) {
        return [null, error];
    }
};


const base64ToFile_fetch = async (dataUrl, filename) => {
    const res = await fetch(dataUrl);          // soporta data: URLs
    const blob = await res.blob();            // ya tienes el Blob decodificado
    return new File([blob], filename, { type: blob.type || 'image/png' });
};


const toBase64 = file => new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.readAsDataURL(file);
    reader.onload = () => resolve(reader.result);
    reader.onerror = reject;
});