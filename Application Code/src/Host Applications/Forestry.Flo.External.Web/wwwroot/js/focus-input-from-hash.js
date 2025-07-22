function focusInputFromHash() {
    if (location.hash) {
        const hash = location.hash.substring(1);
        const inputId = hash.endsWith("Label") ? hash.slice(0, -"Label".length) : hash;
        const label = document.querySelector(`label[for='${inputId}']`);

        if (label) {
            label.click();
            setTimeout(function() {
                label.click();
            }, 400);
        }
    }
}

document.addEventListener("DOMContentLoaded", focusInputFromHash);
window.addEventListener("pageshow", function (event) {
    // The 'persisted' property is true when coming from bfcache (back/forward navigation)
    if (event.persisted) {
        focusInputFromHash();
    }
});
