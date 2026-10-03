// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.querySelectorAll("details[data-specification-path]").forEach((details) => {
    const storageKey = `testman:specification-open:${details.dataset.specificationPath}`;

    try {
        const savedState = sessionStorage.getItem(storageKey);
        if (savedState !== null) {
            details.open = savedState === "true";
        }
    } catch {
        // Keep the server-rendered default when browser storage is unavailable.
    }

    details.addEventListener("toggle", () => {
        try {
            sessionStorage.setItem(storageKey, String(details.open));
        } catch {
            // The disclosure remains usable even when browser storage is unavailable.
        }
    });
});
