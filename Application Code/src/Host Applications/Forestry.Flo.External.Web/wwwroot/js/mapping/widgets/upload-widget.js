define(["require", "exports", "tslib", "esri/core/accessorSupport/decorators", "esri/widgets/Widget", "esri/widgets/support/widget", "esri/layers/FeatureLayer", "esri/Graphic", "esri/layers/support/Field"], function (require, exports, tslib_1, decorators_1, Widget_1, widget_1, FeatureLayer_1, Graphic_1, Field_1) {
    "use strict";
    Widget_1 = tslib_1.__importDefault(Widget_1);
    FeatureLayer_1 = tslib_1.__importDefault(FeatureLayer_1);
    Graphic_1 = tslib_1.__importDefault(Graphic_1);
    Field_1 = tslib_1.__importDefault(Field_1);
    var UploadWidget = /** @class */ (function (_super) {
        tslib_1.__extends(UploadWidget, _super);
        function UploadWidget(params) {
            var _this = _super.call(this, params) || this;
            _this.handleNameChange = function (evt) {
                _this.compartmentName = evt.target.value;
            };
            _this.handleWoodlandChange = function (evt) {
                _this.woodlandName = evt.target.value;
            };
            _this.errorMessage = "";
            _this.isUploading = false;
            _this.hasUpload = false;
            _this.currentlySelecting = false;
            return _this;
        }
        UploadWidget.prototype.stopSelection = function () {
            if (this.currentlySelecting !== false) {
                this.currentlySelecting = false;
            }
        };
        UploadWidget.prototype.clearEvt = function () {
            var elm = document.getElementById("inFile");
            if (elm) {
                elm.value = "";
            }
            var importedLayers = this.map.layers.items.filter(function (l) {
                return l.IsImported;
            });
            this.compartmentName = null;
            this.woodlandName = null;
            this.emit("noSelecting", {});
            this.map.layers.removeMany(importedLayers);
            this.errorMessage = "";
            this.isUploading = false;
            this.hasUpload = false;
        };
        UploadWidget.prototype.getSize = function (value) {
            return value / Math.pow(1024, 2) + "MB";
        };
        UploadWidget.prototype.changeEvt = function (evt) {
            var that = this;
            this.errorMessage = "";
            var elm = document.getElementById("inFile");
            if (!elm) {
                this.errorMessage = "Unable to find file";
                return;
            }
            var filePath = elm.value.toLowerCase();
            if (typeof filePath === "undefined" || filePath.length === 0) {
                this.errorMessage = "No file found";
                return;
            }
            var fileExt = filePath.slice(filePath.lastIndexOf("."), filePath.length);
            if (!this.supportedFileTypes.includes(fileExt)) {
                this.errorMessage = "File Type is not supported";
                return;
            }
            this.isUploading = true;
            if (fileExt.toLocaleLowerCase() === ".kml" || fileExt.toLocaleLowerCase() === ".kmz") {
                var fileInput = document.getElementById("inFile");
                if (!fileInput) {
                    this.isUploading = false;
                    this.errorMessage = "Unable to find file";
                    return;
                }
                var fileReader = new FileReader();
                fileReader.readAsText(fileInput.files[0]);
                fileReader.onload = function () {
                    var dom = new DOMParser().parseFromString(fileReader.result, "text/xml");
                    fetch(window.origin + "/api/Gis/GetShapesFromString", {
                        method: "POST",
                        body: that.getStringFormData(that.geoConvertor.kml(dom), that.getfileNamePartsArray(filePath)),
                    })
                        .then(function (r) {
                            if (r.status != 200) {
                                return r
                                    .text()
                                    .then(function (data) { return ({ status: r.status, body: data }); });
                            }
                            else {
                                return r
                                    .json()
                                    .then(function (data) { return ({ status: r.status, body: data }); });
                            }
                        })
                        .then(function (obj) {
                            if (obj.status !== 200) {
                                that.isUploading = false;
                                that.hasUpload = false;
                                that.emit("failed", { message: obj.body });
                                return;
                            }
                            that.processResult(obj.body);
                            that.errorMessage = "";
                            that.isUploading = false;
                            that.hasUpload = true;
                            that.selectEvt();
                            if (that.allowMappings) {
                                that.emit("uploaded", {
                                    compartmentName: that.compartmentName,
                                    woodlandName: that.woodlandName
                                });
                            }
                            else {
                                that.emit("uploaded");
                            }
                        })
                        .catch(function (e) {
                            that.isUploading = false;
                            that.hasUpload = false;
                            that.emit("failed", { message: e });
                        });
                };
                fileReader.onerror = function () {
                    that.isUploading = false;
                    that.errorMessage = fileReader.error.message;
                };
            }
            else if (fileExt.toLocaleLowerCase() === ".geojson" || fileExt.toLocaleLowerCase() === ".json") {
                var fileInput = document.getElementById("inFile");
                if (!fileInput) {
                    this.isUploading = false;
                    this.errorMessage = "Unable to find file";
                    return;
                }
                var fileReader = new FileReader();
                fileReader.readAsText(fileInput.files[0]);
                fileReader.onload = function () {

                    fetch(window.origin + "/api/Gis/GetShapesFromString", {
                        method: "POST",
                        body: that.getStringFormData(JSON.parse(fileReader.result), that.getfileNamePartsArray(filePath)),
                    })
                        .then(function (r) {
                            if (r.status != 200) {
                                return r
                                    .text()
                                    .then(function (data) { return ({ status: r.status, body: data }); });
                            }
                            else {
                                return r
                                    .json()
                                    .then(function (data) { return ({ status: r.status, body: data }); });
                            }
                        })
                        .then(function (obj) {
                            if (obj.status !== 200) {
                                that.isUploading = false;
                                that.hasUpload = false;
                                that.emit("failed", { message: obj.body });
                                return;
                            }
                            that.processResult(obj.body);
                            that.errorMessage = "";
                            that.isUploading = false;
                            that.hasUpload = true;
                            that.selectEvt();
                            if (that.allowMappings) {
                                that.emit("uploaded", {
                                    compartmentName: that.compartmentName,
                                    woodlandName: that.woodlandName
                                });
                            }
                            else {
                                that.emit("uploaded");
                            }
                        })
                        .catch(function (e) {
                            that.isUploading = false;
                            that.hasUpload = false;
                            that.emit("failed", { message: e });
                        });
                };
                fileReader.onerror = function () {
                    that.isUploading = false;
                    that.errorMessage = fileReader.error.message;
                };
            }
            else {
                if (document.getElementById("inFile").files[0].size > that.maxFileSize) {
                    that.isUploading = false;
                    that.hasUpload = false;
                    that.errorMessage = "File is too big to be imported currently supported size is " + that.getSize(that.maxFileSize) + "";
                    return;
                }
                fetch(window.origin + "/api/Gis/GetShapes", {
                    method: "POST",
                    body: this.getFormData(this.getfileNamePartsArray(filePath)),
                })
                    .then(function (r) {
                        if (r.status != 200) {
                            return r.text().then(function (data) { return ({ status: r.status, body: data }); });
                        }
                        else {
                            return r.json().then(function (data) { return ({ status: r.status, body: data }); });
                        }
                    })
                    .then(function (obj) {
                        if (obj.status !== 200) {
                            that.isUploading = false;
                            that.hasUpload = false;
                            that.emit("failed", { message: obj.body });
                            return;
                        }
                        that.processResult(obj.body);
                        that.errorMessage = "";
                        that.isUploading = false;
                        that.hasUpload = true;
                        that.selectEvt();
                        if (that.allowMappings) {
                            that.emit("uploaded", {
                                compartmentName: that.compartmentName,
                                woodlandName: that.woodlandName
                            });
                        }
                        else {
                            that.emit("uploaded");
                        }
                    })
                    .catch(function (e) {
                        that.isUploading = false;
                        that.hasUpload = false;
                        that.emit("failed", { message: e });
                    });
            }
        };
        UploadWidget.prototype.selectEvt = function () {
            this.currentlySelecting = !this.currentlySelecting;
            this.emit(this.currentlySelecting ? "selecting" : "noSelecting", {});
        };
        UploadWidget.prototype.processResult = function (response) {
            var sourceGraphics = [];

            if (!response.features) {
                var layers = response.featureCollection.layers.filter((l) => l.featureSet.geometryType === "esriGeometryPolygon").map(function (layer) {
                    var graphics = layer.featureSet.features.map(function (feature) {
                        return Graphic_1.default.fromJSON(feature);
                    });
                    sourceGraphics = sourceGraphics.concat(graphics);
                    var featureLayer = new FeatureLayer_1.default({
                        source: graphics,
                        fields: layer.layerDefinition.fields.map(function (field) {
                            return Field_1.default.fromJSON(field);
                        }),
                    });
                    featureLayer.IsImported = true;
                    return featureLayer;
                });
            }
            else {

                var graphics = response.features.filter((i) => i.geometry.type === "Polygon")
                    .map(function (feature) {
                        return Graphic_1.default.fromJSON(feature.geometry);
                    });

                var featureLayer = new FeatureLayer_1.default({
                    source: graphics,
                    fields: layer.layerDefinition.fields.map(function (field) {
                        return Field_1.default.fromJSON(field);
                    }),
                });
                featureLayer.IsImported = true;
                return featureLayer;
            }
            this.map.addMany(layers);
            this.view.goTo(sourceGraphics).catch(function (error) {
                if (error.name != "AbortError") {
                    console.error(error);
                }
            });
        };
        UploadWidget.prototype.getfileNamePartsArray = function (filePath) {
            return filePath.replace("c:\\fakepath\\", "").split(".");
        };
        UploadWidget.prototype.getFormData = function (fileNameParts) {
            var name = "";
            for (var i = fileNameParts.length - 2; i >= 0; i--) {
                name = fileNameParts[i] + name;
            }
            var formData = new FormData();
            formData.append("file", document.getElementById("inFile").files[0]);
            formData.append("name", name);
            formData.append("ext", fileNameParts[fileNameParts.length - 1]);
            return formData;
        };
        UploadWidget.prototype.getStringFormData = function (data, fileNameParts) {
            var name = "";
            for (var i = fileNameParts.length - 2; i >= 0; i--) {
                name = fileNameParts[i] + name;
            }
            var formData = new FormData();
            formData.append("valueString", JSON.stringify(data));
            formData.append("name", name);
            formData.append("ext", "geojson");
            return formData;
        };
        UploadWidget.prototype.renderLoading = function () {
            return this.isUploading ? ((0, widget_1.tsx)("calcite-progress", { label: "uploading", text: "uploading", type: "indeterminate" })) : null;
        };
        UploadWidget.prototype.renderError = function () {
            return !this.errorMessage ? null : ((0, widget_1.tsx)("calcite-notice", { open: true },
                (0, widget_1.tsx)("div", { slot: "title" }, "Error"),
                (0, widget_1.tsx)("div", { slot: "message" }, this.errorMessage)));
        };
        UploadWidget.prototype.renderUploadButton = function () {
            return ((0, widget_1.tsx)("calcite-button", { slot: "footer-actions", width: "auto", disabled: this.isUploading, onclick: this.changeEvt.bind(this) }, "Upload"));
        };
        UploadWidget.prototype.renderClearButton = function () {
            return ((0, widget_1.tsx)("calcite-button", { slot: "footer-actions", width: "auto", disabled: this.isUploading, onclick: this.clearEvt.bind(this), appearance: "clear" }, "Clear"));
        };
        UploadWidget.prototype.renderCompartmentNumber = function () {
            return ((0, widget_1.tsx)("calcite-label", null,
                "Compartment",
                (0, widget_1.tsx)("input", { type: "text", onchange: this.handleNameChange, value: this.compartmentName })));
        };
        UploadWidget.prototype.renderWoodland = function () {
            return ((0, widget_1.tsx)("calcite-label", null,
                "Woodland",
                (0, widget_1.tsx)("input", { type: "text", onchange: this.handleWoodlandChange, value: this.woodlandName })));
        };
        UploadWidget.prototype.renderMapping = function () {
            return ((0, widget_1.tsx)("calcite-block", { collapsible: true, hidden: this.isUploading, heading: "Field Mapping", description: "Please select fields to be mapped from source file" }, [
                this.renderCompartmentNumber(),
                this.renderWoodland()
            ]));
        };
        UploadWidget.prototype.renderUpload = function () {
            return ((0, widget_1.tsx)("calcite-panel", {},
                [this.renderUploadButton(), this.renderClearButton()],
                (0, widget_1.tsx)("calcite-block", { open: true, hidden: !this.errorMessage }, this.renderError()),
                (0, widget_1.tsx)("calcite-block", { open: true, hidden: this.isUploading, heading: "Select File",},
                    (0, widget_1.tsx)("form", { enctype: "multipart/form-data", method: "post", id: "uploadForm" },
                        (0, widget_1.tsx)("input", { type: "file", name: "file", id: "inFile" }))),
                this.allowMappings ? this.renderMapping() : null,
                (0, widget_1.tsx)("calcite-block", { open: true, hidden: !this.isUploading }, this.renderLoading())));
        };
        UploadWidget.prototype.renderButtons = function () {
            return ((0, widget_1.tsx)("calcite-action-bar", { layout: "vertical", expanded: true, "expand-disabled": "" },
                (0, widget_1.tsx)("calcite-action", { text: "Import Shapes", icon: "select", scale: "l", active: this.currentlySelecting, onclick: this.selectEvt.bind(this) }),
                (0, widget_1.tsx)("calcite-action", { text: "Stop importing shapes", icon: "upload-to", scale: "l", onclick: this.clearEvt.bind(this) })
            ));
        };
        UploadWidget.prototype.render = function () {
            var style = "max-height: 500px;overflow: auto;height: 500px;resize: vertical;";
            if (this.hasUpload || this.isUploading) {
                style = "max-height: 500px;overflow: auto;";
            }
            return ((0, widget_1.tsx)("div", {}, this.hasUpload ? this.renderButtons() : this.renderUpload()));
        };
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], UploadWidget.prototype, "supportedFileTypes", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], UploadWidget.prototype, "view", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], UploadWidget.prototype, "multiUpload", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], UploadWidget.prototype, "geoConvertor", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], UploadWidget.prototype, "map", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], UploadWidget.prototype, "hasUpload", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], UploadWidget.prototype, "currentlySelecting", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], UploadWidget.prototype, "errorMessage", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], UploadWidget.prototype, "isUploading", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], UploadWidget.prototype, "hasMappings", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], UploadWidget.prototype, "allowMappings", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], UploadWidget.prototype, "compartmentName", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], UploadWidget.prototype, "woodlandName", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], UploadWidget.prototype, "maxFileSize", void 0);
        UploadWidget = tslib_1.__decorate([
            (0, decorators_1.subclass)("esri.widgets.UploadWidget")
        ], UploadWidget);
        return UploadWidget;
    }(Widget_1.default));
    return UploadWidget;
});
