$(function () {

    var applicationId = $('#applicationId').val();

    $('#assign-application-submit-btn').click(function(e) {

        if ($('#assign-back-to-applicant:checked').val()) {

            e.preventDefault();

            // In the case of assigning back to the applicant, we prevent the form
            // post back and redirect to a dedicated controller to handle this

            var redirectUrl = '/AssignFellingLicenceApplication/AssignBackToApplicant?applicationId=' + applicationId;

            window.location = redirectUrl;
        }
    });
});