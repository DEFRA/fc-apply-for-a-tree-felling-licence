$(function () {

    $(document).ready(setWarningStatus);
    $('#RecommendedLicenceDuration').change(setWarningStatus);

    function setWarningStatus() {
        
        var tenYearsSelected = $('#RecommendedLicenceDuration').find(':selected').val() === "10";

        var isTenYearApplication = $('#is-for-ten-year').val().toLowerCase() === 'true';

        if (isTenYearApplication) {

            if (tenYearsSelected) {
                $('#ten-year-warning').addClass('govuk-visually-hidden');
                $('#ten-year-warning').attr('aria-hidden', 'true');
            } else {
                $('#ten-year-warning').removeClass('govuk-visually-hidden');
                $('#ten-year-warning').removeAttr('aria-hidden');
            }
        }
    }
});