$(function () {

    $(document).ready(setButtonState);

    function setButtonState() {
        var stepsComplete = $('#steps-complete').val() === "True";
        var fmAssigned = $('#fm-assigned').html() !== 'None';

        if (stepsComplete && fmAssigned) {
            $('#submit-review-btn').removeAttr('disabled');
        } else {
            $('#submit-review-btn').attr('disabled', 'disabled');
        }
    }

});