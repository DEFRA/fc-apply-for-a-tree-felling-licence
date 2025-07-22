$(function () {

    const options = {
        "language": {
            "emptyTable": "No users were found."
        },
        "scrollCollapse": true,
        "scrollY": 300,
        "paging": false,
        "search": {regex: true}
    };

    var table = $('#user-list-table').DataTable(options);

    $('.dataTables_info').addClass('govuk-body');
    $('.dataTables_filter').addClass('govuk-body');

    table.on('draw', function () {

        var currentSelection = $('#selected-user-id').val();
        $('#selected-user-id').val(null);
        var table = $(this).DataTable();

        if ($('#assign-to-me-radio').length) {
            if ($('#assign-to-me-radio').is(":checked")) {
                // assign to me radio button is selected so selected user should remain as current user
                $('#selected-user-id').val($('#assign-to-me-radio').val());
                table.$('tbody tr.chosen').removeClass('chosen');
                return;
            }
        }

        var selectedRows = table.$('tbody tr.chosen').length;

        table.$('tbody tr.chosen').each(function () {
            if ($(this).attr('data-id') === currentSelection || selectedRows === 1) {
                $('#selected-user-id').val($(this).attr('data-id'));
            } else {
                $(this).removeClass('chosen');
            }
        });
    });

    $('#user-list-table tbody').on('click', 'tr', function () {
        $('#assign-to-me-radio').prop("checked", false);

        if ($(this).hasClass('chosen')) {
            $(this).removeClass('chosen');
            $('#selected-user-id').val(null);
        } else {
            $('#user-list-table').DataTable().$('tbody tr.chosen').removeClass('chosen');
            $(this).addClass('chosen');
            $('#selected-user-id').val($(this).attr('data-id'));
        }
    });

    $('#assign-to-me-radio').on('change', function() {
        if ($(this).is(":checked")) {
            $('#user-list-table').DataTable().$('tbody tr.chosen').removeClass('chosen');
            $('#selected-user-id').val($(this).val());
        }
    });

});