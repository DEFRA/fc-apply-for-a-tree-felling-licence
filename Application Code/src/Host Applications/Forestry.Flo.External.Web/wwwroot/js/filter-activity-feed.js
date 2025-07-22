$(function () {
    $('#activity-feed-filter').on('change', dynamicallyHideFilteredItems);
    $(document).ready(function () {
        setDefaultFilter();
        dynamicallyHideFilteredItems();
    }
    );

    var filterEnabled = false;

    function dynamicallyHideFilteredItems() {
        const activityItems = document.querySelectorAll(".moj-timeline__item");
        var count = 0;

        if (filterEnabled) {
            var types = [];
            $('button.activity-item-type.selected').each(function () {
                types.push($(this).attr('data-id'));
            });

            for (let i = 0; i < activityItems.length; i++) {
                var activityItem = activityItems[i];
                var type = activityItem.getAttribute("data-type");

                if (types.includes(type))
                    toggleVisibility(activityItem, true);
                else toggleVisibility(activityItem, false);

                if (activityItem.style.display === "block") count++;
            }
        } else count = activityItems.length;


        var emptyElement = document.getElementById("empty-activity-feed-item");
        emptyElement.style.display = count === 0 ? "block" : "none";
    }

    function setDefaultFilter() {
        if (filterEnabled) return;

        var defaultFilterElement = document.getElementById("DefaultCaseNoteFilter");
        if (typeof (defaultFilterElement) != 'undefined' && defaultFilterElement != null) {
            var value = defaultFilterElement.value;
            filterEnabled = true;
            if (typeof value === "undefined" || value === null || value === "") {
                toggleMasterSelection(true);
                toggleMasterCategory();
                return;
            }

            document.getElementById(value).classList.remove("unselected");
            document.getElementById(value).classList.add("selected");
        }
    }

    function setIncludedTypes(preset) {
        var includeNotifications = document.getElementById("AllNotifications").classList.contains("selected");
        var includeCaseNotes = document.getElementById("AllCaseNotes").classList.contains("selected");

        if (preset === "AllNotifications") {
            $("button.activity-item-type").filter(function () {
                return $(this).attr('data-category') === "Notification";
            }).each(function () {
                var category = $(this).attr("data-category");

                if ((includeNotifications && category === "Notification"))
                    activateButtonSelection($(this));
                else deactivateButtonSelection($(this));
            });
        } else if (preset === "AllCaseNotes") {
            $("button.activity-item-type").filter(function () {
                return $(this).attr('data-category') === "CaseNote"
            }).each(function () {
                var category = $(this).attr("data-category");

                if ((includeCaseNotes && category === "CaseNote"))
                    activateButtonSelection($(this));
                else deactivateButtonSelection($(this));
            });
        }
    }

    function toggleVisibility(element, visible) {
        element.style.display = visible ? "block" : "none";
    }

    $('button.filter').each(function () {
        $(this).on('click', function () {
            if ($(this).hasClass('selected')) {
                $(this).removeClass('selected');
                $(this).addClass('unselected');
            } else {
                $(this).removeClass('unselected');
                $(this).addClass('selected');
            }

            dynamicallyHideFilteredItems();
        });
    });

    $('button.activity-item-type').each(function () {
        $(this).on('click', determineIfCategoryShouldBeSelected);
    });

    $('button.activity-item-category').each(function () {
        $(this).on('click', function () {
            setIncludedTypes($(this).attr('data-id'));
            dynamicallyHideFilteredItems();
            if ($('button.activity-item-category.selected').length === 2) {
                toggleMasterSelection(true);
            } else toggleMasterSelection(false);
        });
    });

    function determineIfCategoryShouldBeSelected() {
        var totalNotifications = $("button.activity-item-type").filter(function () {
            return $(this).attr("data-category") === "Notification";
        });

        var totalCaseNotes = $("button.activity-item-type").filter(function () {
            return $(this).attr("data-category") === "CaseNote";
        });

        selectCategoryItem(document.getElementById("AllNotifications"),
            totalNotifications.filter(function () {
                return $(this).hasClass("selected");
            }).length ===
            totalNotifications.length);

        selectCategoryItem(document.getElementById("AllCaseNotes"),
            totalCaseNotes.filter(function () {
                return $(this).hasClass("selected");
            }).length ===
            totalCaseNotes.length);

        toggleMasterSelection($('button.activity-item-type.unselected').length === 0);
    }

    $("#AllTypes").on('click', toggleMasterCategory);

    function deactivateButtonSelection(element) {
        if (element.hasClass('selected')) {
            element.removeClass('selected');
            element.addClass('unselected');
        }
    }

    function activateButtonSelection(element) {
        if (element.hasClass('unselected')) {
            element.removeClass('unselected');
            element.addClass('selected');
        }
    }

    function toggleMasterSelection(enabled) {
        const master = document.getElementById("AllTypes");
        selectCategoryItem(master, enabled);
    }

    function selectCategoryItem(element, enabled) {
        if (enabled) {
            if (element.classList.contains("unselected")) {
                element.classList.remove("unselected");
                element.classList.add("selected");
            }
        } else {
            if (element.classList.contains("selected")) {
                element.classList.remove("selected");
                element.classList.add("unselected");
            }
        }
    }

    function toggleMasterCategory() {
        const master = document.getElementById("AllTypes");
        if (master.classList.contains("selected")) {
            $("button.activity-item-category").each(function () {
                activateButtonSelection($(this));
            });
        } else {
            $("button.activity-item-category").each(function () {
                deactivateButtonSelection($(this));
            });
        }

        setIncludedTypes("AllNotifications");
        setIncludedTypes("AllCaseNotes");

        dynamicallyHideFilteredItems();
    }
});