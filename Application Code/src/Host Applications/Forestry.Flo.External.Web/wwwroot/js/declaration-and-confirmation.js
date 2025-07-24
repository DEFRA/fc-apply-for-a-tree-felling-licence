$(function () {

    $(document).ready(setButtonState);

    $('#TermsAndConditionsAccepted').on('change', setButtonState);

    function setButtonState() {
        $('button[type="submit"]').prop('disabled', !$('#TermsAndConditionsAccepted').is(':checked'));
    }

});