define([
    "require",
    "exports",
    "esri/config",
    "esri/Map",
    "esri/views/MapView",
    "esri/Basemap",
    "esri/Graphic",
    "esri/rest/locator",
    "esri/widgets/Expand",
    "esri/widgets/Print",
    "/js/mapping/maps-html-helper.js",
    "/js/mapping/fcconfig.js",
    "/js/mapping/widgets/graphic-selection-widget.js",
    "/js/mapping/widgets/symbol-editor-widget.js"], 
    function (require, exports, config, Map, MapView, Basemap,
        Graphic, locator, Expand, PrintWidget, maps_html_Helper, fcconfig, Selection, SymbolEditor) {
    var printerMap = /** @class */ (function () {
        function printerMap(location) {
            config.apiKey = fcconfig.esriApiKey;

            var baseMap = new Basemap({
                portalItem: {
                    id: fcconfig.baseMapForUK,
                },
            });

            this.map = new Map({
                basemap: baseMap,
            });

            //Lets Work on the guide
            this.view = new MapView({
                map: this.map,
                container: location,
                extent: fcconfig.englandExtent,
            });

            this.graphicsArray = [];

            this.view
                .when(this.loadCompartments.bind(this))
                .then(this.setupWidgets.bind(this))
                .then(this.GetStartingPoint.bind(this));
        }

        printerMap.prototype.setupWidgets = function () {
            var that = this;

            const selection = new Selection({
                shapes: this.graphicsArray.filter((shape) => { return shape.attributes.isShape }).map((shape) => {
                    return {
                        compartmentName: shape.attributes.compartmentName,
                        itemId: shape.attributes.itemId,
                        visible: shape.attributes.visible
                    }
                }),
                view: this.view
            });

            selection.on("show", (evt) => {
                var shapeIndex = that.graphicsArray.findIndex((shape) => {
                    return shape.attributes.itemId === evt.id;
                });
                if (shapeIndex === -1) {
                    return;
                }
                that.view.graphics.add(that.graphicsArray[shapeIndex]);

                if (labelIndex === -1) {
                    return;
                }

                var labelIndex = that.graphicsArray.findIndex((shape) => {
                    return shape.attributes.shapeId === evt.id;
                });
                that.view.graphics.add(that.graphicsArray[labelIndex]);
            });

            selection.on("hide", (evt) => {
                var shapeIndex = that.view.graphics.items.findIndex((shape) => {
                    return shape.attributes.itemId === evt.id;
                });
                if (shapeIndex === -1) {
                    return;
                }
                that.view.graphics.remove(that.view.graphics.items[shapeIndex]);

                if (labelIndex === -1) {
                    return;
                }
                var labelIndex = that.view.graphics.items.findIndex((shape) => {
                    return shape.attributes.shapeId === evt.id;
                });
                that.view.graphics.remove(that.view.graphics.items[labelIndex]);
            });

            const print = new PrintWidget({
                view: this.view,
                // specify your own print service
                printServiceUrl:
                    "https://utility.arcgisonline.com/arcgis/rest/services/Utilities/PrintingTools/GPServer/Export%20Web%20Map%20Task"
            });

            this.editor = new SymbolEditor({
                view: this.view,
            });
            const expandPrinter = new Expand({
                expandIconClass: "esri-icon-printer",
                view: this.view,
                content: print
            });

            const expandViewer = new Expand({
                expandIconClass: "esri-icon-hollow-eye",
                view: this.view,
                content: selection
            });

            const expandStyle = new Expand({
                expandIconClass: "esri-icon-edit",
                view: this.view,
                content: this.editor
            });

            if (this.graphicsArray.length === 2) {
                var shapeIndex = that.graphicsArray.findIndex((shape) => {
                    return shape.attributes.isShape === true;
                });
                if (shapeIndex === -1) {
                    return;
                }
                var labelIndex = that.graphicsArray.findIndex((shape) => {
                    return shape.attributes.isShape === false;
                });

                if (labelIndex === -1) {
                    return;
                }
                this.editor.shape = {
                    shapeType: that.graphicsArray[shapeIndex].geometry.type,
                    shapeSymbol: that.graphicsArray[shapeIndex].symbol,
                    labelSymbol: that.graphicsArray[labelIndex].symbol,
                    id: that.graphicsArray[shapeIndex].attributes.itemId
                };

            } else if (this.graphicsArray.length > 2) {
                this.editor.shape = null;

                this.view.on("click",
                    (evt) => {
                        that.view.hitTest(evt, { include: that._drawingLayer }).then(function (response) {
                            if (typeof response?.results === "undefined" || response.results.length === 0) {
                                that.editor.shape = null;
                                return;
                            }

                            var filtered = response?.results.filter((item) => {
                                return item.graphic.attributes.isShape;
                            });

                            if (typeof filtered === "undefined" || filtered.length === 0) {
                                that.editor.shape = null;
                                return;
                            }

                            var shapeIndex = that.graphicsArray.findIndex((shape) => {
                                return shape.attributes.isShape === true && shape.attributes.itemId ===filtered[0].graphic.attributes.itemId;
                            });

                            var labelIndex = that.graphicsArray.findIndex((shape) => {
                                return shape.attributes.isShape === false && shape.attributes.shapeId === filtered[0].graphic.attributes.itemId;
                            });

                            that.editor.shape = {
                                shapeType: that.graphicsArray[shapeIndex].geometry.type,
                                shapeSymbol: that.graphicsArray[shapeIndex].symbol,
                                labelSymbol: that.graphicsArray[labelIndex].symbol,
                                id: that.graphicsArray[shapeIndex].attributes.itemId
                            };
                        });
                    });
            }

            this.editor.on("shapeChanged", (evt) => {
                var shapeIndex = that.view.graphics.items.findIndex((shape) => {
                    return shape.attributes.itemId === evt.id;
                });

                if (shapeIndex === -1) {
                    return;
                }
                that.view.graphics.items[shapeIndex].symbol = evt.Symbol;
            });

            this.editor.on("labelChanged", (evt) => {
                var shapeIndex = that.view.graphics.items.findIndex((shape) => {
                    return shape.attributes.isShape === false && shape.attributes.shapeId === evt.id;
                });

                if (shapeIndex === -1) {
                    return;
                }
                console.log(that.view.graphics.items[shapeIndex].symbol);
                that.view.graphics.items[shapeIndex].symbol = evt.Symbol;
            });

            this.view.ui.add(expandStyle, { position: "top-right" });

            this.view.ui.add(expandPrinter, {
                position: "top-right"
            });
            this.view.ui.add(expandViewer, "top-right");
            return Promise.resolve();
        }

        printerMap.prototype.loadCompartments = function () {
            var items = maps_html_Helper.getCompartments();
            var that = this;
            this.graphicsArray = [];
            if (typeof items === "undefined" || items.length === 0) {
                return Promise.resolve();
            }
            items.forEach(function (item) {
                var labelSymbol = JSON.parse(JSON.stringify(fcconfig.activeTextSymbol));
                labelSymbol.text = item.label;
                var geometry;
                var shapeSymbol;
                var centroid;


                if (item.GIS.rings) {
                    geometry = {
                        type: "polygon",
                        rings: item.GIS.rings,
                        spatialReference: item.GIS.spatialReference.wkid,
                    };
                    shapeSymbol = fcconfig.otherPolygonSymbol;
                    centroid = geometry.centroid;
                }
                else if (item.GIS.paths) {
                    geometry = {
                        type: "polyline",
                        paths: item.GIS.paths,
                        spatialReference: item.GIS.spatialReference.wkid,
                    };
                    shapeSymbol = fcconfig.otherLineSymbol;
                    centroid = {
                        type: "point",
                        longitude: geometry.paths[0][(Math.floor(geometry.paths[0].length / 2))][0],
                        latitude: geometry.paths[0][(Math.floor(geometry.paths[0].length / 2))][1],
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
                    shapeSymbol = fcconfig.otherPointSymbol;
                    centroid = {
                        type: "point",
                        longitude: item.GIS.x,
                        latitude: item.GIS.y,
                        spatialReference: item.GIS.spatialReference.wkid,
                    };
                }
                if (geometry) {
                    var labelId = that.GenerateUUID();
                    try {
                        let shape = new Graphic({
                            geometry: geometry,
                            attributes: {
                                compartmentName: item.label,
                                itemId: item.Id,
                                labelId: labelId,
                                isShape: true,
                                visible: true
                            },
                            selected: true,
                            symbol: shapeSymbol
                        });

                        if (!centroid) {
                            centroid = shape.geometry.centroid;
                        }
                        let text = new Graphic({
                            geometry: centroid,
                            symbol: labelSymbol,
                            attributes: {
                                itemId: labelId,
                                shapeId: item.Id,
                                isShape: false
                            },
                        })

                        that.graphicsArray.push(shape);
                        that.graphicsArray.push(text);

                        that.view.graphics.add(shape);
                        that.view.graphics.add(text);

                    }
                    catch (e) {
                        if (window.console) {
                            console.error("Failed to create all required parts for view.", e);
                        }
                    }
                    return;
                }
            });
            return Promise.resolve();
        };

        printerMap.prototype.GenerateUUID = function () {
            return ([1e7] + -1e3 + -4e3 + -8e3 + -1e11).replace(/[018]/g, c =>
                (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
            );
        };

        printerMap.prototype.GetStartingPoint = function () {
            if (typeof this.graphicsArray === "undefined" || this.graphicsArray.length === 0) {
                var town = maps_html_Helper.getNearestTown();
                if (!town) {
                    return;
                }
                if (!this.commonTools) {
                    return;
                }
                this.commonTools.locateAndGoTo(town, locator, fcconfig.esriGeoServiceLocatorUrl);
            }
            else if (this.graphicsArray.length === 1) {
                const topItem = this.graphicsArray[0];
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
                    target: this.graphicsArray.filter((item) => { return item.attributes.isShape; }),
                });
            }
        };
        return printerMap;
    }());
    return printerMap;
});
