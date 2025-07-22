$(function () {

    $(document).ready(setButtonState);

    $('#add-case-note-text').on('change keyup paste selectionchange', setButtonState);

    function setButtonState() {

        var text = $('#add-case-note-text').val();

        if (text == null || text.trim() === '') {
            $('#save-case-note-btn').attr('disabled', 'disabled');
        } else {
            $('#save-case-note-btn').removeAttr('disabled');
        }
    }

});