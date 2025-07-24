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

    $('#fc-staff-table').DataTable(options);

    $('#fc-staff-table').DataTable().on('search.dt', function () {
        removeSelection();
    });

    $('.dataTables_info').addClass('govuk-body');
    
    $('#fc-staff-table tbody').on('click', 'tr', function () {
        if ($(this).hasClass('chosen')) {
            $(this).removeClass('chosen');
            $('#selected-user-id').val(null);
        } else {
            $('#fc-staff-table').DataTable().$('tbody tr.chosen').removeClass('chosen');
            $(this).addClass('chosen');
            $('#selected-user-id').val($(this).attr('data-id'));
        }

        toggleButtons();
    });

    function toggleButtons() {
        const selectedId = $('#selected-user-id');

        if ($(selectedId).val() === undefined || $(selectedId).val() === null || $(selectedId).val() === "") {
            $('#amend-submit').attr('disabled', 'disabled');
        } else {
            $('#amend-submit').removeAttr('disabled');
        }
    };

    function removeSelection() {
        $('#fc-staff-table').DataTable().$('tbody tr.chosen').removeClass('chosen');
        $('#selected-user-id').val(null);

        toggleButtons();
    }

    filterTableByAccountTypes();

    function filterTableByAccountTypes(removeChosen = true) {
        if (removeChosen) {
            removeSelection();
        }

        var value = '';

        $('button.selected').each(function() {
            value += $(this).attr('data-id') + "|";
        });

        value = value.slice(0, -1).replace("&", "\\&").replace(/\s/g, "\\s");;

        $('#fc-staff-table').DataTable().column(2).search(value, true, false).draw();
    }

    $('button.filter').each(function() {
        $(this).on('click', function () {
            if ($(this).hasClass('selected')) {
                $(this).removeClass('selected');
                $(this).addClass('unselected');
            } else {
                $(this).removeClass('unselected');
                $(this).addClass('selected');
            }
            filterTableByAccountTypes();
        });
    });

    $(document).ready(function() {
        $('#amend-submit').attr('disabled', 'disabled');
    });

    $(window).resize(function() {
        filterTableByAccountTypes(false);
    });
});