define(["require", "exports", "tslib", "esri/core/accessorSupport/decorators", "esri/widgets/Widget", "esri/widgets/support/widget"], function (require, exports, tslib_1, decorators_1, Widget_1, widget_1) {
    "use strict";
    Widget_1 = tslib_1.__importDefault(Widget_1);
    var MapAlert = /** @class */ (function (_super) {
        tslib_1.__extends(MapAlert, _super);
        function MapAlert(params) {
            var _this = _super.call(this, params) || this;
            _this.message = "";
            _this.style = "";
            _this.messages = [];
            _this.show = false;
            return _this;
        }
        MapAlert.prototype.ShowMessage = function (style, message) {
            if (typeof message === "undefined" || message === "") {
                return;
            }
            this.style = style;
            this.message = message;
            this.messages = [];
            this.show = true;
        };
        MapAlert.prototype.ShowMessages = function (style, messages) {
            if (typeof messages === "undefined" || messages.length === 0) {
                return;
            }
            this.style = style;
            this.message = "";
            this.messages = messages;
            this.show = true;
        };
        MapAlert.prototype.ShowDetailedMessages = function (style, message, messages) {
            if (typeof messages === "undefined") {
                return;
            }
            this.style = style;
            this.message = message;
            this.messages = messages;
            this.show = true;
        };
        MapAlert.prototype.CloseMessage = function () {
            this.show = false;
            this.messages = [];
            this.message = "";
        };
        MapAlert.prototype.closeEvt = function (evt) {
            this.show = false;
            this.messages = [];
            this.message = "";
        };
        MapAlert.prototype.renderIcon = function () {
            var iconValue = "exclamation-mark-circle";
            switch (this.style) {
                case "success":
                    iconValue = "check-square";
                    break;
                case "info":
                    iconValue = "information";
                    break;
                case "warning":
                    iconValue = "exclamation-mark-triangle";
                    break;
            }
            var labelValue = this.style === "" ? "Error" : this.style;
            return ((0, widget_1.tsx)("calcite-icon", { "aria-label": labelValue, icon: iconValue, scale: "m" }));
        };
        MapAlert.prototype.renderList = function () {
            return ((0, widget_1.tsx)("ul", null, this.messages.map(function (m) {
                return ((0, widget_1.tsx)("li", null, m));
            })));
        };
        MapAlert.prototype.render = function () {
            var classes = ["alert"];
            return ((0, widget_1.tsx)("div", { role: "alert", class: this.classes("alert", this.style), hidden: !this.show },
                this.renderIcon(),
                (0, widget_1.tsx)("span", { class: "closebtn", role: "close", "aria-label": "close", onclick: this.closeEvt.bind(this) }, "\u00D7"),
                this.message,
                this.messages.length === 0 ? null : this.renderList()));
        };
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], MapAlert.prototype, "message", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], MapAlert.prototype, "messages", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], MapAlert.prototype, "style", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], MapAlert.prototype, "show", void 0);
        MapAlert = tslib_1.__decorate([
            (0, decorators_1.subclass)("esri.widgets.UploadWidget")
        ], MapAlert);
        return MapAlert;
    }(Widget_1.default));
    return MapAlert;
});
