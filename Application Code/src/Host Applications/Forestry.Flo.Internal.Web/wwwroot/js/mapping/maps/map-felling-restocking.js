define(["require",
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
    "esri/geometry/Polygon",
    "esri/geometry/Point",
    "esri/geometry/geometryEngine",
    "/js/mapping/maps-html-helper.js?v=" + Date.now(),
    "/js/mapping/maps-common.js?v=" + Date.now(),
    "/js/mapping/gthelper/gt-wgs84.js?v=" + Date.now(),
    "/js/mapping/gthelper/gt-math.js?v=" + Date.now(),
    "/js/mapping/gthelper/proj4.js?v=" + Date.now(),
    "/js/mapping/fcconfig.js?v=" + Date.now(),
    "esri/core/reactiveUtils"],
    function (require,
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
        Polygon,
        Point,
        geometryEngine,
        maps_html_Helper,
        Maps_common,
        GT_WGS84,
        GT_Math,
        Proj4js,
        fcconfig, reactiveUtils) {
        var confirmedFellingMap = /** @class */ (function () {
            function confirmedFellingMap(location, canMove, filter) {
                this.location = location
                this.canMove = false;
                if (typeof canMove !== "undefined") {
                    this.canMove = canMove;
                }
                Proj4js.defs("EPSG:27700", "+proj=tmerc +lat_0=49 +lon_0=-2 +k=0.999601 +x_0=400000 +y_0=-100000 +ellps=airy +towgs84=446.448,-125.157,542.06,0.15,0.247,0.842,-20.489 +units=m +no_defs");
                Proj4js.defs("EPSG:4326", "+proj=longlat +datum=WGS84 +no_defs");
                config.apiKey = fcconfig.esriApiKey;
                //This is the default map for the FC system
                var baseMap = new Basemap({
                    portalItem: {
                        id: fcconfig.baseMapForUK
                    }
                });
                this.map = new Map({
                    basemap: baseMap
                });

 
                const mapComponent = document.getElementById(location);
                mapComponent.map = this.map;
                const splitIds = filter.split(",");
                console.log(location);

                mapComponent.viewOnReady().then(() => {
                    this.view = mapComponent.view;
                    if (!this.view) {
                        console.error("ArcGIS view is undefined. Map component may not be initialized or visible.");
                        return;
                    }
                    this.view.extent = fcconfig.englandExtent;
                    this.view.constraints = { maxZoom: 20 };
                    this.commonTools = new Maps_common(this.view);
                    return Promise.resolve();
                })
                    .then(this.mapLoadEvt.bind(this))
                    .then(this.SetUpWidgets.bind(this))
                    .then(() => this.loadCompartments(splitIds))
                    .then(this.GetStartingPoint.bind(this))
                    .then(this.addWatermark.bind(this));
            }

            confirmedFellingMap.prototype.SetUpWidgets = async function () {
                var that = this;


                that.map.basemap.title = "OS Map";

                var wmsLayer = new WMSLayer(fcconfig.wmsLayer);

                var wmsBasemap = new Basemap({
                    baseLayers: [wmsLayer],
                    title: fcconfig.wmsLayerName,
                    id: "wmsBasemap"
                });

                const mapComponent = document.getElementById(this.location);
                await mapComponent.viewOnReady();

                const [LocalBasemapsSource, centroidOperator] = await $arcgis.import([
                    "@arcgis/core/widgets/BasemapGallery/support/LocalBasemapsSource.js",
                    "@arcgis/core/geometry/operators/centroidOperator.js",
                ]);

                this.centroidOperator = centroidOperator;

                const basemapGallery = document.getElementById(this.location).querySelector("arcgis-basemap-gallery");
                basemapGallery.source = new LocalBasemapsSource({
                    basemaps: [
                        this.map.basemap,
                        wmsBasemap
                    ]
                });

                return Promise.resolve();
            }

            confirmedFellingMap.prototype.loadCompartments = async function (includeIds) {

                var items = maps_html_Helper.getCompartments();

                if (includeIds !== undefined && includeIds !== null && includeIds.length > 0) {
                    items = items.filter(item => includeIds.includes(item.Id));
                }

                if (typeof items === "undefined" || items.length === 0) {
                    return Promise.resolve([]);
                }
                var graphicsArray = [];
                var that = this;
                items.map(async (item) => {
                    let centroid;
                    var geometry;
                    if (item.GIS.rings) {
                        centroid = item
                        geometry = {
                            type: "polygon",
                            rings: item.GIS.rings,
                            spatialReference: item.GIS.spatialReference.wkid,
                        };
                    }
                  
                    if (geometry) {
                        try {
                            let graphic = new Graphic({
                                geometry: geometry,
                                attributes: {
                                    compartmentName: item.label,
                                    compartmentRef: ""
                                },
                            });
                            

                            graphic.attributes.compartmentRef = that.convertToOSGridReference(that.centroidOperator.execute(graphic.geometry));

                            graphicsArray.push(graphic);
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

            confirmedFellingMap.prototype.loadPolygonLayer = function (graphicsArray) {
                try {
                    if (typeof graphicsArray === "undefined" || graphicsArray.length === 0) {
                        return Promise.resolve();
                    }
                    var labelSymbol = JSON.parse(JSON.stringify(fcconfig.activeTextSymbol));
                    labelSymbol.font.size = 20;
                    this.compartmentLayer_polygon = new FeatureLayer({
                        source: graphicsArray,
                        geometryType: "polygon",
                        objectIdField: "ObjectID",
                        fields: [
                            { name: "ObjectID", type: "oid" },
                            { name: "compartmentName", type: "string" },
                            { name: "compartmentRef", type: "string" }
                        ],
                        labelingInfo: {
                            labelExpressionInfo: {
                                expression: "$feature.compartmentName + TextFormatting.NewLine + $feature.compartmentRef",
                            },
                            symbol: labelSymbol,
                            visualVariables: fcconfig.visualVariables
                        },
                        renderer: {
                            type: "simple",
                            symbol: fcconfig.otherPolygonSymbol,
                            visualVariables: fcconfig.visualVariables
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

            confirmedFellingMap.prototype.addWatermark = function () {
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
                        watermarkLayer.visible = !newBasemap.portalItem || newBasemap.portalItem.id !== fcconfig.baseMapForUK;
                    }
                );
                return Promise.resolve();
            };

            confirmedFellingMap.prototype.mapLoadEvt = function () {
                this.commonTools = new Maps_common(this.view);
                if (!this.canMove) {
                    this.commonTools.disableZooming();
                }
                return Promise.resolve();
            };

            confirmedFellingMap.prototype.GetStartingPoint = function (graphicsArray) {
                if (typeof graphicsArray === "undefined" || graphicsArray.length === 0) {
                    var town = maps_html_Helper.getNearestTown();
                    if (!town) {
                        return;
                    }
                    if (!this.commonTools) {
                        return;
                    }
                    this.commonTools.locateAndGoTo(town, locator, fcconfig.esriGeoServiceLocatorUrl);
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

            confirmedFellingMap.prototype.convertToOSGridReference = function (centroid) {

                let returnString = "";
                try {
                    const point27700 = { x: parseFloat(centroid.x.toFixed()), y: parseFloat(centroid.y.toFixed()) };

                    // Transform the point to EPSG:4326 (WGS84)
                    const pointWGS84 = Proj4js("EPSG:27700", "EPSG:4326", point27700);

                    let cord = new GT_WGS84();
                    let osgb = GT_Math.WGS84_To_OSGB(new GT_WGS84(pointWGS84.y, pointWGS84.x));
                    const tester = osgb.getGridRefForEngland(fcconfig.gridPrecision);
                    if (tester === "SV 00000 00000" | tester.startsWith("undefined")) {
                        return returnString;
                    }

                    returnString = tester;
                }

                catch (error) {
                    console.error("Error:", error);
                }
                return returnString;
            };

            confirmedFellingMap.prototype.focusOnCompartment = function (compartmentId) {
                const compartments = maps_html_Helper.getCompartments();
                const relevant = compartments.filter(x => x.Id === compartmentId);

                if (relevant.length < 1) {
                    console.warn("No matching compartment found");
                    return;
                }

                var item = relevant[0];

                const view = this.view;

                if (!view) {
                    console.error("Map view is undefined.");
                    return;
                }

                var geometry = {
                    type: "polygon",
                    rings: item.GIS.rings,
                    spatialReference: item.GIS.spatialReference.wkid,
                };

                let graphic = new Graphic({
                    geometry: geometry,
                    attributes: {
                        compartmentName: item.label,
                        compartmentRef: ""
                    },
                });

                view.when(() => {
                    setTimeout(() => {
                        view.goTo({ target: graphic })
                            .catch(err => console.error("goTo failed:", err));
                    }, 200);
                });
            };

            return confirmedFellingMap;
        }());
        return confirmedFellingMap;
    });
