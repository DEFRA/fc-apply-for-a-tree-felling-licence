///<reference path="../fcconfig.js" />

define(["require",
    "exports",
    "esri/Map",
    "esri/views/MapView",
    "esri/config",
    "esri/Basemap",
    "esri/layers/WMSLayer",
    "esri/layers/WMTSLayer",
    "esri/layers/GraphicsLayer",
    "esri/widgets/BasemapGallery",
    "/js/mapping/fcconfig.js?v=" + Date.now(),
    "/js/mapping/maps-html-helper.js?v=" + Date.now(),
    "esri/layers/GraphicsLayer",
    "esri/Graphic",
    "/js/mapping/maps-common.js?v=" + Date.now(),
    "/js/mapping/gthelper/proj4.js?v=" + Date.now(),
    "/js/mapping/tokml.js?v=" + Date.now(),
    "/js/mapping/shp-write.js?v=" + Date.now(),
    "esri/widgets/Expand"

],
    function (require, exports, Map, MapView, config, BaseMap,
        WMSLayer,
        WMTSLayer,
        GraphicsLayer,
        BasemapToggle,
        fcconfig, HTMLHelper, GraphicLayer, Graphic, Maps_common, proj4, tokml, shpwrite, Expand) {
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

                this.view.when(this.loadGIS.bind(this))
                    .then(this.GetStartingPoint.bind(this))
                    .then(this.wireUpEvents.bind(this))
                    .then(this.SetUpWidgets.bind(this))
                    .then(this.addWatermark.bind(this));
            }

            MapCompartmentSelection.prototype.SetUpWidgets = function () {
                var that = this;


                that.map.basemap.title = "OS Map";

                var wmsLayer = new WMSLayer(fcconfig.wmsLayer);

                var wmsBasemap = new Basemap({
                    baseLayers: [wmsLayer],
                    title: fcconfig.wmsLayerName,
                    id: "wmsBasemap"
                });


                const basemapToggleWidget = new BasemapToggle({
                    view: this.view,
                    source: [that.map.basemap, wmsBasemap]
                });

                const expandLayer = new Expand({
                    expandIconClass: "esri-icon-basemap",
                    view: this.view,
                    content: basemapToggleWidget
                });

                this.view.ui.add(expandLayer, { position: "top-left" });


                return Promise.resolve();
            }

            MapCompartmentSelection.prototype.addWatermark = function () {
                // Create a new GraphicsLayer for the watermark
                const watermarkLayer = new GraphicsLayer({
                    visible: false,
                    id: "watermarkLayer",
                });

                // Define the text symbol for the watermark
                const textSymbol = fcconfig.BlueSkyTextSymbol;

                //Function to update the watermark positions
                const updateWatermarkPositions = () => {
                    const extent = this.view.extent;
                    const width = extent.width;
                    const height = extent.height;
                    const spacing = Math.min(width, height) / 4;
                    const spatialReference = this.view.spatialReference;

                    // Clear existing graphics
                    watermarkLayer.removeAll();

                    // Create watermark graphics at regular intervals
                    for (let x = extent.xmin; x < extent.xmax; x += spacing) {
                        for (let y = extent.ymin; y < extent.ymax; y += spacing) {
                            const point = {
                                type: "point",
                                x: x,
                                y: y,
                                spatialReference: spatialReference
                            };

                            const watermarkGraphic = new Graphic({
                                geometry: point,
                                symbol: textSymbol
                            });

                            watermarkLayer.add(watermarkGraphic);
                        }
                    }
                };

                // Add the watermark layer to the map
                this.map.add(watermarkLayer);

                // Update the watermark positions initially and whenever the view changes
                this.view.watch("stationary", (newValue) => {
                    if (newValue) {
                        updateWatermarkPositions();
                    }
                });
                this.view.watch("extent", updateWatermarkPositions);

                // Watch for basemap changes and toggle watermark visibility
                this.view.watch("map.basemap", (newBasemap) => {
                    if (!newBasemap.portalItem || newBasemap.portalItem.id !== fcconfig.baseMapForUK) {
                        watermarkLayer.visible = true;
                    } else {
                        watermarkLayer.visible = false;
                    }
                });
            };

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
                                polygon: 'polygons',
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
                graphic.symbol = (graphic.attributes.selected) ? fcconfig.selectedPolygonSymbol : fcconfig.activePolygonSymbol;
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
                var labelSymbol = JSON.parse(JSON.stringify(fcconfig.activeTextSymbol));
                labelSymbol.text = shapeGraphic.attributes.DisplayName;

                const resx = new Graphic({
                    geometry: shapeGraphic.geometry.centroid,
                    symbol: labelSymbol,
                });

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
                return "Polygon";
            }

            MapCompartmentSelection.prototype.getCoordinates = function (graphic) {
                let result;
                if (graphic.geometry.type === "polygon") {
                    result = graphic.geometry.rings.map((ring) =>
                        ring.map((point) => proj4('EPSG:27700', 'EPSG:4326', point))
                    );
                } 
                return result;
            }

            MapCompartmentSelection.prototype.getJsonData = function () {
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
