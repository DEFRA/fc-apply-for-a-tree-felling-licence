$(function() {

    var _urlParameters = getUrlParameters();

    var _assignedToUserOnly = false;

    var _currentPath = window.location.href.split('?')[0];

    if (localStorage['currentPageUrl'] === _currentPath) {
        $(document).scrollTop(localStorage['flaAssignmentsScrollPosition']);
    }

    $(document).scroll(function () {
        localStorage['currentPageUrl'] = _currentPath;
        localStorage['flaAssignmentsScrollPosition'] = $(document).scrollTop();
    });

    $(document).ready(tableInfo);

    // Iterate through parameters as read on load and do CSS styling for active filters

    renderUrlParameterSelections(_urlParameters);

    $('[fla-status-filter]').click(function (e) {

        e.preventDefault();

        var filterValue = $(this).attr('fla-status-filter');

        // If array contains the key and val, remove it, else add it

        if (_urlParameters.filter(e => e['key'] === 'fellingLicenceStatusArray' && e['val'] === filterValue).length > 0) {

            _urlParameters = _urlParameters.filter(e => !(e['key'] === 'fellingLicenceStatusArray' && e['val'] === filterValue));

        } else {

            _urlParameters.push({ 'key': 'fellingLicenceStatusArray', 'val': filterValue });
        }

        renderUrlParameterSelections(_urlParameters);

        redirectOnNewParameters();
    });

    $('#fla-assigned-to-user-filter').click(function(e) {

        e.preventDefault();

        // Test if the array has an object with key assignedToUserOnly

        var idx = _urlParameters.findIndex((e => e['key'] === 'assignedToUserOnly'));

        _assignedToUserOnly = !_assignedToUserOnly;

        if (idx < 0) {

            // If it doesn't exist, add it

            _urlParameters.push({ 'key': 'assignedToUserOnly', 'val': _assignedToUserOnly ? 'true' : 'false' });

        } else {

            // Else add the value

            _urlParameters[idx]['val'] = (_assignedToUserOnly).toString();
        }

        renderUrlParameterSelections(_urlParameters);

        redirectOnNewParameters();
    });

    $("#keywords-text").on("input", function (e) {
        var filterString = $("#keywords-text").val();
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

        var currentPathUrl = window.location.href.split('?')[0];

        location.replace(currentPathUrl);
    });

    function renderUrlParameterSelections(urlParameters) {

        // Clear existing selection styling

        applySelectionCss($('[fla-status-filter]'), false);
        applySelectionCss($('#fla-assigned-to-user-filter'), false);

        // Iterate over parameters array to determine, which selection stylings to apply

        for (var i = 0; i < urlParameters.length; i++) {

            console.log(urlParameters[i]);

            // Multiple keys may match fellingLicenceStatusArray as this binds
            // to ASP.NET MVC controller array argument

            if (urlParameters[i]['key'] === 'fellingLicenceStatusArray') {

                applySelectionCss($('[fla-status-filter="' + urlParameters[i]['val'] + '"]'), true);
            }

            if (urlParameters[i]['key'] === 'assignedToUserOnly') {

                if (urlParameters[i]['val'] === 'true') {

                    applySelectionCss($('#fla-assigned-to-user-filter'), true);

                    _assignedToUserOnly = true;

                } else {

                    applySelectionCss($('#fla-assigned-to-user-filter'), false);

                    _assignedToUserOnly = false;
                }
            }
        }
    }

    /*
     * Split query string parameters into an array of key value pairs
     */
    function getUrlParameters() {

        var vars = [];

        if (window.location.href.indexOf('?') > -1) {

            // If we have a querystring, break into key values

            var parameterKeyValues = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');

            // Start from index 1 as 0 is path

            for (var i = 0; i < parameterKeyValues.length; i++) {

                // Check for empty string before split

                if (parameterKeyValues[i]) {

                    // If we have a value after split, add to parameters

                    var parameterKeyValue = parameterKeyValues[i].split('=');
                    vars.push({ 'key': parameterKeyValue[0], 'val': parameterKeyValue[1] });
                }
            }
        }

        return vars;
    }


    /*
     * Construct the query string and navigate to load data
     */
    function redirectOnNewParameters() {

        var queryString = '?';

        for (var i = 0; i < _urlParameters.length; i++) {

            queryString += _urlParameters[i]['key'] + '=' + _urlParameters[i]['val'];

            if (i + 1 < _urlParameters.length) {

                queryString += '&';
            }
        }

        var redirectUrl = window.location.href.split('?')[0];

        redirectUrl = redirectUrl.replace('#', '');

        redirectUrl += queryString;

        location.replace(redirectUrl);
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
        if (totalDataRows === 0 && $('#no-applications-found').length === 0) {
            $('#application-list-table > tbody').append('<tr class="govuk-table__row" id="no-applications-found"><td valign="top" colspan="5" class="govuk-table__cell">No applications found.</td></tr>');
            return;
        }
        if (totalDataRows > 0) {
            if ($('#no-applications-found').length > 0) {
                $('#no-applications-found').remove();
            }
        }
    };
});