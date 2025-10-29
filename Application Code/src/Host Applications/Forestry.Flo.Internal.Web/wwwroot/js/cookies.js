$(document).ready(function () {
    $('#saveCookieSettingsButton').click(function () {
        var value = $('input[name="functional-cookies"]:checked').val();
        if (value == "yes" || value == "no") {
            var now = new Date();
            var expires = (new Date(now.getFullYear() + 1, 12, 31).toUTCString());
            document.cookie = '.AspNet.Consent=' + value + ';expires=' + expires + ';path=/;';

            window.scrollTo({ top: 0, behavior: 'smooth' });

            showOrHideElement('.govuk-cookie-banner', false);
            showOrHideElement('.govuk-notification-banner', true);
        }
    });

    var acceptButton = document.querySelector(".govuk-button-group button[data-cookie-string]");

    if (acceptButton) {
        acceptButton.addEventListener("click", function (event) {
            var now = new Date();
            var expires = (new Date(now.getFullYear() + 1, 12, 31).toUTCString());
            document.cookie = '.AspNet.Consent=yes;expires=' + expires + ';path=/;';
            showOrHideElement('#cookieMessage', false);
            showOrHideElement('#accepted', true);
        }, false);
    }

    var rejectButton = document.querySelector("#rejectButton");

    if (rejectButton) {
        rejectButton.addEventListener("click", function (event) {
            var now = new Date();
            var expires = (new Date(now.getFullYear() + 1, 12, 31).toUTCString());
            document.cookie = '.AspNet.Consent=no;expires=' + expires + ';path=/;';
            showOrHideElement('#cookieMessage', false);
            showOrHideElement('#rejected', true);
        }, false);
    }

    var acceptHideCookieButton = document.querySelector('#acceptHideCookieButton');

    if (acceptHideCookieButton) {
        acceptHideCookieButton.addEventListener("click", function (event) {
            showOrHideElement('.govuk-cookie-banner', false);
        }, false);
    }

    var rejectHideCookieButton = document.querySelector('#rejectHideCookieButton');

    if (rejectHideCookieButton) {
        rejectHideCookieButton.addEventListener("click", function (event) {
            showOrHideElement('.govuk-cookie-banner', false);
        }, false);
    }
});

function showOrHideElement($element, show) {
    const element = $($element);
    if (show) {
        element.show();
        element.removeAttr('aria-hidden');
    } else {
        element.hide();
        element.attr('aria-hidden', 'true');
    }
};