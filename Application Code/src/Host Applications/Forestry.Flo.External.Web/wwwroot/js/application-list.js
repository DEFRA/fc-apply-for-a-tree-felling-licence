$(function () {
    const searchElement = document.querySelector('#search');   

    const applySearchTerms = function (filterString) {
        $('#application-list-table tbody tr').not('#no-applications-found').filter(function () {
            if ($.trim($(this).text().toLowerCase()).indexOf(filterString.toLowerCase()) >= 0) {
                $(this).show();
                return;
            }
            $(this).hide();
        });
    };

    function tableInfo() {
        const totalDataRows = $('#application-list-table tbody').find('tr.data-row:visible').length;
        if (totalDataRows === 0 && $('#no-applications-found').length === 0) {
            $('#application-list-table > tbody').append('<tr class="govuk-table__row" id="no-applications-found"><td valign="top" colspan="4" class="govuk-table__cell">No matching applications found</td></tr>');
            return;
        }
        if (totalDataRows > 0) {
            if ($('#no-applications-found').length > 0) {
                $('#no-applications-found').remove();
            }
        }
    };

    const filterTable = function () {
        applySearchTerms($(searchElement).val());
        tableInfo();
    };

    $(document).ready(tableInfo);
    $(searchElement).on("input", filterTable);
});

