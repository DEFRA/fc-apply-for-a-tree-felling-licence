$(function () {

    $(document).ready(setButtonState);

    $('#Text').on('change keyup paste selectionchange', setButtonState);

    function setButtonState() {

        var text = $('#Text').val();

        if (text == null || text.trim() === '') {
            $('#save-case-note-btn').attr('disabled', 'disabled');
        } else {
            $('#save-case-note-btn').removeAttr('disabled');
        }
    }

});