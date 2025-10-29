define([
    "require",
    "exports",
    "esri/config",
    "esri/Map",
    "esri/views/MapView",
    "esri/Basemap",
    "esri/layers/WMSLayer",
    "esri/layers/WMTSLayer",
    "esri/layers/GraphicsLayer",
    "esri/Graphic",
    "esri/rest/locator",
    "esri/layers/FeatureLayer",
    "esri/core/reactiveUtils",
    "/js/mapping/maps-html-helper.js?v=" + Date.now(),
    "/js/mapping/maps-common.js?v=" + Date.now(),
    "/js/mapping/mapSettings.js?v=" + Date.now()
], function (
    require,
    exports,
    config,
    Map,
    MapView,
    Basemap,
    WMSLayer,
    WMTSLayer,
    GraphicsLayer,
    Graphic,
    locator,
    FeatureLayer,
    reactiveUtils,
    maps_html_Helper,
    Maps_common,
    mapSettings) {
    var profileMap = /** @class */ (function () {
        function profileMap(location) {
            this.location = location;
            config.apiKey = mapSettings.esriApiKey;

            //This is the default map for the FC system
            var baseMap = new Basemap({
                portalItem: {
                    id: mapSettings.baseMapForUK,
                },
            });

            this.map = new Map({
                basemap: baseMap,
            });

            const mapComponent = document.getElementById(location);
            mapComponent.map = this.map;

            this.view = mapComponent.view;

            this.view.when(() => {
                this.view.extent = mapSettings.englandExtent;
                this.view.constraints = { maxZoom: 20 };
                return this.mapLoadEvt();
            })
                .then(this.loadCompartments.bind(this))
                .then(this.GetStartingPoint.bind(this))
                .then(this.addWidgets.bind(this))
                .then(this.addWatermark.bind(this));
        }


        profileMap.prototype.addWatermark = function () {
            // Create a new GraphicsLayer for the watermark
            const watermarkLayer = new GraphicsLayer({
                visible: false,
                id: "watermarkLayer",
            });

            // Define the text symbol for the watermark
            const textSymbol = mapSettings.BlueSkyTextSymbol;

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

            reactiveUtils.watch(
                () => this.view.stationary,
                (newValue) => {
                    if (newValue) updateWatermarkPositions();
                }
            );
            reactiveUtils.watch(
                () => this.view.extent,
                updateWatermarkPositions
            );
            reactiveUtils.watch(
                () => this.view.map.basemap,
                (newBasemap) => {
                    watermarkLayer.visible = !newBasemap.portalItem || newBasemap.portalItem.id !== mapSettings.baseMapForUK;
                }
            );
            return Promise.resolve();
        };

        profileMap.prototype.addWidgets = async function () {

            this.map.basemap.title = "OS Map";
            const mapComponent = document.getElementById(this.location)
            await mapComponent.viewOnReady();

            const [LocalBasemapsSource] = await $arcgis.import([
                "@arcgis/core/widgets/BasemapGallery/support/LocalBasemapsSource.js",
            ]);

            const basemapGallery = document.querySelector("arcgis-basemap-gallery");

            // Create the WMS basemap
            var wmsLayer = new WMSLayer(mapSettings.wmsLayer);

            const wmsBasemap = new Basemap({
                baseLayers: [wmsLayer],
                title: mapSettings.wmsLayerName,
                id: "wmsBasemap"
            });

            basemapGallery.source = new LocalBasemapsSource({
                basemaps: [
                    this.map.basemap,
                    wmsBasemap                   
                ]
            });

            return Promise.resolve();
        };

        profileMap.prototype.loadCompartments = function () {
            var items = maps_html_Helper.getCompartments();
            if (typeof items === "undefined" || items.length === 0) {
                return Promise.resolve([]);
            }
            var graphicsArray = [];
            items.forEach(function (item) {
                var geometry;
                if (item.GIS.rings) {
                    geometry = {
                        type: "polygon",
                        rings: item.GIS.rings,
                        spatialReference: item.GIS.spatialReference.wkid,
                    };
                }
                if (geometry) {
                    try {
                        graphicsArray.push(new Graphic({
                            geometry: geometry,
                            attributes: {
                                compartmentName: item.label,
                            },
                        }));
                    }
                    catch (e) {
                        if (window.console) {
                            console.error("Failed to create all required parts for view.", e);
                        }
                    }
                    return;
                }
            });
            this.loadPolygonLayer(graphicsArray.filter(function (item) {
                return item.geometry.type === "polygon";
            }));
            return Promise.resolve(graphicsArray);
        };

        profileMap.prototype.loadPolygonLayer = function (graphicsArray) {
            try {
                if (typeof graphicsArray === "undefined" || graphicsArray.length === 0) {
                    return Promise.resolve();
                }
                var labelSymbol = JSON.parse(JSON.stringify(mapSettings.activeTextSymbol));
                this.compartmentLayer_polygon = new FeatureLayer({
                    labelsVisible: true,
                    source: graphicsArray,
                    geometryType: "polygon",
                    objectIdField: "ObjectID",
                    fields: [
                        { name: "ObjectID", type: "oid" },
                        { name: "compartmentName", type: "string" },
                    ],
                    labelingInfo: {
                        labelExpressionInfo: {
                            expression: "$feature.compartmentName",
                        },
                        symbol: labelSymbol,
                        visualVariables: mapSettings.visualVariables.textSize
                    },
                    renderer: {
                        type: "simple",
                        symbol: mapSettings.otherPolygonSymbol,
                        visualVariables: mapSettings.visualVariables.size
                    },
                });
                this.map.add(this.compartmentLayer_polygon);
            }
            catch (e) {
                if (window.console) {
                    console.error("Failed to create all required parts for view.", e);
                    Promise.reject(e);
                }
            }
            return Promise.resolve();
        };

        profileMap.prototype.mapLoadEvt = function () {
            this.commonTools = new Maps_common(this.view);
            this.commonTools.disableZooming();
            return Promise.resolve();
        };

        profileMap.prototype.GetStartingPoint = function (graphicsArray) {
            if (typeof graphicsArray === "undefined" || graphicsArray.length === 0) {
                var town = maps_html_Helper.getNearestTown();
                if (!town) {
                    return;
                }
                if (!this.commonTools) {
                    return;
                }
                this.commonTools.locateAndGoTo(town, locator, mapSettings.esriGeoServiceLocatorUrl);
            }
            else if (graphicsArray.length === 1) {
                const topItem = graphicsArray[0];
                if (topItem.geometry.type === 'point') {
                    this.view.goTo({
                        target: topItem.geometry,
                        zoom: 20
                    });
                } else {
                    this.view.goTo({
                        target: topItem.geometry.extent
                    });
                }
            }
            else {
                this.view.goTo({
                    target: graphicsArray,
                });
            }
        };

        return profileMap;
    }());
    return profileMap;
});
