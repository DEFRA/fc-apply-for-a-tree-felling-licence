$(function () {

    $(document).ready(setButtonState);
    $(document).ready(setAssignButtonText);
    $('#review-complete-check').on('change', setButtonState);

    function setAssignButtonText() {
        var assignedWoodlandOfficer = $("#assigned-woodland-officer").html();
        if (assignedWoodlandOfficer != "None") {
            $("#assign-woodland-officer").html("Reassign");
        } else {
            $("#assign-woodland-officer").html("Assign");
        }
    }

    function setButtonState() {
        var stepsComplete = $('#steps-complete').val() === "True";

        if (stepsComplete) {
            $('#submit-admin-officer-review').removeAttr('disabled');
        } else {
            $('#submit-admin-officer-review').attr('disabled', 'disabled');
        }
    }

});