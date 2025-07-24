define(["require", "exports", "tslib", "esri/core/accessorSupport/decorators", "esri/widgets/Widget", "esri/widgets/support/widget"], function (require, exports, tslib_1, decorators_1, Widget_1, widget_1) {
    "use strict";
    Widget_1 = tslib_1.__importDefault(Widget_1);
    var SelectionWidget = /** @class */ (function (_super) {
        tslib_1.__extends(SelectionWidget, _super);
        function SelectionWidget(params) {
            return _super.call(this, params) || this;
        }
        SelectionWidget.prototype.changeEvt = function (evt) {
            var _this = this;
            this.shapes.every(function (shape) {
                if (shape.itemId === evt.currentTarget.value) {
                    shape.visible = !shape.visible;
                    if (shape.visible) {
                        _this.emit("show", { id: shape.itemId });
                    }
                    else {
                        _this.emit("hide", { id: shape.itemId });
                    }
                    return false;
                }
                return true;
            });
        };
        SelectionWidget.prototype.renderList = function () {
            var _this = this;
            return ((0, widget_1.tsx)("calcite-list", { "selection-appearance": "border", key: "graphics" }, this.shapes.map(function (shape) {
                return ((0, widget_1.tsx)("calcite-list-item", { onclick: _this.changeEvt.bind(_this), key: shape.itemId, label: shape.compartmentName, value: shape.itemId }, _this.renderAction(shape)));
            })));
        };
        SelectionWidget.prototype.renderAction = function (shape) {
            var icon = shape.visible ? "layer" : "layer-hide";
            var filler = shape.visible ? " " : " not ";
            return ((0, widget_1.tsx)("calcite-action", { slot: "actions-end", icon: icon, text: "".concat(shape.compartmentName, " is").concat(filler, "visible") }));
        };
        SelectionWidget.prototype.render = function () {
            var style = "max-height: 500px;overflow: auto;resize: vertical;";
            return ((0, widget_1.tsx)("div", { style: style },
                (0, widget_1.tsx)("calcite-panel", { heading: "Select Compartments" }, this.renderList())));
        };
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], SelectionWidget.prototype, "shapes", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], SelectionWidget.prototype, "view", void 0);
        SelectionWidget = tslib_1.__decorate([
            (0, decorators_1.subclass)("esri.widgets.SelectionWidget")
        ], SelectionWidget);
        return SelectionWidget;
    }(Widget_1.default));
    return SelectionWidget;
});
