define(["require", "exports", "esri/Graphic"], function (require, exports, Graphic) {
    "use strict";
    var EsriHelper = /** @class */ (function () {
        function EsriHelper(view) {
            this._view = view;
        }

        EsriHelper.prototype.locateAndGoTo = function (placeName, locatorService, esriGeoServiceLocatorUrl) {
            var view = this._view;
            locatorService
                .addressToLocations(esriGeoServiceLocatorUrl, {
                    maxLocations: 1,
                    countryCode: "GBR",
                    outFields: ["Place_addr", "PlaceName"],
                    address: {
                        SingleLine: placeName,
                    },
                })
                .then(function (result) {
                    // Set the coordinates and zoom, only if the place is valid
                    if (result[0] != undefined) {
                        view.goTo({
                            center: [result[0].location.x, result[0].location.y],
                            zoom: 15,
                        });
                    }
                }, function (e) {
                    if (window.console) {
                        console.error("Unable to query geo service for location data", e);
                    }
                });
        };

        EsriHelper.prototype.locateTown = async function (place, locatorService, esriGeoServiceLocatorUrl) {
            return await locatorService.addressToLocations(esriGeoServiceLocatorUrl,
                {
                    maxLocations: 1,
                    countryCode: "GBR",
                    outFields: ["Place_addr", "PlaceName"],
                    address: {
                        SingleLine: place
                    }
                }).then((result) => {
                return result;
            }).catch((e) => {
                console.error(e);
                return null;
            });
        }


        EsriHelper.prototype.locateGoToSetWidget = function (place, locatorService, homeWidget, viewPoint, esriGeoServiceLocatorUrl) {
            var view = this._view;
            locatorService.addressToLocations(esriGeoServiceLocatorUrl, {
                maxLocations: 1,
                countryCode: "GBR",
                outFields: ["Place_addr", "PlaceName"],
                address: {
                    SingleLine: place
                }
            }).then(function (result) {
                // Set the coordinates and zoom, only if the place is valid
                if (result[0] != undefined) {
                    view.goTo({
                        center: [result[0].location.x, result[0].location.y],
                        zoom: 15
                    });
                    if (homeWidget && viewPoint) {
                        viewPoint.targetGeometry = result[0].extent;
                        homeWidget.viewpoint = viewPoint;
                    }
                }
                else {
                    //Default the zoom level to 6, as GBR was set as country, if the place is invalid
                    view.goTo({
                        zoom: 6
                    });
                }
            }, function (e) {
                if (window.console) {
                    console.error("Unable to query geo service for location data", e);
                }
            });
        };

        EsriHelper.prototype.disableZooming = function () {
            this._view.popup.actions = [];
            function stopEvtPropagation(evt) {
                evt.stopPropagation();
            }
            // exlude the zoom widget from the default UI
            this._view.ui.components = ["attribution"];
            // disable mouse wheel scroll zooming on the view
            this._view.on("mouse-wheel", stopEvtPropagation);
            // disable zooming via double-click on the view
            this._view.on("double-click", stopEvtPropagation);
            // disable zooming out via double-click + Control on the view
            this._view.on("double-click", ["Control"], stopEvtPropagation);
            // disables pinch-zoom and panning on the view
            this._view.on("drag", stopEvtPropagation);
            // disable the view's zoom box to prevent the Shift + drag
            // and Shift + Control + drag zoom gestures.
            this._view.on("drag", ["Shift"], stopEvtPropagation);
            this._view.on("drag", ["Shift", "Control"], stopEvtPropagation);
            // prevents zooming with the + and - keys
            this._view.on("key-down", function (evt) {
                var prohibitedKeys = [
                    "+",
                    "-",
                    "Shift",
                    "_",
                    "=",
                    "ArrowUp",
                    "ArrowDown",
                    "ArrowRight",
                    "ArrowLeft",
                ];
                var keyPressed = evt.key;
                if (prohibitedKeys.indexOf(keyPressed) !== -1) {
                    evt.stopPropagation();
                }
            });
        };

        EsriHelper.buildGraphic = function (geometry, symbol) {
            return new Graphic({
                geometry: geometry,
                symbol: symbol,
            });
        };

        EsriHelper.getGraphicsFromEvt = function (evt) {
            if (!evt || (!evt.graphic && !evt.graphics)) {
                return [];
            }
            return evt.graphics ? evt.graphics.filter((item) => {
                return item.symbol.type !== "text";
            }) : (evt.graphic.symbol.type !== "text") ? [evt.graphic] : []
        };


        EsriHelper.getNonFilteredGraphicsFromEvt = function (evt) {
            if (!evt) {
                return [];
            }
            return evt.graphics ? evt.graphics : [evt.graphic];
        };

        EsriHelper.createTextSymbolWithoutText = function (color, haloColor, haloSize, fontSize, fontWeight) {
            if (haloColor === void 0) { haloColor = "white"; }
            if (haloSize === void 0) { haloSize = 10; }
            if (fontSize === void 0) { fontSize = 12; }
            if (fontWeight === void 0) { fontWeight = "normal"; }
            return {
                type: "text",
                color: color,
                haloColor: haloColor,
                haloSize: haloSize + "px",
                font: {
                    size: fontSize,
                    weight: fontWeight,
                },
            };
        };
        return EsriHelper;
    }());
    return EsriHelper;
});
