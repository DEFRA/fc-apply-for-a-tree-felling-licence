$(function () {

    const options = {
        "language": {
            "emptyTable": "No agent authority forms fit this criteria."
        },
        "scrollCollapse": true,
        "scrollY": 300,
        "paging": false,
        "searching": true,
        "dom": 'lrtip',
        "order": [1, 'asc']
    };

    var authoritiesTable = $('#agent-authorities-list-table').DataTable(options);
    $('.dataTables_info').addClass('govuk-body');
    $('#agent-authorities-list-table_filter').addClass('govuk-body govuk-form-group');
    $('#agent-authorities-list-table_filter > label >input').addClass('govuk-input');

    $(document).ready(function() {
        $('#agent-authorities-list-table').dataTable().fnDraw();
    });


    $.fn.dataTable.ext.search.push(
        function (settings, data, dataIndex) {
            if (settings.nTable.id !== 'agent-authorities-list-table') {
                return true;
            }
            var checked = $('#show-deactivated').prop("checked") === true;
            var hasForm = data[2].trim().toLowerCase() == "deactivated";
            
            if (hasForm === false || checked === true) {
                return true;
            }
            return false;
        }
    );

    $('#show-deactivated').change(function () {
        $('#agent-authorities-list-table').dataTable().fnDraw();
    });
});