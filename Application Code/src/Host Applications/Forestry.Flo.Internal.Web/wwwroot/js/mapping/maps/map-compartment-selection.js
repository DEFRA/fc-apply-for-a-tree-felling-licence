///<reference path="../fcconfig.js" />

define(["require",
    "exports",
    "esri/Map",
    "esri/views/MapView",
    "esri/config",
    "esri/Basemap",
    "/js/mapping/fcconfig.js?v=" + Date.now(),
    "/js/mapping/maps-html-helper.js?v=" + Date.now(),
    "esri/layers/GraphicsLayer",
    "esri/Graphic",
    "/js/mapping/maps-common.js?v=" + Date.now(),
    "/js/mapping/gthelper/proj4.js?v=" + Date.now(),
    "/js/mapping/tokml.js?v=" + Date.now(),
    "/js/mapping/shp-write.js?v=" + Date.now()

],
    function (require, exports, Map, MapView, config, BaseMap, fcconfig, HTMLHelper, GraphicLayer, Graphic, Maps_common, proj4, tokml, shpwrite) {
        var MapCompartmentSelection = /** @class */ (function () {
            MapCompartmentSelection.prototype.map;
            MapCompartmentSelection.prototype.view;
            MapCompartmentSelection.prototype.drawinglayer;
            MapCompartmentSelection.prototype.graphicsArray;
            MapCompartmentSelection.prototype.commonTools;

            proj4.defs("EPSG:27700", "+proj=tmerc +lat_0=49 +lon_0=-2 +k=0.999601 +x_0=400000 +y_0=-100000 +ellps=airy +towgs84=446.448,-125.157,542.06,0.15,0.247,0.842,-20.489 +units=m +no_defs");
            proj4.defs("EPSG:4326", "+proj=longlat +datum=WGS84 +no_defs");

            function MapCompartmentSelection(location) {

                config.apiKey = fcconfig.esriApiKey;

                this.graphicsArray = [];

                this.map = new Map({
                    basemap: new BaseMap({ portalItem: { id: fcconfig.baseMapForUK } })
                });

                //Lets Work on the guide
                this.view = new MapView({
                    map: this.map,
                    container: location,
                    extent: fcconfig.englandExtent,
                    constraints: {
                        maxZoom: 20
                    }
                });

                this.commonTools = new Maps_common(this.view);

                this.view.when(this.loadGIS.bind(this)).then(this.GetStartingPoint.bind(this)).then(this.wireUpEvents.bind(this));
            }

            MapCompartmentSelection.prototype.wireUpEvents = function () {
                var _this = this;

                this.view.on("click", (evt) => {
                    _this.view.hitTest(evt).then((result) => {
                        console.log(result);

                        if (typeof result.results === "undefined" || result.results.length === 0) {
                            return;
                        }

                        let graphic = result.results[0].graphic;
                        _this.UpdateGraphic(graphic, !graphic.attributes.selected)

                        HTMLHelper.checkCheckbox('input[name="SelectedCompartmentIds"][value="' + graphic.attributes.Id + '"]', graphic.attributes.selected);
                    });
                });

                const checks = HTMLHelper.getElements('input[name="SelectedCompartmentIds"]');
                if (checks) {
                    checks.forEach((item) => {
                        item.addEventListener('click', function handleClick(event) {
                            _this.drawinglayer.graphics.every((graphic) => {
                                if (graphic.attributes.Id !== this.value) {
                                    return true;
                                }
                                _this.UpdateGraphic(graphic, this.checked)
                                return false;
                            });

                        })
                    });
                }

                const downloadJson = HTMLHelper.getElementById('Download-JSON');
                if (downloadJson) {
                    downloadJson.addEventListener('click', (evt) => {
                        const jsonString = JSON.stringify(_this.getJsonData());
                        const blob = new Blob([jsonString], { type: "application/json" });
                        const a = document.createElement("a");
                        a.href = URL.createObjectURL(blob);
                        a.download = "mapDownload.geojson";
                        a.click();
                        URL.revokeObjectURL(a.href);
                    });
                }

                const downloadKml = HTMLHelper.getElementById('Download-KML');
                if (downloadKml) {
                    downloadKml.addEventListener('click', (evt) => {
                        const jsonData = _this.getJsonData();
                        const kmlString = tokml(jsonData);
                        const blob = new Blob([kmlString], { type: "application/vnd.google-earth.kml+xml" });
                        const a = document.createElement("a");
                        a.href = URL.createObjectURL(blob);
                        a.download = "mapDownload.kml";
                        a.click();
                        URL.revokeObjectURL(a.href);
                    });
                }

                const downloadShapefile = HTMLHelper.getElementById('Download-SHP');
                if (downloadShapefile) {
                    downloadShapefile.addEventListener('click', (evt) => {
                        const jsonData = _this.getJsonData();
                        const options = {
                            folder: 'shapefile',
                            types: {
                                point: 'points',
                                polygon: 'polygons',
                                polyline: 'lines'
                            }
                        };
                        const zipData = shpwrite.zip(jsonData, options);
                        const blob = new Blob([zipData], { type: "application/zip" });
                        const a = document.createElement("a");
                        a.href = URL.createObjectURL(blob);
                        a.download = "shapefile.zip";
                        a.click();
                        URL.revokeObjectURL(a.href);
                    });
                }
            }

            MapCompartmentSelection.prototype.UpdateGraphic = function (graphic, value) {
                graphic.attributes.selected = value;
                switch (graphic.geometry.type) {
                    case "polyline":
                        graphic.symbol = (graphic.attributes.selected) ? fcconfig.selectedLineSymbol : fcconfig.activeLineSymbol;
                        break;
                    case "point":
                        graphic.symbol = (graphic.attributes.selected) ? fcconfig.selectedPointSymbol : fcconfig.activePointSymbol;
                        break;
                    default:
                        graphic.symbol = (graphic.attributes.selected) ? fcconfig.selectedPolygonSymbol : fcconfig.activePolygonSymbol;
                        break;
                }
            }

            MapCompartmentSelection.prototype.loadGIS = function () {
                try {
                    var gis = HTMLHelper.getGISDataJSON("GisData", true);

                    if (typeof gis === "undefined" || gis.length === 0) {
                        return;
                    }

                    this.drawinglayer = new GraphicLayer();
                    this.map.add(this.drawinglayer)
                    gis.forEach((item) => {
                        if (!item.GISData) {
                            return;
                        }
                        let geometry;
                        let workingSymbol;

                        if (item.GISData.rings) {
                            geometry = {
                                type: "polygon",
                                rings: item.GISData.rings,
                                spatialReference: item.GISData.spatialReference.wkid,
                            };
                            workingSymbol = (item.Selected) ? fcconfig.selectedPolygonSymbol : fcconfig.activePolygonSymbol;
                        } else if (item.GISData.paths) {
                            geometry = {
                                type: "polyline",
                                paths: item.GISData.paths,
                                spatialReference: item.GISData.spatialReference.wkid,
                            }
                            workingSymbol = (item.Selected) ? fcconfig.selectedLineSymbol : fcconfig.activeLineSymbol;
                        } else {
                            geometry = {
                                type: "point",
                                longitude: item.GISData.x,
                                latitude: item.GISData.y,
                                spatialReference: item.GISData.spatialReference.wkid,
                            };
                            workingSymbol = (item.Selected) ? fcconfig.selectedPointSymbol : fcconfig.activePointSymbol;
                        }
                        if (geometry) {
                            try {
                                let graphic = new Graphic({
                                    geometry: geometry,
                                    symbol: workingSymbol,
                                    attributes: {
                                        Id: item.Id,
                                        DisplayName: item.DisplayName,
                                        selected: item.Selected
                                    }
                                });
                                this.graphicsArray.push(graphic);
                                this.drawinglayer.add(graphic);
                                this.drawinglayer.add(this.getLabelGrapic(graphic));

                            } catch (e) {
                                if (window.console) {
                                    console.error("Failed to create all required parts for view.", e);
                                }
                            }
                            return;
                        }


                    });
                }
                catch (error) {
                    console.error(error);
                }
            };

            MapCompartmentSelection.prototype.getLabelGrapic = function (shapeGraphic) {
                var resx;
                var labelSymbol = JSON.parse(JSON.stringify(fcconfig.activeTextSymbol));
                labelSymbol.text = shapeGraphic.attributes.DisplayName;

                if (shapeGraphic.geometry.type === "point") {
                    labelSymbol.xoffset = fcconfig.pointOffset.xoffset;
                    labelSymbol.yoffset = fcconfig.pointOffset.yoffset;
                    resx = new Graphic({
                        geometry: shapeGraphic.geometry,
                        symbol: labelSymbol,
                    });
                }
                else if (shapeGraphic.geometry.type === "polyline") {
                    labelSymbol.xoffset = fcconfig.pointOffset.xoffset;
                    labelSymbol.yoffset = fcconfig.pointOffset.yoffset;
                    resx = new Graphic({
                        geometry: shapeGraphic.geometry.extent.center,
                        symbol: labelSymbol,
                    });
                }
                else {
                    resx = new Graphic({
                        geometry: shapeGraphic.geometry.centroid,
                        symbol: labelSymbol,
                    });
                }
                if (!resx.attributes) {
                    resx.attributes = {
                        shapeID: shapeGraphic.uid,
                    };
                }
                return resx;
            };

            MapCompartmentSelection.prototype.GetStartingPoint = function (graphicsArray) {
                if (typeof this.graphicsArray === "undefined" || this.graphicsArray.length === 0) {
                    return;
                }

                this.view.goTo({
                    target: this.graphicsArray,
                });
            };

            MapCompartmentSelection.prototype.getShapeType = function (value) {
                let result = "Polygon";
                if (value === "point") {
                    result = "Point";
                } else if (value === "polyline") {
                    result = "LineString";
                }
                return result;
            }

            MapCompartmentSelection.prototype.getCoordinates = function (graphic) {
                let result;
                if (graphic.geometry.type === "polygon") {
                    result = graphic.geometry.rings.map((ring) =>
                        ring.map((point) => proj4('EPSG:27700', 'EPSG:4326', point))
                    );
                } else if (graphic.geometry.type === "polyline") {
                    if (graphic.geometry.paths.length < 1) {
                        return [[]];
                    }
                    //Dropping to the first line as the rings struggle in GEOJSON
                    result = graphic.geometry.paths[0].map((ring) => proj4('EPSG:27700', 'EPSG:4326', ring));
                } else {
                    result = proj4("EPSG:27700", "EPSG:4326", [graphic.geometry.x, graphic.geometry.y]);
                }
                return result;
            }

            MapCompartmentSelection.prototype.getJsonData = function() {
                const jsonData = {
                    type: "FeatureCollection",
                    features: []
                };
                // Loop through the layers
                this.view.map.layers.forEach(layer => {
                    layer.graphics.items.forEach(graphic => {
                        jsonData.features.push({
                            type: "Feature",
                            geometry: {
                                type: this.getShapeType(graphic.geometry.type),
                                coordinates: this.getCoordinates(graphic)
                            },
                            properties: {
                                name: graphic.attributes.compartmentName,
                            }
                        });
                    });
                });
                return jsonData;
            }

            return MapCompartmentSelection;
        }());
        return MapCompartmentSelection;
    });
