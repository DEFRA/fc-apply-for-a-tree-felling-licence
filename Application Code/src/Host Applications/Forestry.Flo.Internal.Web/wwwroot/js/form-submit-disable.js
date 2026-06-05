const forms = document.getElementsByTagName("form");
for (const form of forms) {
    form.addEventListener("submit", onFormSubmit);
}


function onFormSubmit(event) {
    const form = event.target;
    const submit = form.querySelector("[type=submit]");
    if (!submit) {
        return;
    }

    if (submit.tagName !== "BUTTON") {
        return;
    }

    submit.textContent = "Please wait...";
    submit.setAttribute("aria-busy", true);
    submit.setAttribute("aria-label", "Please wait...");
    submit.attr('disabled', 'disabled');
}