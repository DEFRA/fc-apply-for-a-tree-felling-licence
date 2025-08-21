window.profileMaps = {};

$(function () {
	$(".profileMap").each(function () {
        const divId = $(this).attr("id");

        const filter = document.getElementById(divId + "-filters");

        require(["../../js/mapping/maps/map-felling-restocking.js?v=" + Date.now()], function (ConfirmedFellingMap) {
            const mapInstance = new ConfirmedFellingMap(divId, true, filter.value);

			window.profileMaps[divId] = mapInstance;
		});
    });

    $(".view-on-map").click(function (e) {
        const accordionDiv = $(this).closest(".govuk-accordion__section");
        if (accordionDiv.length) {
            accordionDiv.attr("aria-expanded", "true");
            accordionDiv.addClass("govuk-accordion__section--expanded");
        }
    });
});

function viewOnMap(divId, compartmentName) {
    require(["../../js/mapping/maps/map-felling-restocking.js?v=" + Date.now()], function () {
        const mapInstance = window.profileMaps[divId];

        if (!mapInstance) {
            console.error(`Map instance for ${divId} not found.`);
            return;
        }

        mapInstance.focusOnCompartment(compartmentName);
    });
}