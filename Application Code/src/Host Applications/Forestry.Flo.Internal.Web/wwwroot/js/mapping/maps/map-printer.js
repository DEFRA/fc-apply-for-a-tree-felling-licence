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
    "/js/mapping/maps-html-helper.js",
    "/js/mapping/fcconfig.js?v=" + Date.now(),
    "esri/core/reactiveUtils"],
    function (require, exports, config, Map, MapView, Basemap,
        WMSLayer,
        WMTSLayer,
        GraphicsLayer,
        Graphic, locator, maps_html_Helper, fcconfig, reactiveUtils) {
        var printerMap = /** @class */ (function () {
            function printerMap(location) {
                this.location = location;
                this.centroidOperator = null;
                config.apiKey = fcconfig.esriApiKey;

                var baseMap = new Basemap({
                    portalItem: {
                        id: fcconfig.baseMapForUK,
                    },
                });

                this.selectedShape = null;
                this.symbolEditorShown = false;

                this.map = new Map({
                    basemap: baseMap,
                });



                this.graphicsArray = [];

                const mapComponent = document.getElementById(location);
                mapComponent.map = this.map;

                mapComponent.viewOnReady().then(() => {
                    this.view = mapComponent.view;
                    if (!this.view) {
                        console.error("ArcGIS view is undefined. Map component may not be initialized or visible.");
                        return;
                    }
                    this.view.extent = fcconfig.englandExtent;
                    this.view.constraints = { maxZoom: 20 };
                    return Promise.resolve();
                })
                    .then(this.loadCompartments.bind(this))
                    .then(this.setupWidgets.bind(this))
                    .then(this.GetStartingPoint.bind(this))
                    .then(this.drawList.bind(this))
                    .then(this.addWatermark.bind(this));
            }

            printerMap.prototype.setupWidgets = async function () {
                var that = this;

                that.map.basemap.title = "OS Map";

                var wmsLayer = new WMSLayer(fcconfig.wmsLayer);

                var wmsBasemap = new Basemap({
                    baseLayers: [wmsLayer],
                    title: fcconfig.wmsLayerName,
                    id: "wmsBasemap"
                });

                const mapComponent = document.getElementById(this.location)
                await mapComponent.viewOnReady();

                const [LocalBasemapsSource] = await $arcgis.import([
                    "@arcgis/core/widgets/BasemapGallery/support/LocalBasemapsSource.js",
                ]);

                const basemapGallery = document.getElementById("printer-basemaps-container").querySelector("arcgis-basemap-gallery");
                basemapGallery.source = new LocalBasemapsSource({
                    basemaps: [
                        this.map.basemap,
                        wmsBasemap
                    ]
                });

                basemapGallery.view = this.view;
              

                const printTool = document.getElementById("printer-export-container").querySelector("arcgis-print");
                printTool.view = this.view;
                printTool.printServiceUrl = fcconfig.printServiceUrl;

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

                } else if (this.graphicsArray.length > 2) {

                    this.view.on("click",
                        (evt) => {
                            that.view.hitTest(evt, { include: that._drawingLayer }).then(function (response) {
                                if (typeof response?.results === "undefined" || response.results.length === 0) {
                                    that.selectedShape = null;
                                    that.handleShapeChange();
                                    return;
                                }

                                var filtered = response?.results.filter((item) => {
                                    return item.graphic.attributes.isShape;
                                });

                                if (typeof filtered === "undefined" || filtered.length === 0) {
                                    that.selectedShape = null;
                                    that.handleShapeChange();
                                    return;
                                }

                                var shapeIndex = that.graphicsArray.findIndex((shape) => {
                                    return shape.attributes.isShape === true && shape.attributes.itemId === filtered[0].graphic.attributes.itemId;
                                });

                                var labelIndex = that.graphicsArray.findIndex((shape) => {
                                    return shape.attributes.isShape === false && shape.attributes.shapeId === filtered[0].graphic.attributes.itemId;
                                });

                                that.selectedShape = {
                                    shapeSymbol: that.graphicsArray[shapeIndex].symbol,
                                    labelSymbol: that.graphicsArray[labelIndex].symbol,
                                    id: that.graphicsArray[shapeIndex].attributes.itemId
                                };


                                document.getElementById("ployon-shape-symbol").value = that.selectedShape.shapeSymbol.style;
                                document.getElementById("ployon-shape-color").value = that.rgba2hex(that.selectedShape.shapeSymbol.color);
                                document.getElementById("ploygon-line-color").value = that.rgba2hex(that.selectedShape.shapeSymbol.outline.color);
                                document.getElementById("ploygon-line-width").value = that.selectedShape.shapeSymbol.outline.width;

                                document.getElementById("printer-label-text").value = that.selectedShape.labelSymbol.text;
                                document.getElementById("printer-label-color").value = that.rgba2hex(that.selectedShape.labelSymbol.color);
                                document.getElementById("printer-halo-color").value = that.rgba2hex(that.selectedShape.labelSymbol.haloColor);
                                document.getElementById("printer-halo-size").value = that.selectedShape.labelSymbol.haloSize;
                                document.getElementById("printer-label-y-offset").value = that.selectedShape.labelSymbol.yoffset
                                document.getElementById("printer-label-x-offset").value = that.selectedShape.labelSymbol.xoffset

                                document.getElementById("printer-font-size").value = that.selectedShape.labelSymbol.font.size;
                                document.getElementById("printer-font-weight").value = that.selectedShape.labelSymbol.font.weight;
                                that.handleShapeChange();
                            });
                        });
                }

                /*Cal*/

                const BM_Help = document.getElementById("help-printer-basemap-button");
                if (BM_Help) {
                    BM_Help.addEventListener("click", () => {
                        that.OpenPopUp("/Compartment/help#basemapHelp");
                    });
                }

                const BM_Close = document.getElementById("close-printer-basemap-button");
                if (BM_Close) {
                    BM_Close.addEventListener("click", () => {
                        document.querySelector(`[data-action-id=${that.activeWidget}]`).active = false;
                        document.querySelector(`[data-panel-id=${that.activeWidget}]`).hidden = true;
                        that.activeWidget = null;
                    });
                }

                const SH_Help = document.getElementById("help-printer-show-hide-button");
                if (SH_Help) {
                    SH_Help.addEventListener("click", () => {
                        that.OpenPopUp("/Compartment/help#ShowHideHelp");
                    });
                }

                const SH_Close = document.getElementById("close-printer-show-hide-button");
                if (SH_Close) {
                    SH_Close.addEventListener("click", () => {
                        document.querySelector(`[data-action-id=${that.activeWidget}]`).active = false;
                        document.querySelector(`[data-panel-id=${that.activeWidget}]`).hidden = true;
                        that.activeWidget = null;
                    });
                }

                const E_Help = document.getElementById("help-printer-export-button");
                if (E_Help) {
                    E_Help.addEventListener("click", () => {
                        that.OpenPopUp("/Compartment/help#ExportHelp");
                    });
                }

                const E_Close = document.getElementById("close-printer-export-button");
                if (E_Close) {
                    E_Close.addEventListener("click", () => {
                        document.querySelector(`[data-action-id=${that.activeWidget}]`).active = false;
                        document.querySelector(`[data-panel-id=${that.activeWidget}]`).hidden = true;
                        that.activeWidget = null;
                    });
                }

                const SE_Help = document.getElementById("help-printer-symbol-button");
                if (SE_Help) {
                    SE_Help.addEventListener("click", () => {
                        that.OpenPopUp("/Compartment/help#ExportHelp");
                    });
                }

                const SE_Close = document.getElementById("close-printer-symbol-button");
                if (SE_Close) {
                    SE_Close.addEventListener("click", () => {
                        document.querySelector(`[data-action-id=${that.activeWidget}]`).active = false;
                        document.querySelector(`[data-panel-id=${that.activeWidget}]`).hidden = true;
                        that.activeWidget = null;
                    });
                }

                document.getElementById("ployon-shape-symbol")?.addEventListener("change", (evt) => {
                    const graphic = this.GetCurrentShape(this.selectedShape.id);
                    if (graphic) {
                        const newSymbol = {
                            type: this.selectedShape.shapeSymbol.type,
                            style: evt.target.value,
                            color: this.selectedShape.shapeSymbol.color,
                            outline: {
                                color: this.selectedShape.shapeSymbol.outline.color,
                                width: this.selectedShape.shapeSymbol.width,
                            }
                        };
                        graphic.symbol = newSymbol;
                    }
                });
                document.getElementById("ployon-shape-color")?.addEventListener("change", (evt) => {
                    const graphic = this.GetCurrentShape(this.selectedShape.id);
                    if (graphic) {
                        const newSymbol = {
                            type: this.selectedShape.shapeSymbol.type,
                            style: this.selectedShape.shapeSymbol.style,
                            color: evt.target.value,
                            outline: {
                                color: this.selectedShape.shapeSymbol.outline.color,
                                width: this.selectedShape.shapeSymbol.width,
                            }
                        };
                        graphic.symbol = newSymbol;
                    }
                });

                document.getElementById("ploygon-line-color")?.addEventListener("change", (evt) => {
                    const graphic = this.GetCurrentShape(this.selectedShape.id);
                    if (graphic) {
                        const newSymbol = {
                            type: this.selectedShape.shapeSymbol.type,
                            style: this.selectedShape.shapeSymbol.style,
                            color: this.selectedShape.shapeSymbol.color,
                            outline: {
                                color: evt.target.value,
                                width: this.selectedShape.shapeSymbol.width,
                            }
                        };
                        graphic.symbol = newSymbol;
                    }
                });

                document.getElementById("ploygon-line-width")?.addEventListener("change", (evt) => {
                    const graphic = this.GetCurrentShape(this.selectedShape.id);
                    if (graphic) {
                        const newSymbol = {
                            type: this.selectedShape.shapeSymbol.type,
                            style: this.selectedShape.shapeSymbol.style,
                            color: this.selectedShape.shapeSymbol.color,
                            outline: {
                                color: this.selectedShape.shapeSymbol.outline.color,
                                width: evt.target.value,
                            }
                        };
                        graphic.symbol = newSymbol;
                    }
                });


                document.getElementById("printer-label-text")?.addEventListener("change", (evt) => {
                    const graphic = this.GetCurrentLabel(this.selectedShape.id);
                    if (graphic) {
                        const newSymbol = {
                            type: this.selectedShape.labelSymbol.type,
                            text: evt.target.value,
                            color: this.selectedShape.labelSymbol.color,
                            haloColor: this.selectedShape.labelSymbol.haloColor,
                            haloSize: this.selectedShape.labelSymbol.haloSize,
                            xoffset: this.selectedShape.labelSymbol.xoffset,
                            yoffset: this.selectedShape.labelSymbol.yoffset,
                            font: {
                                size: this.selectedShape.labelSymbol.font.size,
                                weight: this.selectedShape.labelSymbol.font.weight,
                            }
                        };
                        graphic.symbol = newSymbol;
                    }
                });

                document.getElementById("printer-label-color")?.addEventListener("change", (evt) => {
                    const graphic = this.GetCurrentLabel(this.selectedShape.id);
                    if (graphic) {
                        const newSymbol = {
                            type: this.selectedShape.labelSymbol.type,
                            text: this.selectedShape.labelSymbol.text,
                            color: evt.target.value,
                            haloColor: this.selectedShape.labelSymbol.haloColor,
                            haloSize: this.selectedShape.labelSymbol.haloSize,
                            xoffset: this.selectedShape.labelSymbol.xoffset,
                            yoffset: this.selectedShape.labelSymbol.yoffset,
                            font: {
                                size: this.selectedShape.labelSymbol.font.size,
                                weight: this.selectedShape.labelSymbol.font.weight,
                            }
                        };
                        graphic.symbol = newSymbol;
                    }
                });
                document.getElementById("printer-halo-color")?.addEventListener("change", (evt) => {
                    const graphic = this.GetCurrentLabel(this.selectedShape.id);
                    if (graphic) {
                        const newSymbol = {
                            type: this.selectedShape.labelSymbol.type,
                            text: this.selectedShape.labelSymbol.text,
                            color: this.selectedShape.labelSymbol.color,
                            haloColor: evt.target.value, 
                            haloSize: this.selectedShape.labelSymbol.haloSize,
                            xoffset: this.selectedShape.labelSymbol.xoffset,
                            yoffset: this.selectedShape.labelSymbol.yoffset,
                            font: {
                                size: this.selectedShape.labelSymbol.font.size,
                                weight: this.selectedShape.labelSymbol.font.weight,
                            }
                        };
                        graphic.symbol = newSymbol;
                    }
                });

                document.getElementById("printer-halo-size")?.addEventListener("change", (evt) => {
                    const graphic = this.GetCurrentLabel(this.selectedShape.id);
                    if (graphic) {
                        const newSymbol = {
                            type: this.selectedShape.labelSymbol.type,
                            text: this.selectedShape.labelSymbol.text,
                            color: this.selectedShape.labelSymbol.color,
                            haloColor: this.selectedShape.labelSymbol.haloColor,
                            haloSize: evt.target.value,
                            xoffset: this.selectedShape.labelSymbol.xoffset,
                            yoffset: this.selectedShape.labelSymbol.yoffset,
                            font: {
                                size: this.selectedShape.labelSymbol.font.size,
                                weight: this.selectedShape.labelSymbol.font.weight,
                            }
                        };
                        graphic.symbol = newSymbol;
                    }
                });

                document.getElementById("printer-label-y-offset")?.addEventListener("change", (evt) => {
                    const graphic = this.GetCurrentLabel(this.selectedShape.id);
                    if (graphic) {
                        const newSymbol = {
                            type: this.selectedShape.labelSymbol.type,
                            text: this.selectedShape.labelSymbol.text,
                            color: this.selectedShape.labelSymbol.color,
                            haloColor: this.selectedShape.labelSymbol.haloColor,
                            haloSize: this.selectedShape.labelSymbol.haloSize,
                            xoffset: this.selectedShape.labelSymbol.xoffset,
                            yoffset: evt.target.value,
                            font: {
                                size: this.selectedShape.labelSymbol.font.size,
                                weight: this.selectedShape.labelSymbol.font.weight,
                            }
                        };
                        graphic.symbol = newSymbol;
                    }
                });

                document.getElementById("printer-label-x-offset")?.addEventListener("change", (evt) => {
                    const graphic = this.GetCurrentLabel(this.selectedShape.id);
                    if (graphic) {
                        const newSymbol = {
                            type: this.selectedShape.labelSymbol.type,
                            text: this.selectedShape.labelSymbol.text,
                            color: this.selectedShape.labelSymbol.color,
                            haloColor: this.selectedShape.labelSymbol.haloColor,
                            haloSize: this.selectedShape.labelSymbol.haloSize,
                            xoffset: evt.target.value,
                            yoffset: this.selectedShape.labelSymbol.yoffset,
                            font: {
                                size: this.selectedShape.labelSymbol.font.size,
                                weight: this.selectedShape.labelSymbol.font.weight,
                            }
                        };
                        graphic.symbol = newSymbol;
                    }
                });

                document.getElementById("printer-font-size")?.addEventListener("change", (evt) => {
                    const graphic = this.GetCurrentLabel(this.selectedShape.id);
                    if (graphic) {
                        const newSymbol = {
                            type: this.selectedShape.labelSymbol.type,
                            text: this.selectedShape.labelSymbol.text,
                            color: this.selectedShape.labelSymbol.color,
                            haloColor: this.selectedShape.labelSymbol.haloColor,
                            haloSize: this.selectedShape.labelSymbol.haloSize,
                            xoffset: this.selectedShape.labelSymbol.xoffset,
                            yoffset: this.selectedShape.labelSymbol.yoffset,
                            font: {
                                size: evt.target.value,
                                weight: this.selectedShape.labelSymbol.font.weight
                            }
                        };
                        graphic.symbol = newSymbol;
                    }
                });

                document.getElementById("printer-font-weight")?.addEventListener("change", (evt) => {
                    const graphic = this.GetCurrentLabel(this.selectedShape.id);
                    if (graphic) {
                        const newSymbol = {
                            type: this.selectedShape.labelSymbol.type,
                            text: this.selectedShape.labelSymbol.text,
                            color: this.selectedShape.labelSymbol.color,
                            haloColor: this.selectedShape.labelSymbol.haloColor,
                            haloSize: this.selectedShape.labelSymbol.haloSize,
                            xoffset: this.selectedShape.labelSymbol.xoffset,
                            yoffset: this.selectedShape.labelSymbol.yoffset,
                            font: {
                                size: this.selectedShape.labelSymbol.font.size,
                                weight: evt.target.value
                            }
                        };
                        graphic.symbol = newSymbol;
                    }
                });


                const handleActionBarClick = ({ target }) => {
                    if (target.tagName !== "CALCITE-ACTION") {
                        return;
                    }

                    if (that.activeWidget) {
                        document.querySelector(`[data-action-id=${that.activeWidget}]`).active = false;
                        document.querySelector(`[data-panel-id=${that.activeWidget}]`).hidden = true;
                    }

                    const nextWidget = target.dataset.actionId;
                    if (nextWidget && nextWidget !== that.activeWidget) {
                        document.querySelector(`[data-action-id=${nextWidget}]`).active = true;
                        document.querySelector(`[data-panel-id=${nextWidget}]`).hidden = false;
                        document.getElementById("panelMenu").collapsed = false;
                        that.activeWidget = nextWidget;
                    } else {
                        that.activeWidget = null;
                        document.getElementById("panelMenu").collapsed = true;
                    }

                    if (nextWidget === "printer-symbol-editor") {
                        this.handleShapeChange();
                        this.symbolEditorShown = true;
                    } else {
                        this.symbolEditorShown = false;
                    }
                };

                document.getElementById("printer-action-bar").addEventListener("click", handleActionBarClick);



                var button = document.getElementById("printer-view");
                if (button !== null) {

                    var items = maps_html_Helper.getCompartments("restocking");

                    if (items && items.length > 0) {
                        if (!panelMenu) {
                            return
                        }

                        panelMenu.removeAttribute("closed");
                    }
                    button.addEventListener("click", (evt) => {
                        const state = document.getElementById("printer-runningMode");
                        const headerTitle = document.getElementById("printer-header-title");

                        if (state.value === "felling") {
                            state.value = "restocking"
                            if (headerTitle) {
                                headerTitle.innerHTML = "Generate Image - Restocking";
                            }
                            button.text = "View Felling";
                            button.icon = "analysis";
                        } else {
                            state.value = "felling"
                            if (headerTitle) {
                                headerTitle.innerHTML = "Generate Image - Felling";
                            }
                            button.text = "View Restocking";
                            button.icon = "analysis";
                        }
                        this.view.graphics.removeAll();
                        this.loadCompartments()
                            .then(this.drawList.bind(this))
                            .then(this.GetStartingPoint.bind(this));

                    });
                }

                return Promise.resolve();
            }

            printerMap.prototype.GetCurrentShape = function (id) {
                var shapeIndex = this.graphicsArray.findIndex((shape) => {
                    return shape.attributes.isShape === true && shape.attributes.itemId === id;
                });


                if (shapeIndex === -1) {
                    return null;
                }

                return this.view.graphics.items[shapeIndex];
            }

            printerMap.prototype.GetCurrentLabel = function (id) {
                var labelIndex = this.graphicsArray.findIndex((shape) => {
                    return shape.attributes.isShape === false && shape.attributes.shapeId === id;
                });

                return this.graphicsArray[labelIndex];
            }

            printerMap.prototype.handleShapeChange = function () {
                const noShape = document.getElementById('printer-no-shape-selected');
                const shape = document.getElementById('printer-polygon-symbol');
                const label = document.getElementById('printer-label-symbol');

                if (!this.selectedShape) {
                    noShape?.removeAttribute('hidden');
                    noShape?.setAttribute("expanded", "");
                    shape?.setAttribute("hidden", "");
                    label?.setAttribute("hidden", "");

                } else {
                    shape?.removeAttribute('hidden');
                    label?.removeAttribute('hidden');
                    noShape?.removeAttribute("expanded");
                    noShape?.setAttribute("hidden", "");
                }
            }


            printerMap.prototype.drawList = function () {
                var list = document.getElementById("printer-compartments-list")
                if (list) {
                    list.innerHTML = "";

                    this.graphicsArray.filter((shape) => { return shape.attributes.isShape }).forEach(shape => {
                        const listItem = document.createElement("calcite-list-item");
                        listItem.setAttribute("label", shape.attributes.compartmentName);
                        listItem.setAttribute("value", shape.attributes.itemId);
                        listItem.addEventListener("click", this.changeSelectionEvt.bind(this));

                        const iconAction = document.createElement("calcite-action");
                        iconAction.setAttribute("slot", "actions-end");
                        iconAction.setAttribute("icon", "layer")
                        iconAction.setAttribute("text", `${shape.attributes.compartmentName} is visible`);
                        iconAction.setAttribute("id", `ActionItem-${shape.attributes.itemId}`);
                        iconAction.addEventListener("click", this.changeSelectionChildEvt.bind(this));
                        listItem.appendChild(iconAction);
                        list.appendChild(listItem);
                    });

                }

                return Promise.resolve();
            }

            printerMap.prototype.addWatermark = function () {
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
                this.view.ui.move("zoom", "top-right");

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

            printerMap.prototype.changeSelectionChildEvt = function (evt) {
                this.changeSelectionEvt({ target: evt.target.parentElement })
            }

            printerMap.prototype.changeSelectionEvt = function (evt) {
                const target = evt.target;

                if (!target || !target.children || target.children.length < 1) {
                    return;
                }

                const icon = target.children[0];

                if (!icon) {
                    return;
                }

                const shapeId = target.getAttribute("value");

                if (icon.getAttribute("icon") === "layer-hide") {
                    icon.setAttribute("icon", "layer");
                    const shapeIndex = this.graphicsArray.findIndex((shape) => {
                        return shape.attributes.itemId === shapeId;
                    });

                    if (shapeIndex === -1) {
                        return;
                    }
                    this.view.graphics.add(this.graphicsArray[shapeIndex]);

                    var labelIndex = this.graphicsArray.findIndex((shape) => {
                        return shape.attributes.shapeId === shapeId;
                    });

                    if (labelIndex === -1) {
                        return;
                    }

                    this.view.graphics.add(this.graphicsArray[labelIndex]);
                }
                else {
                    icon.setAttribute("icon", "layer-hide");
                    const shapeIndex = this.view.graphics.items.findIndex((shape) => {
                        return shape.attributes.itemId === shapeId;
                    });
                    if (shapeIndex === -1) {
                        return;
                    }
                    this.view.graphics.remove(this.view.graphics.items[shapeIndex]);

                    var labelIndex = this.view.graphics.items.findIndex((shape) => {
                        return shape.attributes.shapeId === shapeId;
                    });

                    if (labelIndex === -1) {
                        return;
                    }

                    this.view.graphics.remove(this.view.graphics.items[labelIndex]);
                }
            }

            printerMap.prototype.loadCompartments = async function () {
                var items = maps_html_Helper.getCompartments(maps_html_Helper.getViewString("printer-runningMode"));

                if (!this.centroidOperator) {
                    const [centroidOperator] = await $arcgis.import([
                        "@arcgis/core/geometry/operators/centroidOperator.js"
                    ]);

                    this.centroidOperator = centroidOperator;
                }

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
                        centroid = that.centroidOperator.execute(new Graphic({ geometry: geometry, symbol: shapeSymbol }).geometry);
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
                                centroid = that.centroidOperator.execute(shape.geometry);
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

            printerMap.prototype.rgba2hex = function (orig) {
                orig = orig.toString();
                var a,
                    isPercent,
                    rgb = orig
                        .replace(/\s/g, "")
                        .match(/^rgba?\((\d+),(\d+),(\d+),?([^,\s)]+)?/i),
                    alpha = ((rgb && rgb[4]) || "").trim(),
                    hex = rgb
                        ? (rgb[1] | (1 << 8)).toString(16).slice(1) +
                        (rgb[2] | (1 << 8)).toString(16).slice(1) +
                        (rgb[3] | (1 << 8)).toString(16).slice(1)
                        : orig;

                if (alpha !== "") {
                    a = alpha;
                } else {
                    a = 0o1;
                }

                return "#" + hex;
            }

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
