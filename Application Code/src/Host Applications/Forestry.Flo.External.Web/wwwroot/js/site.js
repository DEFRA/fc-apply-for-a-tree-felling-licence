$(function () {
    // Show the hide cookie button when JavaScript is available
    $('#hideCookieButton').parent('.js-hidden').removeClass('js-hidden');
    
    // Handle click event for hiding the cookie banner
    $('#hideCookieButton').on('click', function() {
        $('.govuk-cookie-banner').hide();
    });
});