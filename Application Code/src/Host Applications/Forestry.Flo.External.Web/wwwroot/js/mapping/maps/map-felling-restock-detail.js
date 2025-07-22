define([
    "require",
    "exports",
    "esri/config",
    "esri/Map",
    "esri/views/MapView",
    "esri/Basemap",
    "esri/Graphic",
    "esri/rest/locator",
    "esri/layers/FeatureLayer",
    "/js/mapping/maps-html-helper.js?v=" + Date.now(),
    "/js/mapping/maps-common.js?v=" + Date.now(),
    "/js/mapping/mapSettings.js?v=" + Date.now()],
    function (
        require,
        exports,
        config,
        Map,
        MapView,
        Basemap,
        Graphic,
        locator,
        FeatureLayer,
        maps_html_Helper,
        Maps_common,
        mapSettings) {

    var fellingRestockMap = /** @class */ (function () {
        function fellingRestockMap(location) {
            this.id = "";

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
            //Lets Work on the guide
            this.view = new MapView({
                map: this.map,
                container: location,
                extent: mapSettings.englandExtent,
            });
            this.view
                .when(this.mapLoadEvt.bind(this))
                .then(this.loadCompartments.bind(this))
                .then(this.GetStartingPoint.bind(this));
        }

        fellingRestockMap.prototype.loadCompartments = function () {
            this.id = maps_html_Helper.getCompartmentId();

            var items = maps_html_Helper.getCompartments();
            if (typeof items === "undefined" || items.length === 0) {
                return Promise.resolve([]);
            }
            var that = this;
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
                        graphicsArray.push(new Graphic({
                            geometry: geometry,
                            attributes: {
                                compartmentName: item.label,
                                current: item.Id === that.id ? "Y": "N"
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
            this.loadLineLayer(graphicsArray.filter(function (item) {
                return item.geometry.type === "polyline";
            }));
            this.loadPointLayer(graphicsArray.filter(function (item) {
                return item.geometry.type === "point";
            }));
            return Promise.resolve(graphicsArray);
        };

        fellingRestockMap.prototype.loadPolygonLayer = function (graphicsArray) {
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
                        { name: "current", type:"string" }
                    ],
                    labelingInfo: {
                        labelExpressionInfo: {
                            expression: "$feature.compartmentName",
                        },
                        symbol: labelSymbol,
                        visualVariables: mapSettings.visualVariables.textSize
                    },
                    renderer: {
                        type: "unique-value",
                        field: "current",
                        defaultSymbol: mapSettings.otherPolygonSymbol,
                        uniqueValueInfos: [{
                            value: "Y",
                            symbol: mapSettings.activePolygonSymbol
                        }],
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

        fellingRestockMap.prototype.loadPointLayer = function (graphicsArray) {
            try {
                if (typeof graphicsArray === "undefined" || graphicsArray.length === 0) {
                    return Promise.resolve();
                }
                var labelSymbol = JSON.parse(JSON.stringify(mapSettings.activeTextSymbol));
                labelSymbol.xoffset = -20;
                labelSymbol.yoffset = -15;
                this.compartmentLayer_point = new FeatureLayer({
                    labelsVisible: true,
                    source: graphicsArray,
                    geometryType: "point",
                    objectIdField: "ObjectID",
                    fields: [
                        { name: "ObjectID", type: "oid" },
                        { name: "compartmentName", type: "string" },
                        { name: "current", type: "string" }
                    ],
                    labelingInfo: {
                        labelExpressionInfo: {
                            expression: "$feature.compartmentName",
                        },
                        symbol: labelSymbol,
                        visualVariables: mapSettings.visualVariables.textSize
                    },
                    renderer: {
                        type: "unique-value",
                        field: "current",
                        defaultSymbol: mapSettings.otherPointSymbol,
                        uniqueValueInfos: [{
                            value: "Y",
                            symbol: mapSettings.activePointSymbol
                        }],
                        visualVariables: mapSettings.visualVariables.size
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

        fellingRestockMap.prototype.loadLineLayer = function (graphicsArray) {
            try {
                if (typeof graphicsArray === "undefined" || graphicsArray.length === 0) {
                    return Promise.resolve();
                }
                var labelSymbol = JSON.parse(JSON.stringify(mapSettings.activeTextSymbol));
                this.compartmentLayer_line = new FeatureLayer({
                    labelsVisible: true,
                    source: graphicsArray,
                    geometryType: "polyline",
                    objectIdField: "ObjectID",
                    fields: [
                        { name: "ObjectID", type: "oid" },
                        { name: "compartmentName", type: "string" },
                        { name: "current", type: "string" }
                    ],
                    labelingInfo: {
                        labelExpressionInfo: {
                            expression: "$feature.compartmentName",
                        },
                        symbol: labelSymbol,
                        visualVariables: mapSettings.visualVariables.textSize
                    },
                    renderer: {
                        type: "unique-value",
                        field: "current",
                        defaultSymbol: mapSettings.otherLineSymbol,
                        uniqueValueInfos: [{
                            value: "Y",
                            symbol: mapSettings.activeLineSymbol
                        }],
                        visualVariables: mapSettings.visualVariables.size
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

        fellingRestockMap.prototype.mapLoadEvt = function () {
            this.commonTools = new Maps_common(this.view);
            return Promise.resolve();
        };

        fellingRestockMap.prototype.GetStartingPoint = function (graphicsArray) {
            if (typeof graphicsArray === "undefined" || graphicsArray.length === 0) {
                return;
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

        return fellingRestockMap;
    }());
    return fellingRestockMap;
});
