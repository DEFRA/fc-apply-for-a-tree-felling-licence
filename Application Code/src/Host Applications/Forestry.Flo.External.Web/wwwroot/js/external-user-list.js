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

    $(window).resize(function () {
        $('#external-user-table').DataTable().draw();
    });
});