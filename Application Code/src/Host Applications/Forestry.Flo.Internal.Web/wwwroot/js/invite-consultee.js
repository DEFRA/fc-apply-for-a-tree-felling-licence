$(function() {
    $(document).ready(function() {

        $('td a').click(function() {
            var id = $(this).data("id");

            $('input[value="' + id + '"]').val(null);

            $(this).parent().parent().remove();
        });
    });
});