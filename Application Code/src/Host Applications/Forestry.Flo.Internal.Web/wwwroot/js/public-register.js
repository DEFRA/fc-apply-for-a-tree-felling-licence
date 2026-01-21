$(function () {

    $(document).ready(function () {
        if ($('#PublicRegister_WoodlandOfficerSetAsExemptFromConsultationPublicRegister').length) {
            setExemptOnOff();
        }
    });

    $('#PublicRegister_WoodlandOfficerSetAsExemptFromConsultationPublicRegister').change(setExemptOnOff);
    $('#exempt-no').change(setExemptOnOff);


    function setExemptOnOff() {

        var exemptTrue = $('#PublicRegister_WoodlandOfficerSetAsExemptFromConsultationPublicRegister').is(':checked') === true;
        var exemptFalse = $('#exempt-no').is(':checked') === true;

        $('#save-exemption-form').addClass('govuk-visually-hidden');
        $('#save-exemption-form').attr('aria-hidden', 'true');

        $('#publish-form').addClass('govuk-visually-hidden');
        $('#publish-form').attr('aria-hidden', 'true');

        $('#back-link-no-forms').removeClass('govuk-visually-hidden');
        $('#back-link-no-forms').attr('aria-hidden', 'false');

        if (exemptTrue) {
            $('#save-exemption-form').removeClass('govuk-visually-hidden');
            $('#save-exemption-form').attr('aria-hidden', 'false');

            $('#back-link-no-forms').addClass('govuk-visually-hidden');
            $('#back-link-no-forms').attr('aria-hidden', 'true');
        }

        if (exemptFalse) {
            $('#exempt-backing-field').val(false);

            $('#publish-form').removeClass('govuk-visually-hidden');
            $('#publish-form').attr('aria-hidden', 'false');

            $('#back-link-no-forms').addClass('govuk-visually-hidden');
            $('#back-link-no-forms').attr('aria-hidden', 'true');
        }
    }

});