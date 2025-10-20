define([
    "require",
    "exports",
    "esri/config",
    "esri/Map",
    "esri/views/MapView",
    "esri/layers/GraphicsLayer",
    "esri/layers/FeatureLayer",
    "esri/Graphic",
    "esri/widgets/Sketch/SketchViewModel",
    "esri/widgets/Search",
    "esri/widgets/ScaleBar",
    "esri/geometry/geometryEngine",
    "esri/widgets/Fullscreen",
    "esri/rest/locator",
    "esri/core/reactiveUtils",
    "esri/geometry/Extent",
    "esri/widgets/BasemapGallery",
    "esri/widgets/CoordinateConversion",
    "esri/widgets/Compass",
    "esri/widgets/Home",
    "esri/Viewpoint",
    "esri/Basemap",
    "esri/layers/WMSLayer",
    "esri/layers/WMTSLayer",
    "esri/widgets/CoordinateConversion/support/Format",
    "esri/widgets/CoordinateConversion/support/Conversion",
    "esri/widgets/Search/SearchSource",
    "/js/mapping/gthelper/proj4.js?v=" + Date.now(),
    "/js/mapping/gthelper/gt-wgs84.js?v=" + Date.now(),
    "/js/mapping/gthelper/gt-osgb.js?v=" + Date.now(),
    "/js/mapping/gthelper/gt-math.js?v=" + Date.now(),
    "/js/mapping/maps-html-helper.js?v=" + Date.now(),
    "/js/mapping/mapSettings.js?v=" + Date.now(),
    "/js/mapping/maps-common.js?v=" + Date.now(),
    "/js/mapping/widgets/alert-widget.js?v=" + Date.now(),
    "/js/mapping/KMLConvertor.js?v=" + Date.now()],
    function (
        require,
        exports,
        config,
        Map,
        MapView,
        GraphicsLayer,
        FeatureLayer,
        Graphic,
        Sketch,
        Search,
        ScaleBar,
        geometryEngine,
        Fullscreen,
        locator,
        reactiveUtils,
        Extent,
        BasemapToggle,
        CoordinateConversion,
        Compass,
        Home,
        Viewpoint,
        Basemap,
        WMSLayer,
        WMTSLayer,
        Format,
        Conversion,
        SearchSource,
        Proj4js,
        GT_WGS84,
        GT_OSGB,
        GT_Math,
        maps_html_Helper,
        mapSettings,
        MapsCommon,
        AlertWidget,
        kml) {
        "use strict";
        var mapDrawCompartment = /** @class */ (function () {

            /**
             * This is the consructor it takes in the html to append the map to and set up the items and core map work
             *  @param {any} location the element that the map should be drawn to
             * */
            function mapDrawCompartment(location) {
                Proj4js.defs("EPSG:27700", "+proj=tmerc +lat_0=49 +lon_0=-2 +k=0.999601 +x_0=400000 +y_0=-100000 +ellps=airy +towgs84=446.448,-125.157,542.06,0.15,0.247,0.842,-20.489 +units=m +no_defs");
                Proj4js.defs("EPSG:4326", "+proj=longlat +datum=WGS84 +no_defs");
                //Widgets 
                this.alertWidget;
                this.lastSelection;
                this.deletedGraphics = [];
                this.viewOnClickEvt;
                //Form Fields
                this.submitBtn = document.getElementById("submit");

                config.apiKey = mapSettings.esriApiKey;

                this.defaultMapExtentForEngland = new Extent(mapSettings.englandExtent);
                this.oc_GraphicsArray = [];
                this.activeWidget
                //Load Form
                this.cc_Name = maps_html_Helper.getCurrentCompartmentName();
                this.cc_GIS = maps_html_Helper.getCurrentGeometryJson();
                this.formNearestTownElement = maps_html_Helper.getNearestTown();
                this.formGisDataElement = maps_html_Helper.getCompartmentOfInterest_Element();
                this.formTotalHectaresElement =
                    maps_html_Helper.getCompartmentOfInterest_THectares();

                this.hectareLabel = " ha";
                this.selectedCounter = 0;

                this.drawPolygonButton = document.getElementById("polygon-geometry-button");
                this.toggleCutButton = document.getElementById("cut-geometry-button");
                this.deleteButton = document.getElementById("delete-geometry-button");

                this.isCutting = false;

                this.showLabel = false;
                this.cursorLabel = document.getElementById("hoverLabel");
                this.actionBarExpanded = false;

                //Added below baseMap for England by Forestry/ESRI team
                const basemapUk = new Basemap({
                    portalItem: {
                        id: mapSettings.baseMapForUK
                    },
                });

                this.map = new Map({
                    basemap: basemapUk,
                });

                this.view = new MapView({
                    map: this.map,
                    container: location,
                    extent: new Extent(mapSettings.englandExtent),
                    constraints: {
                        maxZoom: 20
                    }
                });

                this.WireupDefaultOn();

                this._drawingLayer = new GraphicsLayer();
                this.map.add(this._drawingLayer);
                this._esriHelper = new MapsCommon(this.view);

                this.view
                    .when(this.buildGraphics.bind(this))
                    .then(this.createSketchViewModel.bind(this))
                    .then(this.SetUpWidgets.bind(this))
                    .then(this.addCurrentCompartment.bind(this))
                    .then(this.watchMap.bind(this))
                    .then(this.setStartingPoint.bind(this))
                    .then(this.addWatermark.bind(this))
                    .catch(function (e) {
                        if (window.console) {
                            console.error("Failed to create all required parts for view.", e);
                        }
                    });
                var that = this;
                this.view.on("pointer-move", (evt) => {
                    if (!that.cursorLabel | that.showLabel === false) {
                        return;
                    }
                    that.cursorLabel.style.left = (evt.x + 0) + "px";
                    that.cursorLabel.style.top = (evt.y + 0) + "px";

                    // Show the label
                    that.cursorLabel.style.display = "block";
                });
                this.view.on("pointer-leave", () => {
                    if (!that.cursorLabel) {
                        return;
                    }

                    that.cursorLabel.style.display = "none";
                });

                this.view.padding = { left: 49 };
            }

            /**
             * This sets up the draw event of the map. 
             * The event can be turned off as we may need to use the "click" for other things
             **/
            mapDrawCompartment.prototype.WireupDefaultOn = function () {
                const that = this;
                this.viewOnClickEvt = this.view.on("click",
                    (evt) => {
                        that.view.hitTest(evt, { include: that._drawingLayer }).then(function (response) {
                            let currentSelection = [];
                            if (typeof response?.results === "undefined" || response.results.length === 0) {
                                that.sketchViewModel.cancel();
                                that.lastSelection = currentSelection;
                                return;
                            }
                            response.results.forEach((item) => {
                                if (item.graphic.symbol.type !== "text") {
                                    currentSelection.push(item.graphic);
                                }
                            });

                            if (that.sketchViewModel.updateGraphics.length > 1 && currentSelection.length === 1) {
                                that.lastSelection.push(currentSelection[0]);
                            }
                            else {
                                that.sketchViewModel.update(currentSelection[0], { tool: "transform" });
                                that.lastSelection = [currentSelection[0]];
                            }
                        });
                    });
            }

            /**
             * Draws all other woodland compartments shapes to the relevant layers.
             **/
            mapDrawCompartment.prototype.buildGraphics = function () {
                const items = maps_html_Helper.getOtherComparmentJSON();
                const that = this;
                if (typeof items === "undefined" || items === null || items.length === 0) {
                    return;
                }

                items.forEach(function (item) {
                    if (!item.GISData || !item.GISData.spatialReference || !item.GISData.spatialReference.wkid) {
                        that.alertwidget.ShowMessage("", `Invalid object:${JSON.stringify(item)}`);
                        return;
                    }

                    var geometry;

                    if (item.GISData.rings) {
                        geometry = {
                            type: "polygon",
                            rings: item.GISData.rings,
                            spatialReference: item.GISData.spatialReference.wkid
                        };
                    }

                    try {
                        that.oc_GraphicsArray.push(new Graphic({
                            geometry: geometry,
                            attributes: {
                                compartmentName: item.DisplayName
                            }
                        }));
                    }
                    catch (e) {
                        if (window.console) {
                            console.error("Failed to create all required parts for view.", e);
                        }
                    }
                });

                this.CanSave();
                this.buildPolygon_layer(this.oc_GraphicsArray.filter(function (item) {
                    return item.geometry.type === "polygon";
                }));
            };

            /**
             * Imports and sets up all the widgets in the application
             **/
            mapDrawCompartment.prototype.SetUpWidgets = function () {
                var that = this;
                this.alertwidget = new AlertWidget({});

                const fullscreenWidget = new Fullscreen({
                    view: this.view,
                    element: document.getElementById("shell")
                });

                this.homeWidget = new Home({
                    view: this.view
                });
                const compassWidget = new Compass({
                    view: this.view
                });

                that.map.basemap.title = "OS Map";

                var wmsLayer = new WMSLayer(mapSettings.wmsLayer);

                var wmsBasemap = new Basemap({
                    baseLayers: [wmsLayer],
                    title: mapSettings.wmsLayerName,
                    id: "wmsBasemap"
                });


                const basemapToggleWidget = new BasemapToggle({
                    view: this.view,
                    source: [that.map.basemap, wmsBasemap],
                    container: "basemaps-container"
                });




                const scalebarWidget = new ScaleBar({
                    view: this.view,
                    unit: "dual"
                });

                this.drawPolygonButton.addEventListener("click", (evt) => {
                    if (evt.target.active) {
                        that.sketchViewModel.cancel();
                        evt.target.active = false;
                        return;
                    }
                    that.sketchViewModel.cancel();
                    that.showLabel = true;
                    evt.target.active = true;
                    if (that.cursorLabel) {
                        that.cursorLabel.innerHTML = mapSettings.LabelText.drawPoly;
                    }
                    that.sketchViewModel.create("polygon");
                });

                if (this.toggleCutButton) {
                    this.toggleCutButton.addEventListener("click", (evt) => {
                        if (evt.target.active) {
                            that.sketchViewModel.cancel();
                            evt.target.active = false;
                            return;
                        }
                        that.sketchViewModel.cancel();
                        that.isCutting = true;
                        that.showLabel = true;
                        if (that.cursorLabel) {
                            that.cursorLabel.innerHTML = mapSettings.LabelText.cutPoly;
                        }
                        evt.target.active = true;
                        that.sketchViewModel.create("polygon");
                    });
                }

                if (this.deleteButton) {
                    this.deleteButton.addEventListener("click", () => {
                        that.sketchViewModel.delete();
                    });
                }

                const drawHelpButton = document.getElementById("help-geometry-button");
                if (drawHelpButton) {
                    drawHelpButton.addEventListener("click", () => {
                        that.OpenPopUp("/Compartment/help#drawinghelp");
                    });
                }

                const baseMapHelpButton = document.getElementById("help-basemap-button");
                if (baseMapHelpButton) {
                    baseMapHelpButton.addEventListener("click", () => {
                        that.OpenPopUp("/Compartment/help#basemapHelp");
                    });
                }

                const coordinateHelpButton = document.getElementById("help-coordinate-button");
                if (coordinateHelpButton) {
                    coordinateHelpButton.addEventListener("click", () => {
                        that.OpenPopUp("/Compartment/help#coordinateHelp");
                    });
                }


                const drawCloseButton = document.getElementById("close-geometry-button");
                if (drawCloseButton) {
                    drawCloseButton.addEventListener("click", () => {
                        document.querySelector(`[data-action-id=${that.activeWidget}]`).active = false;
                        document.querySelector(`[data-panel-id=${that.activeWidget}]`).hidden = true;
                        that.activeWidget = null;
                    });
                }

                const baseMapCloseButton = document.getElementById("close-basemap-button");
                if (baseMapCloseButton) {
                    baseMapCloseButton.addEventListener("click", () => {
                        document.querySelector(`[data-action-id=${that.activeWidget}]`).active = false;
                        document.querySelector(`[data-panel-id=${that.activeWidget}]`).hidden = true;
                        that.activeWidget = null;
                    });
                }

                const coordinateCloseButton = document.getElementById("close-coordinate-button");
                if (coordinateCloseButton) {
                    coordinateCloseButton.addEventListener("click", () => {
                        document.querySelector(`[data-action-id=${that.activeWidget}]`).active = false;
                        document.querySelector(`[data-panel-id=${that.activeWidget}]`).hidden = true;
                        that.activeWidget = null;
                    });
                }


                const handleActionBarClick = ({ target }) => {
                    if (target.tagName !== "CALCITE-ACTION") {
                        return;
                    }

                    if (that.activeWidget) {
                        document.querySelector(`[data-action-id=${that.activeWidget}]`).active = false;
                        document.querySelector(`[data-panel-id=${that.activeWidget}]`).hidden = true;
                    }

                    const nextWidget = target.dataset.actionId;
                    if (nextWidget !== that.activeWidget) {
                        document.querySelector(`[data-action-id=${nextWidget}]`).active = true;
                        document.querySelector(`[data-panel-id=${nextWidget}]`).hidden = false;
                        document.getElementById("panelMenu").collapsed = false;
                        that.activeWidget = nextWidget;
                    } else {
                        that.activeWidget = null;
                        document.getElementById("panelMenu").collapsed = true;
                    }
                };

                document.querySelector("calcite-action-bar").addEventListener("click", handleActionBarClick);
                document.addEventListener("calciteActionBarToggle", event => {
                    that.actionBarExpanded = !that.actionBarExpanded;
                    that.view.padding = {
                        left: that.actionBarExpanded ? 135 : 49
                    };
                });

                const newFormat = new Format({
                    name: "OS Grid",
                    conversionInfo: {
                        convert: function (point) {
                            let returnString = "Outside Boundary";
                            try {
                                const point27700 = { x: parseFloat(point.x.toFixed()), y: parseFloat(point.y.toFixed()) };

                                const pointWGS84 = Proj4js("EPSG:27700", "EPSG:4326", point27700);


                                let osgb = GT_Math.WGS84_To_OSGB(new GT_WGS84(pointWGS84.y, pointWGS84.x));

                                const tester = osgb.getGridRefForEngland(mapSettings.gridPrecision);
                                if (tester === "SV 00000 00000" | tester.startsWith("undefined")) {
                                    return {
                                        location: returnString,
                                        coordinate: `${returnString}`
                                    };
                                }

                                returnString = tester;
                            }

                            catch (error) {
                                console.error("Error:", error);
                            }
                            return {
                                location: returnString,
                                coordinate: `${returnString}`
                            };
                        }
                    },
                    // Define each segment of the coordinate
                    coordinateSegments: [
                        {
                            alias: "X",
                            description: "Grid Reference"
                        }
                    ]
                });
                const coordinateConversionWidget = new CoordinateConversion({
                    view: that.view,
                    container: "coordinate-container",
                    visibleElements: {
                        settingsButton: false,
                        expandButton: false,
                        editButton: false,
                        captureButton: false,
                    }
                });



                coordinateConversionWidget.formats.add(newFormat);
                coordinateConversionWidget.conversions.splice(
                    0,
                    0,
                    new Conversion({
                        format: newFormat
                    }));

                const osSearch = new SearchSource({
                    name: "OS Grid",
                    placeholder: "SU 11157 89607",
                    autoNavigate: true,
                    getResults: async (params) => {
                        try {
                            var osgb = new GT_OSGB();


                            if (!osgb.parseGridRefForEngland(params.suggestResult.text)) {
                                return;
                            }
                            const ll = GT_Math.OSGB_To_WGS84(osgb);

                            const point27700 = Proj4js("EPSG:4326", "EPSG:27700", { x: ll.Longitude, y: ll.Latitude });

                            const graphic = new Graphic({
                                geometry: {
                                    type: "point",
                                    x: point27700.x,
                                    y: point27700.y,
                                    spatialReference: mapSettings.spatialReference
                                },
                            });

                            const buffer = geometryEngine.buffer(graphic.geometry, 100, "meters");
                            const searchResult = {
                                extent: buffer.extent,
                                feature: graphic,
                                name: params.suggestResult.text
                            };
                            return [searchResult, searchResult];
                        }
                        catch (error) {
                            console.error(error);
                        }
                    }
                });

                const searchWidget = new Search({
                    view: this.view,
                    includeDefaultSources: false,
                    locationEnabled: false,
                    sources: [
                        {
                            url: mapSettings.esriGeoServiceLocatorUrl,
                            countryCode: "GBR",
                            singleLineFieldName: "SingleLine",
                            name: "Search",
                            placeholder: "Enter location to find...",
                            minSuggestCharacters: 3
                        },
                        osSearch
                    ]
                });
                this.view.ui.add(searchWidget, "top-right");
                this.view.ui.move("zoom", "top-right");
                this.view.ui.add(this.homeWidget, "top-right");
                this.view.ui.add(compassWidget, "top-right");
                this.view.ui.add(fullscreenWidget, "top-right");
                this.view.ui.add(this.alertwidget, "top-right");
                this.view.ui.add(scalebarWidget, "bottom-left");
                return;
            };

            /**
             * Opens a pop up window at the path
             *  @param {string} value Stringfied GIS JSON object
             **/
            mapDrawCompartment.prototype.OpenPopUp = function (path) {
                const left = (window.screen.width - mapSettings.popup.width) / 2;
                const top = (window.screen.height - mapSettings.popup.height) / 2;
                let settings = `width=${mapSettings.popup.width}, height=${mapSettings.popup.height}, top=${top}, left=${left}`;

                // The URL you want to open in the popup (replace with your desired URL)
                const popupURL = `${window.location.origin}${path}`;

                // Open the popup window
                const popup = window.open(popupURL, 'Popup Window', settings);

                // Check if the popup was blocked
                if (!popup || popup.closed || typeof popup.closed === 'undefined') {
                    that.alertwidget.ShowMessage("info", "The popup window was blocked by the browser. Please allow pop-ups for this website.");
                }
            };
            /**
             * Sets the GIS data on the form
             *  @param {string} value Stringfied GIS JSON object
             **/
            mapDrawCompartment.prototype.setFormGisData = function (value) {
                if (this.formGisDataElement) {
                    this.formGisDataElement.value = value;
                }
            };

            /**
             * Sets the total size on the form
             * @param {string} value The Size of the shape
             **/
            mapDrawCompartment.prototype.setFormTotalHectaresData = function (value) {
                this.formTotalHectaresElement.value = value;
            };

            /**
             * Clears all the form settings
             **/
            mapDrawCompartment.prototype.clearCompartmentFormFields = function () {
                this.setFormGisData("");
                this.setFormTotalHectaresData("");
            };

            /**
             * Wires up events that the application should look out for 
             **/
            mapDrawCompartment.prototype.watchMap = function () {
                const that = this;
                reactiveUtils.once(function () { return that.view.stationary === true; }, (function () {
                    const currentCenter = that.view.extent.center;
                    if (!that.defaultMapExtentForEngland.contains(currentCenter)) {
                        that.view.goTo(that.defaultMapExtentForEngland.center);
                    }
                }));
            };

            /**
 * Adds the watermark layer to the system
 */
            mapDrawCompartment.prototype.addWatermark = function () {
                // Create a new GraphicsLayer for the watermark
                const watermarkLayer = new GraphicsLayer({
                    visible: false,
                    id: "watermarkLayer",
                });

                // Define the text symbol for the watermark
                const textSymbol = mapSettings.BlueSkyTextSymbol;
                ;

                // Function to update the watermark positions
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
                    if (!newBasemap.portalItem || newBasemap.portalItem.id !== mapSettings.baseMapForUK) {
                        watermarkLayer.visible = true;
                    } else {
                        watermarkLayer.visible = false;
                    }
                });
            };


            /**
             * Draws all other compartment polygons to the "Polygon Layer"
             * @param {Geometry[]} polygons The geometry of the polygons 
             **/
            mapDrawCompartment.prototype.buildPolygon_layer = function (polygons) {
                try {
                    if (typeof polygons === "undefined" || polygons === null || polygons.length === 0) {
                        return;
                    }
                    let labelSymbol = JSON.parse(JSON.stringify(mapSettings.activeTextSymbol));
                    console.log(mapSettings.VisualVariables)
                    this.ocLayer_Polygon = new FeatureLayer({
                        source: polygons,
                        geometryType: "polygon",
                        objectIdField: "ObjectID",
                        fields: [
                            { name: "ObjectID", type: "oid" },
                            { name: "compartmentName", type: "string" },
                        ],
                        labelingInfo: {
                            labelExpressionInfo: {
                                expression: "$feature.compartmentName ",
                            },
                            symbol: labelSymbol,
                            visualVariables: mapSettings.visualVariables.textSize
                        },
                        renderer: {
                            type: "simple",
                            symbol: mapSettings.otherPolygonSymbol,
                            visualVariables: mapSettings.visualVariables.size
                        }
                    });
                    this.map.add(this.ocLayer_Polygon);
                }
                catch (e) {
                    if (window.console) {
                        console.error("Failed to create all required parts for view.", e);
                    }
                }
            };

            /**
             * Draws the current comparment to the map
             **/
            mapDrawCompartment.prototype.addCurrentCompartment = function () {
                var currentShape = this.createWorkingComparment();
                if (currentShape !== undefined && currentShape !== null) {
                    this._drawingLayer.add(currentShape.shape);
                    this._drawingLayer.add(currentShape.label);

                    //centre map view on the compartment of interest
                    if (currentShape.shape.geometry.type === "point") {
                        var holder = new Graphic({
                            geometry: {
                                type: "polygon",
                                rings: [
                                    [
                                        currentShape.shape.geometry.x,
                                        currentShape.shape.geometry.y,
                                    ],
                                    [
                                        currentShape.shape.geometry.X,
                                        currentShape.shape.geometry.y + 1,
                                    ],
                                    [
                                        currentShape.shape.geometry.x + 1,
                                        currentShape.shape.geometry.y + 1,
                                    ],
                                    [
                                        currentShape.shape.geometry.x,
                                        currentShape.shape.geometry.y,
                                    ],
                                ],
                                spatialReference: mapSettings.spatialReference,
                            },
                            symbol: currentShape.shape.symbol,
                        });
                        this.setHomeViewPoint(holder.geometry.extent);
                    }
                    else {
                        this.setHomeViewPoint(currentShape.shape.geometry.extent);
                    }
                }

                this.UpdateSketchTools(true);
                this.CanSave();
            };

            /**
             * Sets the starting point for the mapping application 
             **/
            mapDrawCompartment.prototype.setStartingPoint = async function () {
                const viewPoint = new Viewpoint();

                if (this._drawingLayer.graphics.items.length > 0) {
                    const topItem = this._drawingLayer.graphics.items[0];
                    if (topItem.geometry.type === 'point') {
                        this.view.goTo({
                            target: topItem.geometry,
                            zoom: 20
                        });
                        viewPoint.targetGeometry = topItem.geometry;
                        this.homeWidget.viewpoint = viewPoint;
                    } else {
                        this.view.goTo({
                            target: topItem.geometry.extent
                        });
                        this.setHomeViewPoint(topItem.geometry.extent);
                    }
                } else if (typeof this.oc_GraphicsArray === "undefined" ||
                    this.oc_GraphicsArray.length === 0) {
                    var nearestTown = maps_html_Helper.getNearestTown();
                    if (nearestTown) {
                        var result = await this._esriHelper.locateTown(nearestTown, locator, mapSettings.esriGeoServiceLocatorUrl);
                        if (result[0] != undefined) {
                            this.view.goTo({
                                center: [result[0].location.x, result[0].location.y],
                                zoom: 15
                            });
                            if (this.homeWidget && viewPoint) {
                                viewPoint.targetGeometry = result[0].extent;
                                this.homeWidget.viewpoint = viewPoint;
                            }
                        }
                        else {
                            this.alertwidget.ShowMessage("", `Unable to find town "${nearestTown}"`);
                            //Default the zoom level to 6, as GBR was set as country, if the place is invalid
                            this.view.goTo({
                                zoom: 6
                            });
                        }
                    }
                    else {
                        this.alertwidget.ShowMessage("info", "Nearest town wasn't set");
                        this.view.goTo({
                            target: this.defaultMapExtentForEngland.center,
                            zoom: 8
                        });
                        this.setHomeViewPoint(this.defaultMapExtentForEngland.extent);
                    }
                }
                else {
                    const items = this.oc_GraphicsArray.map((g) => { return g.geometry; });
                    this.view.goTo({ target: items });
                }
            };

            /**
             * Sets the Viewpoint extent
             * @param {any} extent The Extent of the most logical starting point
             **/
            mapDrawCompartment.prototype.setHomeViewPoint = function (extent) {
                const vp = new Viewpoint({
                    targetGeometry: extent
                });

                //Setting the Home view for Home Button. This will trigger when clicked
                if (this.homeWidget) {
                    this.homeWidget.viewpoint = vp;
                }
            };

            /**
             * Sets up the sketchViewModel of the mapping tool
             **/
            mapDrawCompartment.prototype.createSketchViewModel = function () {

                this.sketchViewModel = new Sketch({
                    layer: this._drawingLayer,
                    view: this.view,
                    availableCreateTools: [
                        "polygon"
                    ],
                    tool: "move",
                    creationMode: "single",
                    defaultCreateOptions: {
                        mode: "hybrid"
                    },
                    visibleElements: {
                        selectionTools: {
                            "lasso-selection": false,
                            "rectangle-selection": true,
                        },
                        snappingControlsElements: {
                            featureEnabledToggle: true,
                            layerList: true,
                        },
                        settingsMenu: false,
                        undoRedoMenu: false,
                    },
                });

                this.sketchViewModel.updateOnGraphicClick = false;

                var sources = [];
                if (this.ocLayer_Polygon) {
                    sources.push({
                        layer: this.ocLayer_Polygon,
                        enabled: true,
                    });
                }
                this.sketchViewModel.snappingOptions = {
                    enabled: true,
                    distance: 5,
                    featureEnabled: true,
                    featureSources: sources,
                };

                var that = this;
                this.sketchViewModel.on("delete", function (evt) {
                    that.deleteGraphics(MapsCommon.getGraphicsFromEvt(evt));
                    that.UpdateSketchTools(true);
                    that.CanSave();
                });
                this.sketchViewModel.on("create", function (evt) {

                    const graphics = MapsCommon.getGraphicsFromEvt(evt);

                    that._drawingLayer.removeMany(that._drawingLayer.graphics.items.filter((i) => {
                        return i.attributes !== null && i.attributes.isTemp === true && i.symbol.type === "text"
                    }));
                    if (evt.state === "start") {
                        that.CanSave(false);
                    }
                    if (evt.state === "complete" || evt.state === "create") {
                        if (that.isCutting === false) {
                            that.onGraphicUpdate(evt);
                            if (evt.state === "complete") {
                                that.isInEngland(evt);
                            }
                        }
                    }
                    if (evt.state === "cancel") {
                        that.UnsetActiveButtons([
                            that.drawPolygonButton,
                            that.toggleCutButton]);
                        that.showLabel = false;
                        that.cursorLabel.style.display = "none";
                        that.deleteButton.disabled = true;
                    }
                    if (evt.state === "complete") {
                        that.showLabel = false;
                        that.cursorLabel.style.display = "none";
                        that.deleteButton.disabled = true;
                        that.UnsetActiveButtons([
                            that.drawPolygonButton,
                            that.toggleCutButton]);
                        if (that.isCutting) {
                            const list = that._drawingLayer.graphics.items.filter(i => i.geometry.type === 'polygon');
                            var result = geometryEngine.difference(list[0].geometry, evt.graphic.geometry);
                            that.deleteGraphics(that._drawingLayer.graphics);
                            const currentShape = that.createWorkingComparment(result);
                            if (currentShape !== undefined && currentShape !== null) {
                                that._drawingLayer.add(currentShape.shape);
                                currentShape.label.text = that.cc_Name + "\n " + that.getSizeOfShape(currentShape.shape).toFixed(2) + this.hectareLabel;
                                that._drawingLayer.add(currentShape.label);
                            }
                            that.updateFormGisData();
                            that.UpdateSketchTools(true);
                            that.CanSave();

                        }

                        that.isCutting = false;
                        that.UpdateSketchTools(true);
                    }
                    else {
                        if (that.isCutting) {
                            return;
                        }
                        if (typeof graphics === "undefined" || graphics.length === 0) {
                            return;
                        }
                        that._drawingLayer.add(that.getLabelGraphic(graphics[0]));

                    }
                    that.UpdateSketchTools((evt.state === "complete" | evt.state === "cancel"));

                });

                this.sketchViewModel.on("update", function (evt) {
                    that.deleteButton.disabled = false;
                    if (evt.state === "start") {
                        that.CanSave(false);
                    }
                    that.UpdateSketchTools((evt.state === "complete" | evt.state === "cancel"));
                    // check if a graphic is being selected or deselected.
                    if (evt.tool === "transform" || evt.tool === "reshape") {
                        if (evt.state === "start" || evt.state === "active") {
                            var graphics = (MapsCommon.getGraphicsFromEvt(evt)).filter((item) => {
                                return item.symbol.type !== "text";
                            });

                            that.CanSave();
                            that.onGraphicUpdate(evt);
                        }
                        if (evt.state === "complete" | evt.state === "cancel") {
                            that.isInEngland(evt);
                        }
                    }
                    if (evt.state === "complete" | evt.state === "cancel") {
                        that.deleteButton.disabled = true;
                    }
                });
            };

            mapDrawCompartment.prototype.toggleButtons = function (buttons, state) {

                buttons.forEach((b) => {
                    b.disabled = !state;
                });
            }

            mapDrawCompartment.prototype.UnsetActiveButtons = function (buttons) {

                buttons.forEach((b) => {
                    b.active = false;
                });
            }

            /**
             * Handling logical state changes in the IO
             * @param {boolean} unlock If the setchwidget just needs to be locked, without calculating state
             */
            mapDrawCompartment.prototype.UpdateSketchTools = function (unlock) {
                if (unlock === false) {
                    this.toggleButtons([
                        this.drawPolygonButton,
                        this.toggleCutButton], false);
                }
                else {
                    const workingItems = this._drawingLayer.graphics.items.filter((item) => {
                        return item.symbol.type !== "text";
                    });

                    if (workingItems.length === 0) {

                        this.toggleButtons([
                            this.drawPolygonButton], true);
                        this.toggleButtons([
                            this.toggleCutButton], false);
                    }
                    else {
                        switch (workingItems[0].geometry.type) {
                            default:
                                if (workingItems.length > 0) {
                                    this.toggleButtons([
                                        this.drawPolygonButton], false);
                                    this.toggleButtons([
                                        this.toggleCutButton], true);
                                } else {
                                    this.toggleButtons([
                                        this.drawPolygonButton], true);
                                    this.toggleButtons([
                                        this.toggleCutButton], false);
                                }
                                break;
                        }
                    }

                }
            }


            /**
             * Deletes all the given graphics from the drawing layer
             * @param {any} graphics The Graphics to delete
             */
            mapDrawCompartment.prototype.deleteGraphics = function (graphics) {
                var _this = this;
                var list = [];
                graphics.forEach(function (graphic) {
                    _this.deletedGraphics.push(graphic.uid);
                    _this._drawingLayer.graphics.items.every(function (label) {
                        if (!label.attributes || !label.attributes.shapeID) {
                            // Not a label
                        }
                        else if (label.attributes.shapeID === graphic.uid) {
                            list.push(label);
                        }
                        return true;
                    });
                });

                this._drawingLayer.removeMany(graphics.concat(list));
                this.updateFormGisData();
                this.CanSave();
            };

            /**
             * Checks if the current state of the graphic is valid.
             * @param {any} graphics THe Graphics from the evt to check
             */
            mapDrawCompartment.prototype.checkIsValid = function (graphics) {
                const that = this;
                let isValid = true;
                graphics
                    .filter(function (g) {
                        return !g.symbol || !g.symbol.type || g.symbol.type !== "text";
                    })
                    .every(function (g) {
                        const shape = g.geometry.type;
                        if (shape === "polygon") {
                            if (g.geometry.isSelfIntersecting) {
                                isValid = false;
                                return false;
                            }
                            if (!geometryEngine.contains(that.defaultMapExtentForEngland, g.geometry)) {
                                isValid = false;
                                return false;
                            }
                            if (typeof that.oc_GraphicsArray !== "undefined") {
                                if (that.oc_GraphicsArray.length > 0) {
                                    that.oc_GraphicsArray.every(function (ocGraphic) {
                                        if (ocGraphic.geometry.type === "polygon") {
                                            const overlaps = geometryEngine.overlaps(ocGraphic.geometry, g.geometry); // returns true if one geometry overlaps another, but is not contained or disjointed
                                            const contained = geometryEngine.contains(ocGraphic.geometry, g.geometry);
                                            const within = geometryEngine.within(ocGraphic.geometry, g.geometry);
                                            if (overlaps || contained || within) {
                                                isValid = false;
                                                return false;
                                            }
                                        }
                                        return true;
                                    });
                                }
                            }
                        }
                        return true;
                    });
                return isValid;
            }

            /**
             * Handles the Update event of the Graphic selected
             * @param {any} evt The event that is triggering the Update
             * @param {any} state Overrides the isValid check
             */
            mapDrawCompartment.prototype.onGraphicUpdate = function (evt, state) {
                var _this = this;
                var graphics = MapsCommon.getGraphicsFromEvt(evt);
                if (typeof graphics === "undefined" || graphics.length === 0) {
                    return;
                }
                graphics.every(function (g) {
                    if (!g.attributes) {
                        g.attributes = {
                            name: _this.cc_Name,
                            isValid: true
                        };
                    }
                });
                // lets test
                var isValid;
                if (typeof state === "undefined") {
                    isValid = this.checkIsValid(graphics);
                } else {
                    isValid = state;
                }

                //Finally lets apply all fields
                this._drawingLayer.graphics.items.forEach(function (g) {
                    g.attributes.isValid = isValid;
                    if (g.geometry.type === "polygon") {
                        g.symbol = g.attributes.isValid
                            ? mapSettings.activePolygonSymbol
                            : mapSettings.invalidPolygonSymbol;

                    }
                });
                if (evt.toolEventInfo &&
                    (evt.toolEventInfo.type === "move-stop" ||
                        evt.toolEventInfo.type === "reshape-stop")) {
                    if (isValid) {
                        if (this.sketchViewModel) {
                            this.sketchViewModel.complete();
                        }
                        this.updateFormGisData();
                    }
                    else {
                        this.clearCompartmentFormFields();
                    }
                }
                else if (evt.state === "complete") {
                    if (isValid) {
                        this.updateFormGisData();
                    }
                    else {
                        var graphic = graphics[0];
                        if (this.sketchViewModel) {
                            this.sketchViewModel.update([graphic], { tool: "reshape" });
                        }
                        this.clearCompartmentFormFields();
                    }
                    this.CanSave();
                }
                if (!this.isCutting) {
                    this.UpdateLabels(graphics);
                }
            };

            mapDrawCompartment.prototype.isInEngland = function (evt) {
                var that = this;
                let graphics = MapsCommon.getGraphicsFromEvt(evt);
                if (typeof graphics === "undefined" || graphics.length === 0) {
                    return;
                }
                if (graphics[0].geometry.type !== "polygon") {
                    return;
                }

                if (graphics[0].attributes.isValid === false) {
                    return;
                }


                fetch("/api/GIS/IsInEngland",
                    {
                        credentials: mapSettings.requestParamsAPI.credentials,
                        method: "POST",
                        headers: {
                            'Accept': "application/json",
                            "Content-type": "application/json"
                        },
                        body: JSON.stringify({
                            Name: "",
                            ShapeType: graphics[0].geometry.type,
                            ShapeDetails: JSON.stringify(graphics[0].geometry.toJSON())
                        })
                    })
                    .then(function (response) {
                        if (!response.ok) {
                            throw new Error(`Failed with HTTP code ${response.status}`);
                        }
                        return response.json();
                    }).then((json) => {
                        if (graphics.length > 0) {
                            // Check the shape is still on the layer
                            if (that._drawingLayer.graphics.map((g) => { return g.uid }).indexOf(graphics[0].uid) === -1) {
                                return;
                            }
                        }

                        if (!json.isSuccess) {
                            that.CanSave(false);
                            that.alertwidget.ShowMessage("", "Unable to validate the shape");
                            return;
                        }

                        if (!json.result) {
                            that.CanSave(false);
                            that.onGraphicUpdate(evt, false);
                            that.alertwidget.ShowMessage("", "The shape falls outside of England. Please move or redraw");
                            return;
                        }
                        that.onGraphicUpdate(evt);
                        that.alertwidget.CloseMessage();
                        that.CanSave();
                    }).catch((e) => {
                        that.CanSave(false);
                        that.onGraphicUpdate(evt, false);
                        that.alertwidget.ShowMessage("", "Unable to validate the shape");
                    });

                return true;
            }
            mapDrawCompartment.prototype.calculatePolygonArea = function (polygon) {
                var area = geometryEngine.planarArea(polygon, "square-meters");

                if (area < 0) {
                    const counterPoly = new Polygon({
                        rings: clockwisePolygon.rings.map(function (ring) {
                            return ring.reverse();
                        })
                    });
                    area = geometryEngine.planarArea(counterPoly, "square-meters");
                }
                return area;
            }


            /**
             * Gets the size of the shape.
             */
            mapDrawCompartment.prototype.getSizeOfShape = function (workingGraphic) {
                return geometryEngine.planarArea(workingGraphic.geometry, "hectares");
            }


            /**
             * Updates the GIS data on the form
             */
            mapDrawCompartment.prototype.updateFormGisData = function () {
                // Makes more sense as a Webservice
                var that = this;

                var area = 0;
                var shape = "";
                var workingGraphics = this._drawingLayer.graphics.items.filter(function (g) {
                    return g.symbol.type !== "text";
                });
                if (Array.isArray(workingGraphics) && workingGraphics.length) {
                    if (workingGraphics.length === 1) {
                        area = area + this.getSizeOfShape(workingGraphics[0]);
                        shape = workingGraphics[0].geometry.toJSON();
                    }
                }

                this.setFormTotalHectaresData(area);
                if (shape !== "") {
                    this.setFormGisData(JSON.stringify(shape));
                }
                else {
                    this.setFormGisData("");
                }
            };

            /**
             * Handles the update for the text labels
             * @param {any} graphics: the labels to update
             */
            mapDrawCompartment.prototype.UpdateLabels = function (graphics) {
                var _this = this;
                if (this._drawingLayer.graphics.items.length > 1) {
                    graphics.forEach(function (graphic) {
                        var compLabelIndex = _this._drawingLayer.graphics.items.findIndex(function (l) {
                            return (l.symbol.type === "text" &&
                                l.attributes.shapeID === graphic.uid);
                        });
                        var compLabel = _this._drawingLayer.graphics.items[compLabelIndex];
                        _this._drawingLayer.remove(compLabel); //orig comp text label graphic
                        if (_this.deletedGraphics.indexOf(graphic.uid) === -1) {
                            _this._drawingLayer.add(_this.getLabelGraphic(graphic));
                        }
                    });
                }
                else {
                    if (_this.deletedGraphics.indexOf(graphics[0].uid) === -1) {
                        _this._drawingLayer.add(_this.getLabelGraphic(graphics[0]));
                    }
                }
            };

            /**
             * Gets the label Graphic to add to the map
             * @param {any} shapeGraphic The Shape to add the label to.
             */
            mapDrawCompartment.prototype.getLabelGraphic = function (shapeGraphic) {
                var resx;
                var labelSymbol = JSON.parse(JSON.stringify(mapSettings.activeTextSymbol));
                var line = "";
                if (shapeGraphic.attributes === null) {
                    if (shapeGraphic.geometry.type === "polygon") {
                        labelSymbol.text = parseFloat(this.getSizeOfShape(shapeGraphic)).toFixed(2) + this.hectareLabel;
                    }
                }
                else if (!shapeGraphic.attributes.SelectOrder) {
                    if (shapeGraphic.geometry.type === "polygon") {
                        var size = parseFloat(this.getSizeOfShape(shapeGraphic));

                        if (!isNaN(size))
                            line = "\n " + size.toFixed(2) + this.hectareLabel;

                        labelSymbol.text = shapeGraphic.attributes.name + line;
                    }
                    else {
                        labelSymbol.text = shapeGraphic.attributes.name;
                    }
                }
                else {
                    if (shapeGraphic.geometry.type === "polygon") {
                        var size = parseFloat(this.getSizeOfShape(shapeGraphic));
                        if (!isNaN(size))
                            line = "\n " + size.toFixed(2) + this.hectareLabel;

                        labelSymbol.text =
                            shapeGraphic.attributes.name
                            + line;
                    }
                    else {
                        labelSymbol.text =
                            shapeGraphic.attributes.name;
                    }

                }

                resx = new Graphic({
                    geometry: shapeGraphic.geometry.centroid,
                    symbol: labelSymbol,
                });

                if (!resx.attributes) {
                    resx.attributes = {
                        shapeID: shapeGraphic.uid,
                        isTemp: shapeGraphic.attributes === null
                    };
                }
                return resx;
            };


            /**
             * Works out if the Shape can be saved to the server.
             * @param {any} param
             */
            mapDrawCompartment.prototype.CanSave = function (param) {
                if (typeof param === "undefined") {
                    param = true;
                }
                if (param === false | !this.formGisDataElement.value | this.formGisDataElement.value === "") {
                    this.submitBtn.disabled = true;
                    return;
                }

                const workingItems = this._drawingLayer.graphics.items.filter((item) => {
                    return item.symbol.type !== "text";
                });

                if (workingItems.length === 0) {
                    this.submitBtn.enable = true;
                }
                else {
                    this.submitBtn.disabled = !(workingItems.length > 0 & workingItems.length < 2);
                }
            }

            /**
             * Creates a working compartment (Label and shape)
             * @param {any} graphic The graphic to render
             */
            mapDrawCompartment.prototype.createWorkingComparment = function (graphic) {
                if (graphic === void 0) { graphic = null; }
                var shape;
                if (!graphic) {
                    if (!this.cc_GIS) {
                        return;
                    }
                    //Whos that shape?
                    if (this.cc_GIS.rings) {
                        //Its a Polygon
                        shape = new Graphic({
                            geometry: {
                                type: "polygon",
                                rings: this.cc_GIS.rings,
                                spatialReference: this.cc_GIS.spatialReference.wkid,
                            },
                            symbol: mapSettings.activePolygonSymbol,
                        });
                    }
                }
                else {
                    shape = new Graphic({
                        geometry: graphic,
                        symbol: mapSettings.activePolygonSymbol,
                    });
                }
                shape.attributes = {
                    name: this.cc_Name,
                    isValid: true
                };
                const labelGraphic = this.getLabelGraphic(shape);
                return {
                    shape: shape,
                    label: labelGraphic,
                };
            };
            return mapDrawCompartment;
        }());
        return mapDrawCompartment;
    });