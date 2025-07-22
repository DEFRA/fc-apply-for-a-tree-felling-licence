$(function () {

    const options = {
        "language": {
            "emptyTable": "No users were found."
        },
        "scrollCollapse": true,
        "scrollY": 300,
        "paging": false,
        "search": { regex: true },
        "order": [[2, 'desc'], [1, 'desc']]
    };

    $('#external-user-table').DataTable(options);

    $('#external-user-table').DataTable().on('search.dt', function () {
        removeSelection();
    });

    $('.dataTables_info').addClass('govuk-body');

    $('#external-user-table tbody').on('click', 'tr', function () {
        if ($(this).hasClass('chosen')) {
            $(this).removeClass('chosen');
            $('#selected-user-id').val(null);
        } else {
            $('#external-user-table').DataTable().$('tbody tr.chosen').removeClass('chosen');
            $(this).addClass('chosen');
            $('#selected-user-id').val($(this).attr('data-id'));
        }

        toggleButtons();
    });

    function toggleButtons() {
        const selectedId = $('#selected-user-id');

        // todo: uncomment close button disabling when close account logic has been implemented

        if ($(selectedId).val() === undefined || $(selectedId).val() === null || $(selectedId).val() === "") {
            $('#amend-submit').attr('disabled', 'disabled');
            //$('#close-submit').attr('disabled', 'disabled');
        } else {
            $('#amend-submit').removeAttr('disabled');

            if ($(selectedId).val() !== $('#viewing-user-id').val()) {
                //$('#close-submit').removeAttr('disabled');
            } else {
                //$('#close-submit').attr('disabled', 'disabled');
            }
        }
    };

    function removeSelection() {
        $('#external-user-table').DataTable().$('tbody tr.chosen').removeClass('chosen');
        $('#selected-user-id').val(null);

        toggleButtons();
    }

    $('button.filter').each(function () {
        $(this).on('click', function () {
            if ($(this).hasClass('selected')) {
                $(this).removeClass('selected');
                $(this).addClass('unselected');
            } else {
                $(this).removeClass('unselected');
                $(this).addClass('selected');
            }
        });
    });

    $(document).ready(function () {
        $('#amend-submit').attr('disabled', 'disabled');
        $('#close-submit').attr('disabled', 'disabled');
    });

    $(window).resize(function () {
        $('#external-user-table').DataTable().draw();
    });
});