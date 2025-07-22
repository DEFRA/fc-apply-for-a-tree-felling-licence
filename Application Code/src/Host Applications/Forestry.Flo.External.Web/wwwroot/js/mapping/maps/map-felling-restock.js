define([
    "require",
    "exports",
    "esri/Map",
    "esri/views/MapView",
    "esri/config",
    "esri/Basemap",
    "esri/Graphic",
    "esri/layers/GraphicsLayer",
    "/js/mapping/mapSettings.js?v=" + Date.now(),
    "/js/mapping/maps-common.js?v=" + Date.now()
],
    function (
        require,
        exports,
        Map, MapView,
        config,
        Basemap,
        Graphic,
        GraphicLayer,
        mapSettings,
        Maps_Common) {
        "use strict";

        var FellingRestock = /** @class */ (function () {
            function FellingRestock(location, shouldReturnToApplicationSummary) {
                this.shouldReturnToApplicationSummary = shouldReturnToApplicationSummary;
                this.lastSelected = [];
                this.drawinglayer;

                config.apiKey = mapSettings.esriApiKey;

                ////This is the default map for the FC system
                var baseMap = new Basemap({
                    portalItem: {
                        id: mapSettings.baseMapForUK
                    }
                });
                this.map = new Map({
                    basemap: baseMap
                });

                //Lets Work on the guide
                this.view = new MapView({
                    map: this.map,
                    container: location,
                    extent: mapSettings.englandExtent
                });
                this.drawinglayer = new GraphicLayer();
                this.map.add(this.drawinglayer)

                this.view.when(this.LoadData.bind(this))
                    .then(this.WireUpHitTest.bind(this));
            }

            FellingRestock.prototype.WireUpHitTest = function () {
                var that = this;

                that.view.on("pointer-move", (evt) => {
                    that.lastSelected.forEach((item) => {
                        that.UpdateGraphic(item, false);
                    });
                    that.lastSelected = [];

                    that.view.hitTest(evt).then((hit) => {
                        if (typeof hit.results === "undefined" || hit.results.length === 0) {
                            return;
                        }

                        hit.results.forEach((result) => {
                            if (result.graphic.attributes.shapeID) {
                                return;
                            }
                            that.UpdateGraphic(result.graphic, true);
                            that.lastSelected.push(result.graphic);
                        });
                    })
                });

                this.view.on("click", (evt) => {
                    that.view.hitTest(evt).then((result) => {

                        if (typeof result.results === "undefined" || result.results.length === 0) {
                            return;
                        }
                        var r = result.results[0]; //In GIS land Always take the top!!
                        if (typeof r === "undefined" || r === null) {
                            return;
                        }
                        if (typeof r.graphic.attributes === "undefined" || r.graphic.attributes === null) {
                            return;
                        }
                        if (typeof r.graphic.attributes.flowID === "undefined" || r.graphic.attributes.flowID === null || r.graphic.attributes.flowID === "") {
                            return;
                        }

                        // Brute force load.
                        const origin = window.location.origin
                        if (typeof window.location.pathname === "undefined" || window.location.pathname === null || window.location.pathname === "") {
                            return;
                        }

                        if (typeof origin === "undefined" || origin === null || origin === "") {
                            return;
                        }

                        const items = window.location.pathname.split('/');

                        if (typeof items === "undefined" || items.length === 0) {
                            return;
                        }

                        var returnToApplicationSummaryParam = this.shouldReturnToApplicationSummary ? "&returnToApplicationSummary=True" : "";

                        var path = `${origin}/FellingLicenceApplication/FellingAndRestockingDetails?applicationId=${items[items.length - 1]}&compartmentId=${r.graphic.attributes.flowID}${returnToApplicationSummaryParam}`;

                        window.location.replace(path);
                    });
                });
            }

            FellingRestock.prototype.UpdateGraphic = function (graphic, value) {
                switch (graphic.geometry.type) {
                    case "polyline":
                        graphic.symbol = (value) ? mapSettings.selectedLineSymbol : mapSettings.activeLineSymbol;
                        break;
                    case "point":
                        graphic.symbol = (value) ? mapSettings.selectedPointSymbol : mapSettings.activePointSymbol;
                        break;
                    default:
                        graphic.symbol = (value) ? mapSettings.selectedPolygonSymbol : mapSettings.activePolygonSymbol;
                        break;
                }
            }

            FellingRestock.prototype.LoadData = function () {
                var paths = window.location.pathname.split('/');
                var that = this;
                fetch('/api/gis/FellingAndRestockingSelectedCompartments?applicationId=' + paths[paths.length - 1], {
                    credentials: mapSettings.requestParamsAPI.credentials,
                    method: "POST",
                    headers: {
                        'Accept': "application/json",
                        "Content-type": "application/json"
                    }
                })
                    .then((response) => {
                        if (!response.ok) {
                            throw new Error("Failed with HTTP code " + response.status);
                        }
                        return response.json();
                    }).then((data) => {
                        that.loadCompartments(JSON.parse(data));
                        that.mapLoadEvt();
                    });
            }

            FellingRestock.prototype.loadCompartments = function (items) {
                if (typeof items === "undefined" || items.length === 0) {
                    return Promise.resolve([]);
                }
                var that = this;
                var graphicsArray = [];
                items.forEach(function (item) {
                    var geometry;
                    let workingSymbol;
                    var geo = JSON.parse(item.GISData);

                    if (geo.rings) {
                        geometry = {
                            type: "polygon",
                            rings: geo.rings,
                            spatialReference: geo.spatialReference.wkid,
                        }
                        workingSymbol = mapSettings.activePolygonSymbol;
                    }
                    else if (geo.paths) {
                        geometry = {
                            type: "polyline",
                            paths: geo.paths,
                            spatialReference: geo.spatialReference.wkid,
                        };
                        workingSymbol = mapSettings.activeLineSymbol;
                    }
                    else {
                        geometry = {
                            type: "point",
                            longitude: geo.x,
                            latitude: geo.y,
                            spatialReference: geo.spatialReference.wkid,
                        };
                        workingSymbol = mapSettings.activePointSymbol;
                    }

                    if (geometry) {
                        try {
                            const graphic = new Graphic({
                                geometry: geometry,
                                symbol: workingSymbol,
                                attributes: {
                                    compartmentName: item.DisplayName,
                                    flowID: item.Id
                                },
                            });
                            graphicsArray.push(graphic);
                            that.drawinglayer.add(graphic);
                            that.drawinglayer.add(that.getLabelGrapic(graphic));
                        }
                        catch (e) {
                            if (window.console) {
                                console.error("Failed to create all required parts for view.", e);
                            }
                        }
                        return;
                    }
                });

                this.GetStartingPoint(graphicsArray)
                return Promise.resolve(graphicsArray);
            };

            FellingRestock.prototype.mapLoadEvt = function () {
                this.commonTools = new Maps_Common(this.view);
                this.commonTools.disableZooming();
                return Promise.resolve();
            };

            FellingRestock.prototype.getLabelGrapic = function (shapeGraphic) {
                var resx;
                var labelSymbol = JSON.parse(JSON.stringify(mapSettings.activeTextSymbol));
                labelSymbol.text = shapeGraphic.attributes.compartmentName;

                if (shapeGraphic.geometry.type === "point") {
                    labelSymbol.xoffset = mapSettings.pointOffset.xoffset;
                    labelSymbol.yoffset = mapSettings.pointOffset.yoffset;
                    resx = new Graphic({
                        geometry: shapeGraphic.geometry,
                        symbol: labelSymbol,
                    });
                }
                else if (shapeGraphic.geometry.type === "polyline") {
                    labelSymbol.xoffset = mapSettings.pointOffset.xoffset;
                    labelSymbol.yoffset = mapSettings.pointOffset.yoffset;
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
            FellingRestock.prototype.GetStartingPoint = function (graphicsArray) {
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
            return FellingRestock;
        }());
        return FellingRestock;
    });
