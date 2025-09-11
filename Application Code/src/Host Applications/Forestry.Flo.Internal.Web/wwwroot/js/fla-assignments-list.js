$(function() {

    let params = new URLSearchParams(window.location.search);

    let _assignedToUserOnly = false;

    const _currentPath = window.location.href.split('?')[0];

    // Search elements
    const $searchInput = $('#search');
    const $searchButton = $('#searchButton');

    if (localStorage['currentPageUrl'] === _currentPath) {
        $(document).scrollTop(localStorage['flaAssignmentsScrollPosition']);
    }

    $(document).scroll(function () {
        localStorage['currentPageUrl'] = _currentPath;
        localStorage['flaAssignmentsScrollPosition'] = $(document).scrollTop();
    });

    // Iterate through parameters as read on load and do CSS styling for active filters
    renderUrlParameterSelections();

    // Search handling
    function updateSearchButtonState() {
        const hasText = ($searchInput.val() || '').trim().length > 0;
        const hasSearchParam = params.has('search');
        const shouldEnable = hasText || hasSearchParam; // if a search param exists, keep button enabled
        if (shouldEnable) {
            $searchButton.removeClass('govuk-button--disabled')
                .removeAttr('aria-disabled')
                .prop('disabled', false);
        } else {
            $searchButton.addClass('govuk-button--disabled')
                .attr('aria-disabled', 'true')
                .prop('disabled', true);
        }
    }

    function applySearchAndRedirect() {
        const term = ($searchInput.val() || '').trim();
        if (term.length > 0) {
            params.set('search', term);
        } else {
            params.delete('search');
        }
        redirectOnNewParameters(true);
    }

    $searchInput.on('input change keyup', function () {
        updateSearchButtonState();
    });

    $searchInput.on('keydown', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            // Allow sending empty when a search param exists to clear it
            if ($searchButton.hasClass('govuk-button--disabled') && !params.has('search')) return;
            applySearchAndRedirect();
        }
    });

    $searchButton.on('click', function (e) {
        e.preventDefault();
        if ($searchButton.hasClass('govuk-button--disabled')) return;
        applySearchAndRedirect();
    });

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

        // Sync search input from URL and set initial button state
        const searchTerm = params.get('search') || '';
        $searchInput.val(searchTerm);
        updateSearchButtonState();
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
});