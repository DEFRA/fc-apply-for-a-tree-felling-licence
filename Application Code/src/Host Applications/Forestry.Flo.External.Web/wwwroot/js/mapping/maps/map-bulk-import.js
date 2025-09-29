define(["require",
    "exports",
    "esri/config",
    "esri/Map",
    "esri/views/MapView",
    "esri/Basemap",
    "esri/layers/WMSLayer",
    "esri/layers/GraphicsLayer",
    "esri/layers/support/Field",
    "esri/Graphic",
    "esri/rest/locator",
    "esri/layers/FeatureLayer",
    "esri/geometry/Extent",
    "esri/widgets/BasemapGallery",
    "esri/widgets/CoordinateConversion",
    "esri/widgets/Compass",
    "esri/widgets/ScaleBar",
    "esri/widgets/Fullscreen",
    "esri/geometry/geometryEngine",
    "esri/layers/GraphicsLayer",
    "esri/widgets/CoordinateConversion/support/Format",
    "esri/widgets/CoordinateConversion/support/Conversion",
    "/js/mapping/gthelper/proj4.js?v=" + Date.now(),
    "/js/mapping/gthelper/gt-wgs84.js?v=" + Date.now(),
    "/js/mapping/gthelper/gt-math.js?v=" + Date.now(),
    "/js/mapping/maps-common.js?v=" + Date.now(),
    "/js/mapping/mapSettings.js?v=" + Date.now(),
    "/js/mapping/widgets/alert-widget.js?v=" + Date.now(),
    "/js/mapping/KMLConvertor.js?v=" + Date.now(),
"/js/JZip.js"],
    function (require,
        exports,
        config,
        Map,
        MapView,
        Basemap,
        WMSLayer,
        GraphicsLayer,
        Field,
        Graphic,
        locator,
        FeatureLayer,
        Extent,
        BasemapToggle,
        CoordinateConversion,
        Compass, ScaleBar,
        Fullscreen,
        geometryEngine,
        GraphicsLayer,
        Format,
        Conversion,
        Proj4js,
        GT_WGS84,
        GT_Math,
        MapsCommon, mapSettings,
        AlertWidget,
        kml,
        JSZip    ) {
        var bulkImportMap = /** @class */ (function () {

            /**
             * This is the constructor it takes in the html to append the map to and set up the items and core map work
             *  @param {any} location the element that the map should be drawn to
             * */
            function bulkImportMap(location) {
                Proj4js.defs("EPSG:27700", "+proj=tmerc +lat_0=49 +lon_0=-2 +k=0.999601 +x_0=400000 +y_0=-100000 +ellps=airy +towgs84=446.448,-125.157,542.06,0.15,0.247,0.842,-20.489 +units=m +no_defs");
                Proj4js.defs("EPSG:4326", "+proj=longlat +datum=WGS84 +no_defs");
                this.viewOnClickEvt = null;
                this.compartmentName = 1;
                this.compartmentSub = 'A';
                this.supportedFileTypes = [];
                this.maxFileSize = 30;
                this.currentHighlight = null;
                this.submitBtn = document.getElementById("submit");

                this.importControl = document.getElementById("importFileControl");

                if (this.importControl) {
                    const that = this;
                    this.importControl.addEventListener("change", (e) => {
                        document.querySelector('[data-panel-id="importer"]').setAttribute("loading", "");
                        if (e.target.files.length === 0) {
                            return;
                        }

                        if (e.target.files[0].size > that.maxFileSize) {
                            that.alertWidget.ShowMessage("error", "The file you atemptting to upload is to big");
                            return;
                        }

                        const filePath = e.target.value;
                        if (typeof filePath === "undefined" || filePath.length === 0) {
                            that.alertWidget.ShowMessage("error", "Unable to find file");
                            return;
                        }

                        const fileExt = filePath.slice(filePath.lastIndexOf('.'));
                        if (!this.supportedFileTypes.includes(fileExt)) {
                            that.alertWidget.ShowMessage("error", "File type is not supported");
                            return;
                        }
                        if (fileExt.toLocaleLowerCase() === ".kml" ) {
                            var fileReader = new FileReader();
                            fileReader.readAsText(e.target.files[0]);
                            fileReader.onload = function () {
                                var dom = new DOMParser().parseFromString(
                                    fileReader.result,
                                    "text/xml"
                                );
                                fetch(window.origin + "/api/Gis/GetShapesFromString", {
                                    method: "POST",
                                    body: that.getStringFormData(
                                        kml.kml(dom),
                                        that.getfileNamePartsArray(filePath)
                                    ),
                                }).then((r) => {
                                    if (r.status != 200) {
                                        return r
                                            .text()
                                            .then((data) => ({ status: r.status, body: data }));
                                    } else {
                                        return r
                                            .json()
                                            .then((data) => ({ status: r.status, body: data }));
                                    }
                                })
                                    .then((obj) => {
                                        if (obj.status !== 200) {
                                            that.alertWidget.ShowMessage("error", "Unable to load shape data");
                                            return;
                                        }
                                        that.processResult(obj.body);
                                    })
                            }
                            return;
                        }
                        else if (fileExt.toLocaleLowerCase() === ".kmz") {
                            const fileReader = new FileReader();
                            fileReader.readAsArrayBuffer(e.target.files[0]);
                            fileReader.onload = async function () {
                                try {
                                    const jszip = new JSZip();
                                    const zip = await jszip.loadAsync(fileReader.result); 
                                    const kmlFile = Object.keys(zip.files).find((fileName) => fileName.endsWith(".kml"));

                                    if (!kmlFile) {
                                        that.alertWidget.ShowMessage("error", "No KML file found in the KMZ archive.");
                                        return;
                                    }

                                    const kmlContent = await zip.files[kmlFile].async("text");
                                    const dom = new DOMParser().parseFromString(kmlContent, "text/xml");

                                    // Process the KML content as you already do
                                    fetch(window.origin + "/api/Gis/GetShapesFromString", {
                                        method: "POST",
                                        body: that.getStringFormData(
                                            kml.kml(dom),
                                            that.getfileNamePartsArray(filePath)
                                        ),
                                    })
                                        .then((r) => {
                                            if (r.status != 200) {
                                                return r.text().then((data) => ({ status: r.status, body: data }));
                                            } else {
                                                return r.json().then((data) => ({ status: r.status, body: data }));
                                            }
                                        })
                                        .then((obj) => {
                                            if (obj.status !== 200) {
                                                that.alertWidget.ShowMessage("error", "Unable to load shape data");
                                                return;
                                            }
                                            that.processResult(obj.body);
                                        });
                                } catch (error) {
                                    console.error("Error processing KMZ file:", error);
                                    that.alertWidget.ShowMessage("error", "Failed to process KMZ file.");
                                }
                            };
                            return;
                        }
                        else if (fileExt.toLocaleLowerCase() === ".geojson" || fileExt.toLocaleLowerCase() === ".json") {
                            var fileReader = new FileReader();
                            fileReader.readAsText(e.target.files[0]);
                            fileReader.onload = function () {

                                fetch(window.origin + "/api/Gis/GetShapesFromString", {
                                    method: "POST",
                                    body: that.getStringFormData(JSON.parse(fileReader.result), that.getfileNamePartsArray(filePath)),
                                }).then((r) => {
                                    if (r.status != 200) {
                                        return r
                                            .text()
                                            .then((data) => ({ status: r.status, body: data }));
                                    } else {
                                        return r
                                            .json()
                                            .then((data) => ({ status: r.status, body: data }));
                                    }
                                })
                                    .then((obj) => {
                                        if (obj.status !== 200) {
                                            that.alertWidget.ShowMessage("error", "Unable to load shape data");
                                            return;
                                        }
                                        that.processResult(obj.body);
                                    })
                            }
                            return;
                        }

                        fetch(window.origin + "/api/Gis/GetShapes", {
                            method: "POST",
                            body: that.getFormData(that.getfileNamePartsArray(filePath), e.target.files[0]),
                        })
                            .then((r) => {
                                if (r.status != 200) {
                                    return r.text().then((data) => ({ status: r.status, body: data }));
                                } else {
                                    return r.json().then((data) => ({ status: r.status, body: data }));
                                }
                            })
                            .then((obj) => {
                                if (obj.status !== 200) {
                                    return;
                                }

                                that.processResult(obj.body);
                            });
                    });
                }

                this.fieldControl = document.getElementById("group");

                config.apiKey = mapSettings.esriApiKey;


                const baseMap = new Basemap({
                    portalItem: {
                        id: mapSettings.baseMapForUK
                    }
                });

                this.map = new Map({
                    basemap: baseMap
                });

                this.view = new MapView({
                    map: this.map,
                    container: location,
                    extent: mapSettings.englandExtent,
                    constraints: {
                        maxZoom: 20
                    }
                });


                this.esriHelper = new MapsCommon(this.view);
                this.defaultMapExtentForEngland = new Extent(mapSettings.englandExtent);

                this._drawingLayer = new GraphicsLayer();
                this.map.add(this._drawingLayer);
                this.addSelectionWatcher()
                this.view.when(this.loadData.bind(this))
                    .then(this.buildGraphics.bind(this))
                    .then(this.setUpWidgets.bind(this))
                    .then(this.addWatermark.bind(this))
                    .then(this.Goto.bind(this));

                this.view.padding = { left: 49 };

            };

            /**
             * Adds the watermark layer to the system
             */
            bulkImportMap.prototype.addWatermark = function () {
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
             * Sets up the widgets to be used on the map.
             * */
            bulkImportMap.prototype.setUpWidgets = async function () {
                const that = this;

                this.submitBtn.addEventListener("click", () => {
                    that.Save();
                });

                this.view.on("click", (e) => {
                    that.view.hitTest(e).then(async (r) => {
                        if (!r.results || r.results.length === 0) {
                            return;
                        }

                        const results = r.results.filter((i) => i.graphic && i.graphic.attributes && i.graphic.attributes.ImportKey);

                        if (results.length === 0) {
                            return;
                        }
                        const graphic = results[0].graphic;

                        const intersects = await this.checkIntersectionsWithFeatureLayer(graphic.geometry);

                        if (intersects) {
                            that.alertWidget.ShowMessage("error", "This shape intersects with another shape in the map.");
                            checkbox.checked = false;
                            return;
                        }

                        const isInDrawingLayer = that._drawingLayer.graphics.items.includes(graphic);
                        if (isInDrawingLayer && graphic.attributes) {
                            graphic.attributes.isSelected = !graphic.attributes.isSelected;

                            graphic.symbol = graphic.attributes.isSelected
                                ? mapSettings.importShapeSelected
                                : mapSettings.importShape;

                            const checkbox = document.querySelector(
                                `input[data-ImportKey="${graphic.attributes.ImportKey}"]`
                            );
                            if (checkbox) {
                                checkbox.checked = graphic.attributes.isSelected;
                            }

                            const allCheckbox = document.getElementById("all").querySelector("input[type='checkbox']");
                            const childCheckboxes = document.getElementById("Main-Selection").querySelectorAll("input[type='checkbox']");
                            const areChecked = Array.from(childCheckboxes).every(cb => cb.checked);
                            const noneChecked = Array.from(childCheckboxes).every(cb => !cb.checked);
                            if (noneChecked) {
                                allCheckbox.checked = false;
                            }
                            else if (areChecked) {
                                allCheckbox.checked = true;
                            }
                            else {
                                allCheckbox.checked = false;

                            }

                            const hasSelectedItems = this._drawingLayer.graphics.items.some((graphic) => {
                                return graphic.attributes && graphic.attributes.isSelected === true;
                            });
                            const button = document.getElementById("submit");
                            button.disabled = !hasSelectedItems;

                        }
                    });
                });

                if (this.fieldControl) {
                    this.fieldControl.addEventListener("change", (e) => {
                        that.handleLabelsOnDrawingLayer();
                        const listItems = document.querySelectorAll("#Main-Selection li");
                        listItems.forEach((li) => {
                            const importKey = li.id;
                            const item = that._drawingLayer.graphics.items.find((pi) => pi.attributes && pi.attributes["ImportKey"] === importKey);
                            if (item) {
                                const existingTextNode = li.querySelector("span");
                                if (existingTextNode) {
                                    existingTextNode.textContent = item.attributes[that.fieldControl.options[that.fieldControl.selectedIndex].value];
                                } else {
                                    const textNode = document.createElement("span");
                                    textNode.textContent = item.attributes[that.fieldControl.options[that.fieldControl.selectedIndex].value];
                                    li.appendChild(textNode);
                                }
                            }
                        });

                    });
                }

                let sources = [];
                if (this.ocLayer_Polygon) {
                    sources.push({
                        layer: this.ocLayer_Polygon,
                        enabled: true
                    });
                }
                if (this.ocLayer_Line) {
                    sources.push({
                        layer: this.ocLayer_Line,
                        enabled: true
                    });
                }

                const fullscreenWidget = new Fullscreen({
                    view: this.view,
                    element: document.getElementById("shell")
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

                const scaleBarWidget = new ScaleBar({
                    view: this.view,
                    unit: "dual" // The scale bar displays both metric and non-metric units.
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
                    coordinateSegments: [
                        {
                            alias: "X",
                            description: "Grid Reference"
                        }
                    ]
                });
                const coordinateConversionWidget = new CoordinateConversion({
                    view: this.view,
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

                const coordinateHelpButton = document.getElementById("help-coordinate-button");
                if (coordinateHelpButton) {
                    coordinateHelpButton.addEventListener("click", () => {
                        that.OpenPopUp("/Compartment/help#coordinateHelp");
                    });
                }

                const importHelpButton = document.getElementById("help-import-button");
                if (importHelpButton) {
                    importHelpButton.addEventListener("click", () => {
                        that.OpenPopUp("/Compartment/help#importHelp");
                    });
                }

                const baseMapHelpButton = document.getElementById("help-basemap-button");
                if (baseMapHelpButton) {
                    baseMapHelpButton.addEventListener("click", () => {
                        that.OpenPopUp("/Compartment/help#basemapHelp");
                    });
                }

                const compartmentHelpButton = document.getElementById("help-compartment-button");
                if (compartmentHelpButton) {
                    compartmentHelpButton.addEventListener("click", () => {
                        that.OpenPopUp("/Compartment/help#compartmentEditHelp");
                    });
                }

                //
                const coordinateCloseButton = document.getElementById("close-coordinate-button");
                if (coordinateCloseButton) {
                    coordinateCloseButton.addEventListener("click", () => {
                        document.querySelector(`[data-action-id=${that.activeWidget}]`).active = false;
                        document.querySelector(`[data-panel-id=${that.activeWidget}]`).hidden = true;
                        that.activeWidget = null;
                    });
                }

                const importCloseButton = document.getElementById("close-import-button");
                if (importCloseButton) {
                    importCloseButton.addEventListener("click", () => {
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

                const compartmentCloseButton = document.getElementById("close-compartment-button");
                if (compartmentCloseButton) {
                    compartmentCloseButton.addEventListener("click", () => {
                        document.querySelector(`[data-action-id=${that.activeWidget}]`).active = false;
                        document.querySelector(`[data-panel-id=${that.activeWidget}]`).hidden = true;
                        that.activeWidget = null;
                    });
                }

                this.alertWidget = new AlertWidget({});

                await fetch("/api/Gis/UploadSettings", mapSettings.requestParamsAPI).then((response) => {
                    if (!response.ok) {
                        throw new Error(`Failed with HTTP code ${response.status}`);
                    }
                    return response.json();

                }).then((json) => {
                    that.supportedFileTypes = json.supportedFileTypes;
                    that.maxFileSize = json.maxSize;
                    this.view.ui.move("zoom", "top-right");
                    that.view.ui.add(compassWidget, "top-right");
                    that.view.ui.add(fullscreenWidget, "top-right");
                    that.view.ui.add(that.alertWidget, "top-right");
                    that.view.ui.add(scaleBarWidget, "bottom-left");
                    that.view.ui.add(document.getElementById("editCompartment"), "top-left");

                    const editDetailsHelpButton = document.getElementById("help-editDetails-button");
                    if (editDetailsHelpButton) {
                        editDetailsHelpButton.addEventListener("click", () => {
                            that.OpenPopUp("/Compartment/help#editDetails");
                        });
                    }
                }).catch(() => {
                    that.alertWidget.ShowMessage("error", "Unable to load supported file types. Importing is blocked! ");
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

                return;

            };

            /**
           * Opens a pop up window at the path
           *  @param {string} value Stringfied GIS JSON object
           **/
            bulkImportMap.prototype.OpenPopUp = function (path) {
                const left = (window.screen.width - mapSettings.popup.width) / 2;
                const top = (window.screen.height - mapSettings.popup.height) / 2;
                let settings = `width=${mapSettings.popup.width}, height=${mapSettings.popup.height}, top=${top}, left=${left}`;

                const popupURL = `${window.location.origin}${path}`;
                const popup = window.open(popupURL, 'Popup Window', settings);
                if (!popup || popup.closed || typeof popup.closed === 'undefined') {
                    that.alertWidget.ShowMessage("info", "The popup window was blocked by the browser. Please allow pop-ups for this website.");
                }
            };

            /**
             * Loads the data from the API
             **/
            bulkImportMap.prototype.loadData = async function () {
                const that = this;
                return await fetch("/api/GIS/GetPropertyDetails?propertyGuid=" +
                    document.getElementById("PropertyProfileId").value,
                    {
                        credentials: mapSettings.requestParamsAPI.credentials,
                        headers: {
                            'Accept': "application/json",
                            "Content-type": "application/json"
                        }
                    }).then(function (r) {
                        if (r.status !== 200) {
                            return r.text().then(function (data) { return ({ status: r.status, body: data }); });
                        }
                        return r.json().then(function (data) { return ({ status: r.status, body: data }); });
                    }).then(function (obj) {
                        if (obj.status !== 200) {
                            if (obj.status === 401) {
                                that.alertWidget.ShowMessage("", "You don't have access to this property");
                                return [];
                            }
                            that.alertWidget("", "Unable to load the property");
                            return [];
                        }
                        if (!obj.body) {
                            that.alertWidget.ShowMessage("", "Unable to read settings");
                            return [];
                        }
                        that.nearestTown = obj.body.nearestTown;
                        return obj.body.allPropertyCompartments;
                    });
            }

            /**
            * Draws all other woodland compartments shapes to the relevant layers.
            **/
            bulkImportMap.prototype.buildGraphics = async function (compartments) {
                const that = this;
                let graphics = [];
                if (typeof compartments === "undefined" || compartments === null || compartments.length === 0) {
                    return graphics;
                }

                compartments.forEach(function (item) {
                    if (!item.gisData) {
                        return;
                    }

                    const gisData = JSON.parse(item.gisData);
                    if (typeof gisData === "undefined" || gisData === null || !gisData.spatialReference || !gisData.spatialReference.wkid) {
                        that.alertWidget.ShowMessage("", `Invalid object:${JSON.stringify(item)}`);
                        return;
                    }

                    var geometry;

                    if (gisData.rings) {
                        geometry = {
                            type: "polygon",
                            rings: gisData.rings,
                            spatialReference: gisData.spatialReference.wkid
                        };


                        try {
                            graphics.push(new Graphic({
                                geometry: geometry,
                                attributes: {
                                    compartmentName: item.compartmentNumber
                                }
                            }));
                        } catch (e) {
                            if (window.console) {
                                console.error("Failed to create all required parts for view.", e);
                            }
                        }
                    }
                });


                this.buildBackGroundLayer(graphics);
                return graphics;
            };


            /**
            * Draws all other compartment polygons to the "Polygon Layer"
            * @param {Geometry[]} polygons The geometry of the polygons 
            **/
            bulkImportMap.prototype.buildBackGroundLayer = function (polygons) {
                try {
                    if (typeof polygons === "undefined" || polygons === null || polygons.length === 0) {
                        return;
                    }
                    const labelSymbol = JSON.parse(JSON.stringify(mapSettings.activeTextSymbol));

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
                                expression: "$feature.compartmentName",
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
                } catch (e) {
                    if (window.console) {
                        console.error("Failed to create all required parts for view.", e);
                    }
                }
            };

            bulkImportMap.prototype.checkIntersectionsWithFeatureLayer = async function (geometry) {
                try {
                    const query = this.ocLayer_Polygon.createQuery();
                    query.geometry = geometry;
                    query.spatialRelationship = "intersects";
                    query.returnGeometry = false;
                    query.outFields = ["*"];

                    const result = await this.ocLayer_Polygon.queryFeatures(query);

                    return result.features.length > 0;
                } catch (error) {
                    console.error("Error checking intersections with FeatureLayer:", error);
                    return false;
                }
            };

            /**
             * Goes to an extent on the map.
             * If theres graphics then the graphics.
             * else the nearest town, if thats not set the the extent for england
             * @param {graphics[]} points The graphics to go to
             **/
            bulkImportMap.prototype.Goto = async function (graphics) {
                var that = this;

                if (typeof graphics === "undefined" || graphics === null || graphics.length === 0) {
                    if (!that.nearestTown) {
                        that.alertWidget.ShowMessage("info", "Nearest town wasn't set");
                        this.view.goTo({
                            target: this.defaultMapExtentForEngland.center,
                            zoom: 8
                        });
                        return true;
                    }
                    return this.esriHelper.locateTown(this.nearestTown, locator, mapSettings.esriGeoServiceLocatorUrl)
                        .then(function (results) {
                            if (results[0] != undefined) {
                                that.view.goTo({
                                    center: [results[0].location.x, results[0].location.y],
                                    zoom: 15
                                });
                            } else {
                                that.alertWidget.ShowMessage("", `Unable to find town "${nearestTown}"`);
                                //Default the zoom level to 6, as GBR was set as country, if the place is invalid
                                that.view.goTo({
                                    zoom: 6
                                });
                            }
                            return true;
                        });
                } else {
                    const items = graphics.map((g) => { return g.geometry; });
                    this.view.goTo({ target: items });
                }
                return true;
            }


            /**
             * Checks the string to see if its a valid string
             * @param {value} string - The value to check
             * @param {fieldName} String -  field name to use in the validation errors returned
             * @param {maxLength} Number - Max length the string can be (Set to 0 to ignore)
             * @param {canBeEmpty} bool - If the string must be set
             * @param {regTest} bool - If the reg check must be preformed
             */
            bulkImportMap.prototype.checkString = function (value, fieldName, maxLength, canBeEmpty, regTest) {
                if (!canBeEmpty) {
                    if (typeof value === "undefined" || value === null || value === "" || value.trim() === "") {

                        return { result: false, value: `${fieldName} must be set` };
                    }
                }

                if (regTest) {
                    const reg = new RegExp("^[-a-zA-Z0-9'\\s]*$");
                    if (!reg.test(value)) {
                        return {
                            result: false,
                            value: `${fieldName} must only include letters a to z, numbers, and special characters such as hyphens, spaces and apostrophes`
                        };
                    }
                }
                if (maxLength > 0) {
                    if (typeof value === "undefined" || value === null || value.trim() === "") {

                        return { result: true, value: "" };
                    }
                    if (value.length > maxLength) {
                        return {
                            result: false,
                            value: `${fieldName}  must be ${maxLength} characters or less`
                        };
                    }
                }

                return { result: true, value: "" };
            },

                /**
                 * Try's to get the value of the field listed
                 * @param {item} Graphic
                 * @param {field} string - The name of the field to look for
                 */
                bulkImportMap.prototype.GetFieldValue = function (item, field) {
                    if (typeof field === "undefined" || field === null || field === "") {
                        return { found: true, value: "" }
                    }

                    const testVal = item.attributes[field];
                    if (typeof testVal === "undefined" || testVal === null) {
                        return { found: false, value: "" }
                    }
                    return { found: true, value: testVal + "" }
                },

                /**
                * Gets the label Graphic to add to the map
                * @param {any} shapeGraphic The Shape to add the label to.
                */
                bulkImportMap.prototype.getLabelGraphic = function (shapeGraphic) {
                    var resx;
                    let label = shapeGraphic.attributes.compartmentName;
                    var labelSymbol = JSON.parse(JSON.stringify(mapSettings.activeTextSymbol));
                    labelSymbol.text = label;
                    if (shapeGraphic.geometry.type === "point") {
                        labelSymbol.xoffset = mapSettings.pointOffset.xoffset;
                        labelSymbol.yoffset = mapSettings.pointOffset.yoffset;
                        resx = new Graphic({
                            geometry: shapeGraphic.geometry,
                            symbol: labelSymbol
                        });
                    }
                    else if (shapeGraphic.geometry.type === "polyline") {
                        labelSymbol.xoffset = mapSettings.pointOffset.xoffset;
                        labelSymbol.yoffset = mapSettings.pointOffset.yoffset;
                        resx = new Graphic({
                            geometry: shapeGraphic.geometry.extent.center,
                            symbol: labelSymbol
                        });
                    }
                    else {
                        resx = new Graphic({
                            geometry: shapeGraphic.geometry.centroid,
                            symbol: labelSymbol
                        });
                    }
                    if (!resx.attributes) {
                        resx.attributes = {
                            shapeID: shapeGraphic.uid
                        };
                    }
                    return resx;
                };
            bulkImportMap.prototype.getSizeOfShape = function (workingGraphic) {
                var hectares = geometryEngine.planarArea(workingGraphic.geometry, "hectares");
                if (hectares < 0) {
                    hectares = hectares * -1;
                }

                return hectares;
            }
            /**
             * Saves the features to the system
             **/
            bulkImportMap.prototype.Save = function () {
                var list = [];
                var that = this;
                this._drawingLayer.graphics.items.forEach((g) => {
                    if (!g.attributes || g.attributes.isSelected !== true) {
                        return;
                    }
                    const name = "" + g.attributes[that.fieldControl.options[that.fieldControl.selectedIndex].value] || that.generateGuid();
                    list.push({
                        ShapeID: g.uid,
                        CompartmentNumber: name,
                        TotalHectares: that.getSizeOfShape(g),
                        designation: "",
                        woodlandName: "",
                        GISData: JSON.stringify(g.geometry.toJSON())
                    });
                });

                this.submitBtn.enabled = false;

                fetch("/api/GIS/Import",
                    {
                        credentials: mapSettings.requestParamsAPI.credentials,
                        method: "POST",
                        headers: {
                            'Accept': "application/json",
                            "Content-type": "application/json"
                        },
                        body: JSON.stringify({
                            PropertyProfile: document.getElementById("PropertyProfileId").value,
                            Compartments: list
                        })
                    }).then(function (response) {
                        if (!response.ok) {
                            return {
                                error: `Failed with HTTP code ${response.status} `
                            }
                        }
                        return response.json();
                    }).then(function (resx) {
                        if (resx.error) {
                            that.alertWidget.ShowMessage("", resx.error);
                            return;
                        }

                        if (typeof resx.failures === "undefined" || resx.failures === null || resx.failures.length === 0) {
                            that.alertWidget.ShowMessage("success", "Shapes imported. Redirecting back to woodland");
                            try {
                                var applicationId = document.getElementById("ApplicationId").value;
                                var woodlandOwnerId = document.getElementById("WoodlandOwnerId").value;
                                var agencyId = document.getElementById("AgencyId").value;

                                if (applicationId === null || applicationId === "00000000-0000-0000-0000-000000000000" || applicationId === '') {

                                    var woodlandOwnerParameter = '&woodlandOwnerId=' + woodlandOwnerId;
                                    var agencyParameter = '';

                                    if (agencyId !== null && agencyId !== "00000000-0000-0000-0000-000000000000" && agencyId !== '') {
                                        agencyParameter = '&agencyId=' + agencyId;
                                    }

                                    window.location = window.origin + "/PropertyProfile/Edit?id=" + document.getElementById("PropertyProfileId").value + woodlandOwnerParameter + agencyParameter;
                                }
                                else {
                                    window.location = window.origin + `/FellingLicenceApplication/SelectCompartments?applicationId=${applicationId}`;
                                }
                            }
                            catch (e) {
                                window.location = window.origin;
                            }
                            return;
                        } else {
                            that.alertWidget.ShowDetailedMessages("warning", "The following compartments couldn't be saved:", resx.failures.map((f) => f.value));
                            resx.success.forEach((s) => {
                                that._drawingLayer.graphics.every((g) => {
                                    if (g.uid === s) {
                                        g.attributes.saved = true;
                                        return false;
                                    }
                                    return true;
                                });
                            });
                        }

                    });
            }

            bulkImportMap.prototype.getfileNamePartsArray = function (filePath) {
                return filePath.replace("c:\\fakepath\\", "").split(".");
            }

            bulkImportMap.prototype.getStringFormData = function (data, fileNameParts) {
                var name = "";
                for (let i = fileNameParts.length - 2; i >= 0; i--) {
                    name = fileNameParts[i] + name;
                }
                var formData = new FormData();
                formData.append("valueString", JSON.stringify(data));
                formData.append("name", name);
                formData.append("ext", "geojson");
                return formData;
            }

            bulkImportMap.prototype.getFormData = function (fileNameParts, file) {
                var name = "";
                for (let i = fileNameParts.length - 2; i >= 0; i--) {
                    name = fileNameParts[i] + name;
                }
                var formData = new FormData();
                formData.append(
                    "file",
                    file
                );
                formData.append("name", name);
                formData.append("ext", fileNameParts[fileNameParts.length - 1]);
                return formData;
            }

            bulkImportMap.prototype.processResult = function (data) {
                if (data.featureCollection.layers[0].layerDefinition.geometryType !== "esriGeometryPolygon") {
                    this.alertWidget.ShowMessage("error", "Only Polygons are supported");
                    return;
                }

                const that = this;

                if (data.featureCollection.layers.length === 0) {
                    that.alertWidget.ShowMessage("error", "No layers found in the file");
                    document.querySelector('[data-panel-id="importer"]').removeAttribute("loading");
                    that.clearImportWidget();
                    return;
                }
                if (data.featureCollection.layers[0].featureSet.features.length === 0) {
                    that.alertWidget.ShowMessage("error", "No features found");
                    document.querySelector('[data-panel-id="importer"]').removeAttribute("loading");
                    that.clearImportWidget();
                    return;
                }

                var nextPanel = document.getElementById(`SelectArea`);
                if (nextPanel) {
                    nextPanel.removeAttribute("hidden");
                }

                nextPanel = document.getElementById(`SelectField`);
                if (nextPanel) {
                    nextPanel.removeAttribute("hidden");
                }

                data.featureCollection.layers[0].featureSet.features[0].attributes
                this.setCommonKeys(data.featureCollection);

                const possibleItems = data.featureCollection.layers[0].featureSet.features.map((f) => {
                    f.attributes["ImportKey"] = this.generateGuid();
                    f.attributes.isSelected = false;
                    f.geometry.type = "polygon";
                    return new Graphic({
                        geometry: f.geometry,
                        attributes: f.attributes,
                        symbol: {
                            type: "simple-fill",
                            color: [0, 0, 255, 0.5],
                            outline: {
                                color: [0, 0, 255],
                                width: 1
                            }
                        }
                    });
                });

                this._drawingLayer.removeAll();
                this._drawingLayer.addMany(possibleItems);
                that.handleLabelsOnDrawingLayer();

                this.view.goTo(possibleItems).catch(function (error) {
                    if (error.name != "AbortError") {
                        console.error(error);
                    }
                });

                var list = document.getElementById("selection");
                this.removeAllChildNodes(list);
                list.innerHTML = "";
                var main = document.createElement("ul");
                main.id = "Main-Selection";
                var all = document.createElement("li");
                all.id = "all";

                possibleItems.forEach(item => {
                    const li = document.createElement("li");
                    var checkbox = document.createElement("input");
                    checkbox.setAttribute("data-ImportKey", item.attributes.ImportKey);
                    checkbox.type = "checkbox";
                    li.appendChild(checkbox);
                    li.setAttribute("data-ImportKey", item.attributes.ImportKey);
                    var span = document.createElement("span");
                    span.textContent = item.attributes[that.fieldControl.options[that.fieldControl.selectedIndex].value] || item.attributes["FID"];
                    li.appendChild(span);

                    const goToButton = document.createElement("button");
                    goToButton.textContent = "Go to";
                    goToButton.className = "govuk-button";
                    goToButton.addEventListener("click", (e) => {
                        e.stopPropagation();

                        const gotoItem = this._drawingLayer.graphics.items.find((pi) => pi.attributes && pi.attributes["ImportKey"] === item.attributes.ImportKey);

                        if (gotoItem) {
                            this.view.goTo(gotoItem.geometry).catch((error) => {
                                if (error.name !== "AbortError") {
                                    console.error(error);
                                }
                            });
                        }
                    });
                    li.appendChild(goToButton);

                    li.addEventListener("mouseenter", (e) => {
                        const gotoItem = possibleItems.find((pi) => pi.attributes && pi.attributes["ImportKey"] === e.target.id);

                        if (!gotoItem) {
                            return;
                        }
                        if (this.currentHighlight) {
                            this.currentHighlight.remove();
                        }


                        gotoItem.symbol = {
                            type: "simple-fill",
                            color: gotoItem.symbol.color,
                            outline: mapSettings.importShapeSelected.outline
                        };

                        this.currentHighlight = {
                            remove: () => {
                                gotoItem.symbol = {
                                    type: "simple-fill",
                                    color: gotoItem.symbol.color,
                                    outline: mapSettings.importShape.outline
                                };
                            }
                        };

                        if (document.getElementById("snapTo").checked) {
                            this.view.goTo(gotoItem.geometry).catch((error) => {
                                if (error.name !== "AbortError") {
                                    console.error(error);
                                }
                            });
                        }



                    });
                    li.addEventListener("mouseleave", (e) => {
                        if (that.currentHighlight) {
                            that.currentHighlight.remove();
                        }
                    });
                    li.addEventListener("click", async (e) => {
                        e.stopPropagation();

                        let checkbox;
                        let key = e.target.getAttribute("data-ImportKey");
                        if (e.target.type === "checkbox") {
                            checkbox = e.target;
                        } else {
                            checkbox = e.target.querySelector("input[type='checkbox']");
                            checkbox.checked = !checkbox.checked;
                        }

                        const gotoItem = this._drawingLayer.graphics.items.find((pi) => pi.attributes && pi.attributes["ImportKey"] === key);

                        if (!gotoItem) {
                            console.warn("Graphic not found for key:", key);
                            return;
                        }

                        if (checkbox.checked) {
                            const intersects = await this.checkIntersectionsWithFeatureLayer(gotoItem.geometry);

                            if (intersects) {
                                that.alertWidget.ShowMessage("error", "This shape intersects with another shape in the map.");
                                checkbox.checked = false;
                                return;
                            }
                            gotoItem.attributes.isSelected = true;
                            gotoItem.symbol = mapSettings.importShapeSelected;
                            this._drawingLayer.remove(gotoItem);
                            this._drawingLayer.add(gotoItem);
                        } else {
                            // Unselect the graphic
                            gotoItem.attributes.isSelected = false;
                            gotoItem.symbol = mapSettings.importShape;

                            this._drawingLayer.remove(gotoItem);
                            this._drawingLayer.add(gotoItem);
                        }

                        if (checkbox) {
                            const allCheckbox = document.getElementById("all").querySelector("input[type='checkbox']");
                            const childCheckboxes = main.querySelectorAll("input[type='checkbox']");
                            const areChecked = Array.from(childCheckboxes).every(cb => cb.checked);
                            const noneChecked = Array.from(childCheckboxes).every(cb => !cb.checked);
                            if (noneChecked) {
                                allCheckbox.checked = false;
                            }
                            else if (areChecked) {
                                allCheckbox.checked = true;
                            }
                            else {
                                allCheckbox.checked = false;

                            }

                        }
                    });
                    li.id = item.attributes["ImportKey"];
                    main.appendChild(li);
                });

                var checkbox = document.createElement("input");
                checkbox.type = "checkbox";
                all.appendChild(checkbox);
                all.addEventListener("click", (e) => {
                    e.stopPropagation();
                    let checkbox;
                    if (e.target.type === "checkbox") {
                        checkbox = e.target;
                    } else {
                        checkbox = e.target.querySelector("input[type='checkbox']");
                        checkbox.checked = !checkbox.checked;
                    }

                    const childCheckboxes = main.querySelectorAll("input[type='checkbox']");
                    childCheckboxes.forEach((childCheckbox) => {
                        childCheckbox.checked = checkbox.checked;
                    });


                    let items = [];
                    if (checkbox.checked) {
                        items = this._drawingLayer.graphics.items.filter((i) => i.attributes).map((i) => {
                            i.attributes.isSelected = true;
                            i.symbol = mapSettings.importShapeSelected;
                            return i
                        });
                    }
                    else {
                        items = this._drawingLayer.graphics.items.filter((i) => i.attributes).map((i) => {
                            i.attributes.isSelected = false;
                            i.symbol = mapSettings.importShape;
                            return i
                        });
                    }

                    that._drawingLayer.removeMany(items);
                    that._drawingLayer.addMany(items);

                });
                all.appendChild(document.createTextNode("All"));
                all.appendChild(main);
                list.appendChild(all);

                document.querySelector('[data-panel-id="importer"]').removeAttribute("loading");
            }

            bulkImportMap.prototype.setCommonKeys = function (featureCollection) {
                const features = featureCollection.layers[0].featureSet.features;
                const that = this;

                if (!features || features.length === 0) {
                    return [];
                }

                let commonKeys = Object.keys(features[0].attributes);

                features.forEach(feature => {
                    const currentKeys = Object.keys(feature.attributes);
                    commonKeys = commonKeys.filter(key => currentKeys.includes(key));
                });
                this.removeAllChildNodes(this.fieldControl);

                commonKeys.forEach(key => {
                    const option = document.createElement("option");
                    option.value = key;
                    option.textContent = key;
                    that.fieldControl.appendChild(option);
                });
                if (that.fieldControl.options.length > 0) {
                    that.fieldControl.options[0].selected = true;
                }

            }

            bulkImportMap.prototype.clearImportWidget = function () {
                this._drawingLayer.removeAll();
                this.handleLabelsOnDrawingLayer();

                var list = document.getElementById("selection");
                
                this.removeAllChildNodes(list);

                var nextPanel = document.getElementById(`SelectArea`);
                if (nextPanel) {
                    nextPanel.setAttribute("hidden", "true");
                }
                nextPanel = document.getElementById(`SelectField`);
                if (nextPanel) {
                    nextPanel.setAttribute("hidden", "true");
                }
            };

            bulkImportMap.prototype.removeAllChildNodes = function (parent) {
                if (!parent) return;

                while (parent.firstChild) {
                    parent.removeChild(parent.firstChild);
                }
            }

            bulkImportMap.prototype.generateGuid = function () {
                return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
                    const r = Math.random() * 16 | 0;
                    const v = c === 'x' ? r : (r & 0x3 | 0x8);
                    return v.toString(16);
                });
            };

            bulkImportMap.prototype.handleLabelsOnDrawingLayer = function () {
                const that = this;
                const textGraphics = this._drawingLayer.graphics.items.filter((graphic) => {
                    return graphic.symbol.type === "text";
                });

                this._drawingLayer.removeMany(textGraphics);
                const labelGraphics = this._drawingLayer.graphics.items.map((graphic) => {
                    let textSymbol = JSON.parse(JSON.stringify(mapSettings.activeTextSymbol));
                    textSymbol.text = graphic.attributes[that.fieldControl.options[that.fieldControl.selectedIndex || 0].value] || "";

                    let labelGeometry;
                    if (graphic.geometry.type === "point") {
                        labelGeometry = graphic.geometry;
                    } else if (graphic.geometry.type === "polygon") {
                        labelGeometry = graphic.geometry.centroid;
                    } else if (graphic.geometry.type === "polyline") {
                        labelGeometry = graphic.geometry.extent.center;
                    }

                    return new Graphic({
                        geometry: labelGeometry,
                        symbol: textSymbol
                    });
                });

                this._drawingLayer.addMany(labelGraphics);
            };

            bulkImportMap.prototype.addSelectionWatcher = function () {
                const that = this;

                const button = document.getElementById("submit");

                this._drawingLayer.graphics.on("change", () => {
                    const hasSelectedItems = this._drawingLayer.graphics.items.some((graphic) => {
                        return graphic.attributes && graphic.attributes.isSelected === true;
                    });

                    button.disabled = !hasSelectedItems;
                });
            };

            return bulkImportMap;
        }());
        return bulkImportMap;
    });
