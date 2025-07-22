$(document).ready(function () {
    $('#saveCookieSettingsButton').click(function () {
        var value = $('input[name="functional-cookies"]:checked').val();
        if (value == "yes" || value == "no") {
            var now = new Date();
            var expires = (new Date(now.getFullYear() + 1, 12, 31).toUTCString());
            document.cookie = '.AspNet.Consent=' + value + ';expires=' + expires + ';path=/;';

            window.scrollTo({ top: 0, behavior: 'smooth' });

            $('.govuk-cookie-banner').hide();
            $('.govuk-notification-banner').show();
        }
    });
});