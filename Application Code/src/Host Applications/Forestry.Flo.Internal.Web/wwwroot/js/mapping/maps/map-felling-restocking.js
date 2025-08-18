define(["require",
    "exports",
    "esri/config",
    "esri/Map",
    "esri/views/MapView",
    "esri/Basemap",
    "esri/Graphic",
    "esri/rest/locator",
    "esri/layers/FeatureLayer",
    "esri/Geometry/Polygon",
    "esri/Geometry/Point",
    "esri/Geometry/geometryEngine",
    "/js/mapping/maps-html-helper.js?v=" + Date.now(),
    "/js/mapping/maps-common.js?v=" + Date.now(),
    "/js/mapping/gthelper/gt-wgs84.js?v=" + Date.now(),
    "/js/mapping/gthelper/gt-math.js?v=" + Date.now(),
    "/js/mapping/gthelper/proj4.js?v=" + Date.now(),
    "/js/mapping/fcconfig.js?v=" + Date.now()],
    function (require,
        exports,
        config,
        Map,
        MapView,
        Basemap,
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
        fcconfig) {
        var confirmedFellingMap = /** @class */ (function () {
            function confirmedFellingMap(location, canMove, filter) {
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
                        id: fcconfig.baseMapForUK,
                    },
                });
                this.map = new Map({
                    basemap: baseMap,
                });

                var button = document.getElementById("view");
                var that = this;

                if (button !== null) {
                    var items = maps_html_Helper.getCompartments("restocking");

                    if (items && items.length > 0) {
                        const panelMenu = document.getElementById("panelMenu");
                        if (!panelMenu) {
                            return
                        }

                        panelMenu.removeAttribute("closed");
                    }

                    button.addEventListener("click", (evt) => {
                        var state = document.getElementById("runningMode");
                        if (state.value === "felling") {
                            state.value = "restocking"
                            document.getElementById("header-title").innerHTML = "Restocking";
                            button.text = "View Felling";
                            button.icon = "analysis";
                        } else {
                            state.value = "felling"
                            document.getElementById("header-title").innerHTML = "Felling";
                            button.text = "View Restocking";
                            button.icon = "analysis";
                        }

                        that.map.layers.removeAll();
                        that.loadCompartments()
                            .then(that.GetStartingPoint.bind(that));

                    });
                }

                this.view = new MapView({
                    map: this.map,
                    container: location,
                    extent: fcconfig.englandExtent,
                });

                const splitIds = filter.split(",");

                this.view
                    .when(this.mapLoadEvt.bind(this))
                    .then(() => this.loadCompartments(splitIds))
                    .then(this.GetStartingPoint.bind(this));
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
                    else if (item.GIS.paths) {
                        geometry = {
                            type: "polyline",
                            paths: item.GIS.paths,
                            spatialReference: item.GIS.spatialReference.wkid,
                        };
                    }
                    else {
                        geometry = {
                            type: "point",
                            longitude: item.GIS.x,
                            latitude: item.GIS.y,
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

                            switch (graphic.geometry.type) {
                                case "polygon":
                                    centroid = graphic.geometry.centroid;
                                    break;
                                case "polyline":
                                    centroid = graphic.geometry.extent.center;
                                    break;
                                default:
                                    centroid = graphic.geometry;
                                    break;
                            }

                            graphic.attributes.compartmentRef = that.convertToOSGridReference(centroid);

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
                this.loadLineLayer(graphicsArray.filter(function (item) {
                    return item.geometry.type === "polyline";
                }));
                this.loadPointLayer(graphicsArray.filter(function (item) {
                    return item.geometry.type === "point";
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

            confirmedFellingMap.prototype.loadPointLayer = function (graphicsArray) {
                try {
                    if (typeof graphicsArray === "undefined" || graphicsArray.length === 0) {
                        return Promise.resolve();
                    }
                    var labelSymbol = JSON.parse(JSON.stringify(fcconfig.activeTextSymbol));
                    labelSymbol.font.size = 20;
                    labelSymbol.xoffset = -20;
                    labelSymbol.yoffset = -15;
                    this.compartmentLayer_point = new FeatureLayer({
                        source: graphicsArray,
                        geometryType: "point",
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
                            symbol: fcconfig.otherPointSymbol,
                            visualVariables: fcconfig.visualVariables
                        },
                    });
                    this.map.add(this.compartmentLayer_point);
                }
                catch (e) {
                    if (window.console) {
                        console.error("Failed to create all required parts for view.", e);
                        Promise.reject(e);
                    }
                }
                return Promise.resolve();
            };

            confirmedFellingMap.prototype.loadLineLayer = function (graphicsArray) {
                try {
                    if (typeof graphicsArray === "undefined" || graphicsArray.length === 0) {
                        return Promise.resolve();
                    }
                    var labelSymbol = JSON.parse(JSON.stringify(fcconfig.activeTextSymbol));
                    labelSymbol.font.size = 20;
                    this.compartmentLayer_line = new FeatureLayer({
                        source: graphicsArray,
                        geometryType: "polyline",
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
                            symbol: fcconfig.otherLineSymbol,
                            visualVariables: fcconfig.visualVariables
                        },
                    });
                    this.map.add(this.compartmentLayer_line);
                }
                catch (e) {
                    if (window.console) {
                        console.error("Failed to create all required parts for view.", e);
                        Promise.reject(e);
                    }
                }
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
