$(function () {

    $(document).ready(setButtonState);

    $('#AcceptsTermsAndConditions_AcceptsPrivacyPolicy').on('change', setButtonState);
    $('#AcceptsTermsAndConditions_AcceptsTermsAndConditions').on('change', setButtonState);

    function setButtonState() {
        const acceptedPrivacy = $('#AcceptsTermsAndConditions_AcceptsPrivacyPolicy').prop("checked") === true;
        const acceptedTAndCs = $('#AcceptsTermsAndConditions_AcceptsTermsAndConditions').prop("checked") === true;

        if (acceptedPrivacy && acceptedTAndCs) {
            $('#accept-terms-btn').removeAttr('disabled');
        } else {
            $('#accept-terms-btn').attr('disabled', 'disabled');
        }
    }

});