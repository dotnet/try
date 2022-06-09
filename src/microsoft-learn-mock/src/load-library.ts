export function loadLibrary<T>(
    url: string,
    integrityHash: string | null,
    globalName?: string,
    timeLimit?: number
): Promise<T | undefined> {
    return new Promise((resolve, reject) => {
        if (timeLimit) {
            setTimeout(() => {
                reject(`${url} load timeout`);
            }, timeLimit);
        }

        const script = document.createElement('script');
        script.src = url;
        script.async = true;
        script.defer = true;
        script.onload = resolve;
        if (integrityHash) {
            // script.integrity = integrityHash;
            script.crossOrigin = 'anonymous';
        }
        script.onerror = () => {
            reject(`Failed to load ${url}`);
        };
        (document.body || document.head).appendChild(script);
    }).then(() => {
        if (globalName === undefined) {
            return undefined;
        }
        if ((window as { [key: string]: any })[globalName] === undefined) {
            throw new Error(`${url} loaded successfully but ${globalName} is undefined.`);
        }
        return (window as { [key: string]: any })[globalName] as Promise<T>;
    });
}