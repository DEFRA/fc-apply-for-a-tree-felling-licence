$(function() {

    let params = new URLSearchParams(window.location.search);

    let _assignedToUserOnly = false;

    const _currentPath = window.location.href.split('?')[0];

    if (localStorage['currentPageUrl'] === _currentPath) {
        $(document).scrollTop(localStorage['flaAssignmentsScrollPosition']);
    }

    $(document).scroll(function () {
        localStorage['currentPageUrl'] = _currentPath;
        localStorage['flaAssignmentsScrollPosition'] = $(document).scrollTop();
    });

    $(document).ready(tableInfo);

    // Iterate through parameters as read on load and do CSS styling for active filters
    renderUrlParameterSelections();

    $('[fla-status-filter]').click(function (e) {
        e.preventDefault();

        const filterValue = $(this).attr('fla-status-filter');

        // Toggle value in multi-value param fellingLicenceStatusArray
        const existing = params.getAll('fellingLicenceStatusArray');
        if (existing.includes(filterValue)) {
            // remove all occurrences of this value
            const kept = existing.filter(v => v !== filterValue);
            params.delete('fellingLicenceStatusArray');
            kept.forEach(v => params.append('fellingLicenceStatusArray', v));
        } else {
            params.append('fellingLicenceStatusArray', filterValue);
        }

        renderUrlParameterSelections();
        redirectOnNewParameters(true);
    });

    $('#fla-assigned-to-user-filter').click(function(e) {
        e.preventDefault();

        _assignedToUserOnly = !(params.get('assignedToUserOnly') === 'true');
        params.set('assignedToUserOnly', _assignedToUserOnly ? 'true' : 'false');

        renderUrlParameterSelections();
        redirectOnNewParameters(true);
    });

    $("#keywords-text").on("input", function () {
        const filterString = $("#keywords-text").val();
        $('#application-list-table tbody tr').not('#no-applications-found').filter(function () {
            if ($.trim($(this).text().toLowerCase()).indexOf(filterString.toLowerCase()) >= 0) {
                $(this).show();
                return;
            }
            $(this).hide();
        });

        tableInfo();
    });

    $('#clear-all-filters').click(function(e) {
        e.preventDefault();
        const currentPathUrl = window.location.href.split('?')[0];
        location.replace(currentPathUrl);
    });

    function renderUrlParameterSelections() {
        // Clear existing selection styling
        applySelectionCss($('[fla-status-filter]'), false);
        applySelectionCss($('#fla-assigned-to-user-filter'), false);

        // Status selections
        const selectedStatuses = params.getAll('fellingLicenceStatusArray');
        selectedStatuses.forEach(s => {
            applySelectionCss($('[fla-status-filter="' + s + '"]'), true);
        });

        // Assigned to user
        if (params.get('assignedToUserOnly') === 'true') {
            applySelectionCss($('#fla-assigned-to-user-filter'), true);
            _assignedToUserOnly = true;
        } else {
            applySelectionCss($('#fla-assigned-to-user-filter'), false);
            _assignedToUserOnly = false;
        }
    }

    /*
     * Construct the query string and navigate to load data
     */
    function redirectOnNewParameters(resetPage) {
        if (resetPage) {
            // reset page when changing filters to avoid empty result sets
            params.delete('page');
            params.set('page', '1');
        }

        const redirectUrl = window.location.href.split('?')[0].replace('#', '');
        const queryString = params.toString();
        location.replace(queryString ? (redirectUrl + '?' + queryString) : redirectUrl);
    }

    function applySelectionCss($selector, selected) {
        if (selected) {
            $selector.css('outline', '3px solid orange');
        } else {
            $selector.css('outline', 'none');
        }
    }

    function tableInfo() {
        const totalDataRows = $('#application-list-table tbody').find('tr.data-row:visible').length;
        const colSpan = $('#application-list-table thead th').length || 5;
        if (totalDataRows === 0 && $('#no-applications-found').length === 0) {
            $('#application-list-table > tbody').append('<tr class="govuk-table__row" id="no-applications-found"><td valign="top" colspan="' + colSpan + '" class="govuk-table__cell">No applications found.</td></tr>');
            return;
        }
        if (totalDataRows > 0) {
            if ($('#no-applications-found').length > 0) {
                $('#no-applications-found').remove();
            }
        }
    };
});