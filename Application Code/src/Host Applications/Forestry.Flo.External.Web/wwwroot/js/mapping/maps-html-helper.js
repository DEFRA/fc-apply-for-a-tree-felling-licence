define(["require", "exports"], function (require, exports) {
    "use strict";
    var HTMLHelper = /** @class */ (function () {
        function HTMLHelper() {
        }
        HTMLHelper.stringCheck = function (stringToCheck) {
            return !(typeof stringToCheck === "string" && stringToCheck.trim().length === 0);
        };
        HTMLHelper.getNearestTown = function () {
            var item = document.getElementById("NearestTown");
            if (!item) {
                return "";
            }
            return item === null || item === void 0 ? "" : item.value;
        };
        HTMLHelper.getCompartmentOfInterest_THectares = function () {
            var item = document.getElementById("CompartmentModelOfInterest_TotalHectares");
            if (!item) {
                return null;
            }
            return item === null || item;
        };
        HTMLHelper.getCompartmentOfInterest_Element = function () {
            var item = document.getElementById("CompartmentModelOfInterest_GISData");
            if (!item) {
                return null;
            }
            return item === null || item;
        };
        HTMLHelper.getCompartments = function () {
            var resx = [];
            var _this = this;
            var items = document.querySelectorAll('[data-group="compartments_GIS"]');
            if (typeof items !== "undefined" && items.length > 0) {
                items.forEach(function (item) {
                    var checkValue;
                    try {
                        //It is possible the users have created Compartments with no GIS
                        if (_this.stringCheck(item.value)) {
                            checkValue = JSON.parse(item.value);
                        }
                    }
                    catch (error) {
                        console.error(error);
                    }
                    if (checkValue) {
                        resx.push({
                            label: item.getAttribute("data-label"),
                            GIS: checkValue,
                            Id: item.getAttribute("id"),
                        });
                    }
                });
            }
            return resx;
        };
        HTMLHelper.getCurrentCompartmentName = function () {
            var elm = document.getElementById("CompartmentModelOfInterest_DisplayName");
            if (!elm) {
                return "";
            }
            return elm.value;
        };
        HTMLHelper.getCompartmentId = function () {
            var elm = document.getElementById("CompartmentId");
            if (!elm) {
                return "";
            }
            return elm.value;
        };
        HTMLHelper.getCurrentGeometryJson = function () {
            var resx = null;
            var elm = document.getElementById("CompartmentModelOfInterest_GISData");
            if (elm && this.stringCheck(elm.value))
                try {
                    resx = JSON.parse(elm.value);
                }
                catch (error) {
                    console.error(error);
                }
            return resx;
        };
        HTMLHelper.getOtherComparmentJSON = function () {
            var resx = null;
            var elm = document.getElementById("AllOtherPropertyCompartmentJson");
            if (elm && this.stringCheck(elm.value))
                try {
                    resx = JSON.parse(elm.value);
                    resx = resx.map(function (item) {
                        try {
                            return {
                                DisplayName: item.DisplayName,
                                GISData: JSON.parse(item.GISData),
                            };
                        }
                        catch (error) {
                            console.error(error);
                        }
                        return false;
                    });
                }
                catch (error) {
                    console.error(error);
                }
            return resx;
        };
        HTMLHelper.getGISDataJSON = function (fieldName, contvertGISDataField) {
            if (contvertGISDataField === void 0) { contvertGISDataField = false; }
            var resx = null;
            var elm = document.getElementById(fieldName);
            if (elm && this.stringCheck(elm.value)) {
                try {
                    resx = JSON.parse(elm.value);
                    if (contvertGISDataField) {
                        resx = resx.map(function (item) {
                            try {
                                return {
                                    Id: item.Id,
                                    DisplayName: item.DisplayName,
                                    GISData: ((!item.GISData) ? null : JSON.parse(item.GISData)),
                                    Selected: item.Selected
                                };
                            }
                            catch (error) {
                                console.error(error);
                            }
                        });
                    }
                }
                catch (error) {
                    console.error(error);
                }
            }
            return resx;
        };
        HTMLHelper.checkCheckbox = function (selector, check) {
            var elm = document.querySelector(selector);
            try {
                if (elm) {
                    elm.checked = check;
                }
            }
            catch (error) {
                console.error(error);
            }
            return;
        };

        HTMLHelper.getElements = function(selector){
            return document.querySelectorAll(selector);
        }

        HTMLHelper.getElementById = function (name) {
            return document.getElementById(name);
        }
        return HTMLHelper;
    }());
    return HTMLHelper;
});
