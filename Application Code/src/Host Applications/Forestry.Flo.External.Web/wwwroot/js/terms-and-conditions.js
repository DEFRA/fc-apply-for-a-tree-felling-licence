$(function () {

    $(document).ready(setButtonState);

    $('#accept-privacy-policy').on('change', setButtonState);
    $('#accept-terms-and-conditions').on('change', setButtonState);

    function setButtonState() {
        const acceptedPrivacy = $('#accept-privacy-policy').prop("checked") === true;
        const acceptedTAndCs = $('#accept-terms-and-conditions').prop("checked") === true;

        if (acceptedPrivacy && acceptedTAndCs) {
            $('#accept-terms-btn').removeAttr('disabled');
        } else {
            $('#accept-terms-btn').attr('disabled', 'disabled');
        }
    }

});