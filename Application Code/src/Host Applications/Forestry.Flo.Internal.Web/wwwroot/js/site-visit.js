$(function () {

    $(document).ready(setButtonState);

    $('#not-needed-reason-text').on('change keyup paste selectionchange', setButtonState);

    function setButtonState() {

        var text = $('#not-needed-reason-text').val();

        if (text == null || text.trim() === '') {
            $('#save-not-needed-btn').attr('disabled', 'disabled');
        } else {
            $('#save-not-needed-btn').removeAttr('disabled');
        }
    }

});