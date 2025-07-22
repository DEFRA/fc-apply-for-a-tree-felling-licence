define(["require", "exports", "tslib", "esri/core/accessorSupport/decorators", "esri/widgets/Widget", "esri/widgets/support/widget"], function (require, exports, tslib_1, decorators_1, Widget_1, widget_1) {
    "use strict";
    Widget_1 = tslib_1.__importDefault(Widget_1);
    var EditWidget = /** @class */ (function (_super) {
        tslib_1.__extends(EditWidget, _super);
        function EditWidget(params) {
            var _this = _super.call(this, params) || this;
            _this.compartmentMessage = "";
            _this.woodlandMessage = "";
            _this.designationMessage = "";
            _this.handleNameChange = function (evt) {
                _this.shape.compartmentName = evt.target.value;
                if (_this.shape.compartmentName === null ||
                    _this.shape.compartmentName.length === 0) {
                    _this.compartmentMessage = "Compartment name or number must be provided";
                    return;
                }
                if (_this.shape.compartmentName.length > 35) {
                    _this.compartmentMessage =
                        "Compartment name or number must be 35 characters or less";
                    return;
                }
                if (!_this.reg.test(_this.shape.compartmentName)) {
                    _this.compartmentMessage =
                        "Compartment name or number must only include letters a to z, numbers, and special characters such as hyphens, spaces and apostrophes";
                    return;
                }
                _this.compartmentMessage = "";
            };
            _this.handleDesignationChange = function (evt) {
                _this.shape.designation = evt.target.value;
                if (_this.shape.designation.length > 35) {
                    _this.designationMessage =
                        "Designation must be 35 characters or less";
                    return;
                }
            };
            _this.handleWoodlandChange = function (evt) {
                _this.shape.woodlandName = evt.target.value;
                if (_this.shape.woodlandName.length > 35) {
                    _this.woodlandMessage =
                        "Woodland must be 35 characters or less";
                    return;
                }
            };
            _this.handleCancel = function (evt) {
                _this.SetShape(null);
                _this.emit("cancel", {});
            };
            _this.handleDelete = function (evt) {
                _this.emit("delete", { shapeID: _this.shape.shapeID });
                _this.SetShape(null);
            };
            _this.handleSubmit = function (evt) {
                _this.emit("save", {
                    shapeID: _this.shape.shapeID,
                    compartmentName: _this.shape.compartmentName,
                    designation: _this.shape.designation,
                    woodlandName: _this.shape.woodlandName
                });
                _this.SetShape(null);
            };
            _this.compartmentMessage = "";
            _this.shape = null;
            _this.reg = new RegExp("^[-a-zA-Z0-9'\\s]*$");
            return _this;
        }
        EditWidget.prototype.SetShape = function (shape) {
            this.shape = shape;
        };
        EditWidget.prototype.renderCompartmentNumber = function () {
            return ((0, widget_1.tsx)("calcite-label", null,
                "Compartment",
                (0, widget_1.tsx)("input", { type: "text", onchange: this.handleNameChange, value: this.shape.compartmentName }),
                (0, widget_1.tsx)("calcite-input-message", { active: this.compartmentMessage.length > 0 }, this.compartmentMessage)));
        };
        EditWidget.prototype.renderWoodland = function () {
            return ((0, widget_1.tsx)("calcite-label", null,
                "Woodland (optional)",
                (0, widget_1.tsx)("input", { type: "text", onchange: this.handleWoodlandChange, value: this.shape.woodlandName }),
                (0, widget_1.tsx)("calcite-input-message", { active: this.woodlandMessage.length > 0 }, this.woodlandMessage)));
        };
        EditWidget.prototype.renderDesignation = function () {
            return ((0, widget_1.tsx)("calcite-label", null,
                "Designation (optional)",
                (0, widget_1.tsx)("input", { type: "text", onchange: this.handleDesignationChange, value: this.shape.designation }),
                (0, widget_1.tsx)("calcite-input-message", { active: this.designationMessage.length > 0 }, this.designationMessage)));
        };
        EditWidget.prototype.render = function () {
            return ((0, widget_1.tsx)("div", null, this.shape === null ? null : ((0, widget_1.tsx)("calcite-panel", { },
                (0, widget_1.tsx)("calcite-button", { color: "red", width: "auto", slot: "footer-actions", onclick: this.handleDelete }, "Remove Compartment"),
                (0, widget_1.tsx)("calcite-button", { width: "auto", slot: "footer-actions", appearance: "outline", onclick: this.handleCancel }, "Cancel"),
                (0, widget_1.tsx)("calcite-button", {
                    width: "auto", slot: "footer-actions", disabled: this.compartmentMessage.length > 0 ||
                        this.designationMessage.length > 0 ||
                        this.woodlandMessage.length > 0, onclick: this.handleSubmit
                }, "Apply"),
                (0, widget_1.tsx)("calcite-block", { open: true }, this.shape === null
                    ? null
                    : [
                        this.renderCompartmentNumber(),
                        this.renderWoodland(),
                        this.renderDesignation(),
                    ])))));
        };
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], EditWidget.prototype, "shape", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], EditWidget.prototype, "compartmentMessage", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], EditWidget.prototype, "woodlandMessage", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], EditWidget.prototype, "designationMessage", void 0);
        EditWidget = tslib_1.__decorate([
            (0, decorators_1.subclass)("esri.widgets.EditWidget")
        ], EditWidget);
        return EditWidget;
    }(Widget_1.default));
    return EditWidget;
});
