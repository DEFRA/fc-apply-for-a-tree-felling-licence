$(function () {

    const options = {
        "language": {
            "emptyTable": "No compartments have been defined."
        },
        "scrollCollapse": true,
        "scrollY": 300,
        "paging": false,
        "searching": false,
        "ordering": false
    };

    $('#compartment-list-table')
        .DataTable(options);

    $('.dataTables_info').addClass('govuk-body');
    $('#compartment-list-table_filter').addClass('govuk-body govuk-form-group');
    $('#compartment-list-table_filter > label >input').addClass('govuk-input');
});