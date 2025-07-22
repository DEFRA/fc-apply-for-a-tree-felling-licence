$(document).ready(function () {
    $('#confirm-reopen-button').prop('disabled', !$('#confirm-reopen-check').is(':checked'));

    $('#confirm-reopen-check').on('click', function (e) {
        $('#confirm-reopen-button').prop('disabled', !this.checked);
    });
});