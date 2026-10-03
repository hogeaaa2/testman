// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

function initializeDisclosureState(root, storage) {
    root.querySelectorAll("details[data-disclosure-key]").forEach((details) => {
        const storageKey = `testman:disclosure-open:${details.dataset.disclosureKey}`;

        try {
            const savedState = storage?.getItem(storageKey);
            if (savedState != null) {
                details.open = savedState === "true";
            }
        } catch {
            // Keep the server-rendered default when browser storage is unavailable.
        }

        details.addEventListener("toggle", () => {
            try {
                storage?.setItem(storageKey, String(details.open));
            } catch {
                // The disclosure remains usable even when browser storage is unavailable.
            }
        });
    });
}

if (typeof document !== "undefined") {
    try {
        initializeDisclosureState(document, window.sessionStorage);
    } catch {
        initializeDisclosureState(document, null);
    }
}

if (typeof module !== "undefined") {
    module.exports = { initializeDisclosureState };
}
