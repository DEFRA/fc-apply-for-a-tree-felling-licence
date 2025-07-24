$(function () {

    const options = {
        "language": {
            "emptyTable": "No Woodlands have been created."
        },
        "scrollCollapse": true,
        "scrollY": 300,
        "paging": false,
        "searching" : false
    };

    $('#property-list-table').DataTable(options);

    $('.dataTables_info').addClass('govuk-body');
    $('#property-list-table_filter').addClass('govuk-body govuk-form-group');
    $('#property-list-table_filter > label >input').addClass('govuk-input');

});