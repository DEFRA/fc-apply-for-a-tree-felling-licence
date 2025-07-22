$(function () {

    const confirmedFellingType = 'confirmed-felling-type';
    const confirmedFellingTreeSpecies = 'confirmed-felling-tree-species'

    const filterCategories = [confirmedFellingTreeSpecies, confirmedFellingType];

    const filtercategory_dataattribute = 'data-filtercategory'
    const filter_data_key = 'data-key'
    const filter_data_name = 'data-name'

    function clearErrorAndInformationNotices() {
        $("div.govuk-notification-banner").remove();
        $("div.govuk-error-summary").remove();
        $('div.govuk-form-group--error').removeClass('govuk-form-group--error');
        $('input').removeClass('govuk-input--error');
    }

    function filteredSelections(filterCategory) {
        return $('div#' + filterCategory +'>ul>li').map(function () {
            return {
                key: $(this)[0].getAttribute(filter_data_key),
                name: $(this)[0].getAttribute(filter_data_name)
            };
        }).get();
    }

    function showNoFilterAppliedMessage(filterCategory) {
        $('#' + filterCategory + '>h3>span').show();
    }
    function hideNoFilterAppliedMessage(filterCategory) {
        $('#' + filterCategory + '>h3>span').hide();
    }

    function removeFromFilter(e) {
        let filterCategory = e.currentTarget.parentElement.getAttribute(filtercategory_dataattribute);
        let itemToRemove = (e.target.parentNode)
        let itemKeyToRemove = e.currentTarget.parentElement.getAttribute(filter_data_key);
        let selectedFiltersForCategoryArray = filteredSelections(filterCategory);

        const index = selectedFiltersForCategoryArray.findIndex(o => o.key === itemKeyToRemove)

        if (index > -1) {
            selectedFiltersForCategoryArray.splice(index, 1);
        }

        itemToRemove.remove();

        let filterSelectOptions = $('#' + filterCategory.replace('-filters', '-select') + ' option');
        filterSelectOptions.filter(function () { return this.value === itemKeyToRemove}).attr('disabled', false);
    
        //if all filters for the filter type are gone, then default to 'all' and advise user.
        if ($('#' + filterCategory + '>ul>li').length === 0) {
            showNoFilterAppliedMessage(filterCategory);
        }
    }

    function addInputForViewModel(filterCategory) {
        var filterCategoryFull = filterCategory + "-filters";
        var selectedValues = filteredSelections(filterCategoryFull);

        var addInput = function (index, valueToAdd) {

            $.each(['key', 'name'], function (i, propertyName) {
                $hdnInput = $("<input>")
                    .attr("type", "hidden")
                    .addClass('model')
                    .attr("value", valueToAdd[propertyName]);

                if (filterCategory === confirmedFellingTreeSpecies) {
                    $hdnInput.attr("name", filterCategoryFull + "[" + index + "]." + propertyName);
                }
                else {
                    //everything else is just an enum
                    $hdnInput.attr("name", filterCategoryFull + "[" + index + "]");
                }

                $('div.moj-filter__content').append($hdnInput);
            });
        }

        $.each(selectedValues, function (i, filterValue) {
            addInput(i, filterValue);
        });
    }

    function applyAllFiltersToViewModel() {
        $('input:hidden[class="model"]').remove();
        $.each(filterCategories, function (i, category) {
            addInputForViewModel(category);
        });
    };  

    function appendFilterToCategory(selectedText, selectedValue, filterCategoryId) {
        const $link = $("<a/>")
            .text(" " + `${selectedText}`)
            .addClass("moj-filter__tag")
            .attr("href", "#"); //todo just here for the cursor..

        const $span = $("<span>")
            .addClass("govuk-visually-hidden")
            .text("Remove this filter")

        const $filterBullet = $("<li/>")
            .attr(filter_data_key, `${selectedValue}`)
            .attr(filter_data_name, `${selectedText}`)
            .attr(filtercategory_dataattribute, `${filterCategoryId}`);

        $span.appendTo($link);

        $link.appendTo($filterBullet);

        $filterBullet.appendTo("#" + filterCategoryId +">ul");

        $("#" + filterCategoryId +">h3>span.no-filter-applied").hide();
    };
        
    function addFilter(e) {
        e.preventDefault(); // Prevent form submission

        const filterCategory = e.data.category +"-filters";

        const select = e.currentTarget.previousElementSibling;
        const selectedIndex = select.selectedIndex;
        const selectedValue = select.value;
        const selectedText = select.options[selectedIndex].text;   

        let selectedFiltersForCategoryArray = filteredSelections(filterCategory);

        appendFilterToCategory(selectedText, selectedValue, filterCategory);

        selectedFiltersForCategoryArray.push(selectedValue);

        $(select.options[selectedIndex]).attr('disabled', true);

        select.selectedIndex = 0;
    }

    function init() {
        $.each(filterCategories, function (i, filterCategory) {
            let fullFilterCategory = filterCategory +'-filters'
            let appliedFilters = filteredSelections(fullFilterCategory);
            if (appliedFilters.length > 0) {
                hideNoFilterAppliedMessage(fullFilterCategory);

                //set select items to disabled if already added to filter
                let $categorySelectElement = $('#' + filterCategory + '-select option');
                $.each(appliedFilters, function (i, filter) {
                    $categorySelectElement.filter(function () {
                        return (this.value === filter.key);
                    }).attr('disabled', true);
                });
            }
        });
    }

    $(document).ready(function () {

        init();

        $("button#submit-reportquery").click(function (e) {
            e.preventDefault();
            clearErrorAndInformationNotices();
            applyAllFiltersToViewModel();
            $("form").submit();
        });

        $('div.moj-filter__selected').on('click', 'li>a.moj-filter__tag', removeFromFilter);

        //species selection and filter handling:
        $('#add-tree-species-btn').click({ category: confirmedFellingTreeSpecies }, addFilter);
        $('#confirmed-felling-tree-species-select').on('change', function (e) {
            if ($(this).find("option:selected").text() == "Select ...") {
                $('#add-tree-species-btn').prop('disabled', true);
            }
            else {
                $('#add-tree-species-btn').prop('disabled', false);
            }
        });

        //confirmed felling type selection and filter handling:
        $('#add-confirmed-felling-type-btn').click({ category: confirmedFellingType }, addFilter);
        $('#confirmed-felling-type-select').on('change', function (e) {
            if ($(this).find("option:selected").text() == "Select ...") {
                $('#add-confirmed-felling-type-btn').prop('disabled', true);
            }
            else {
                $('#add-confirmed-felling-type-btn').prop('disabled', false);
            }
        });
    });
});
