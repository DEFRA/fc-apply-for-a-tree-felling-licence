define([
    "require",
    "exports",
    "esri/Map",
    "esri/views/MapView",
    "esri/config",
    "esri/Basemap",
    "esri/Graphic",
    "esri/layers/WMSLayer",
    "esri/layers/WMTSLayer",
    "esri/layers/GraphicsLayer",
    "esri/widgets/BasemapGallery",
    "esri/widgets/Expand",
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
        WMSLayer,
        WMTSLayer,
        GraphicsLayer,
        BasemapToggle,
        Expand,
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
                    extent: mapSettings.englandExtent,
                    constraints: {
                        maxZoom: 20
                    }
                });
                this.drawinglayer = new GraphicLayer();
                this.map.add(this.drawinglayer)

                this.view.when(this.LoadData.bind(this))
                    .then(this.addWidgets.bind(this))
                    .then(this.WireUpHitTest.bind(this))
                    .then(this.addWatermark.bind(this));
            }

            FellingRestock.prototype.addWatermark = function () {
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
                return Promise.resolve();
            };

            FellingRestock.prototype.addWidgets = function () {

                this.map.basemap.title = "OS Map";

                var wmsLayer = new WMSLayer(mapSettings.wmsLayer);

                var wmsBasemap = new Basemap({
                    baseLayers: [wmsLayer],
                    title: mapSettings.wmsLayerName,
                    id: "wmsBasemap"
                });


                const basemapToggleWidget = new BasemapToggle({
                    view: this.view,
                    source: [this.map.basemap, wmsBasemap]
                });

                const expandLayer = new Expand({
                    expandIconClass: "esri-icon-basemap",
                    view: this.view,
                    content: basemapToggleWidget
                });

                this.view.ui.add(expandLayer, { position: "top-left" });

                return Promise.resolve();

            };

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
                if (graphic.geometry.type !== "polygon") {
                    return;
                }
                graphic.symbol = (value) ? mapSettings.selectedPolygonSymbol : mapSettings.activePolygonSymbol;
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
                var labelSymbol = JSON.parse(JSON.stringify(mapSettings.activeTextSymbol));
                labelSymbol.text = shapeGraphic.attributes.compartmentName;

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
