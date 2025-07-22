define(["require", "exports", "tslib", "esri/core/accessorSupport/decorators", "esri/widgets/Widget", "esri/widgets/support/widget", "/js/mapping/gthelper/proj4.js?v=" + Date.now()], function (require, exports, tslib_1, decorators_1, Widget_1, widget_1, proj4) {
    "use strict";
    Widget_1 = tslib_1.__importDefault(Widget_1);
    var DownloadWidget = /** @class */ (function (_super) {
        tslib_1.__extends(DownloadWidget, _super);
        function DownloadWidget(params) {
            var _this = _super.call(this, params) || this;
            _this.handleDownload = function () {
                var jsonString = JSON.stringify(_this.getJsonData());
                var blob = new Blob([jsonString], { type: "application/json" });
                var a = document.createElement("a");
                a.href = URL.createObjectURL(blob);
                a.download = "mapDownload.geojson";
                a.click();
                URL.revokeObjectURL(a.href);
            };
            proj4.defs("EPSG:27700", "+proj=tmerc +lat_0=49 +lon_0=-2 +k=0.999601 +x_0=400000 +y_0=-100000 +ellps=airy +towgs84=446.448,-125.157,542.06,0.15,0.247,0.842,-20.489 +units=m +no_defs");
            proj4.defs("EPSG:4326", "+proj=longlat +datum=WGS84 +no_defs");
            return _this;
        }
        DownloadWidget.prototype.getJsonData = function () {
            var _this = this;
            var jsonData = {
                type: "FeatureCollection",
                features: []
            };
            // Loop through the layers
            this.view.map.layers.forEach(function (layer) {
                layer.source.items.forEach(function (graphic) {
                    jsonData.features.push({
                        type: "Feature",
                        geometry: {
                            type: _this.getShapeType(graphic.geometry.type),
                            coordinates: _this.getCoordinates(graphic)
                        },
                        properties: {
                            name: graphic.attributes.compartmentName,
                        }
                    });
                });
            });
            return jsonData;
        };
        DownloadWidget.prototype.getShapeType = function (value) {
            var result = "Polygon";
            if (value === "point") {
                result = "Point";
            }
            else if (value === "polyline") {
                result = "LineString";
            }
            return result;
        };
        DownloadWidget.prototype.getCoordinates = function (graphic) {
            var result;
            if (graphic.geometry.type === "polygon") {
                result = graphic.geometry.rings.map(function (ring) {
                    return ring.map(function (point) { return proj4('EPSG:27700', 'EPSG:4326', point); });
                });
            }
            else if (graphic.geometry.type === "polyline") {
                if (graphic.geometry.paths.length < 1) {
                    return [[]];
                }
                //Dropping to the first line as the rings struggle in GEOJSON
                result = graphic.geometry.paths[0].map(function (ring) { return proj4('EPSG:27700', 'EPSG:4326', ring); });
            }
            else {
                result = proj4("EPSG:27700", "EPSG:4326", [graphic.geometry.x, graphic.geometry.y]);
            }
            return result;
        };
        DownloadWidget.prototype.render = function () {
            return ((0, widget_1.tsx)("calcite-action-pad", { "expand-disabled": true, expanded: true },
                (0, widget_1.tsx)("calcite-action", { text: "Download", icon: "save", scale: "m", onclick: this.handleDownload })));
        };
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], DownloadWidget.prototype, "view", void 0);
        DownloadWidget = tslib_1.__decorate([
            (0, decorators_1.subclass)("esri.widgets.DownloadWidget")
        ], DownloadWidget);
        return DownloadWidget;
    }(Widget_1.default));
    return DownloadWidget;
});
