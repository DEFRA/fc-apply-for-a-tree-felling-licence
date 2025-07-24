define(["require", "exports", "tslib", "esri/core/accessorSupport/decorators", "esri/widgets/Widget", "esri/widgets/support/widget"], function (require, exports, tslib_1, decorators_1, Widget_1, widget_1) {
    "use strict";
    Widget_1 = tslib_1.__importDefault(Widget_1);
    var SymbolEditor = /** @class */ (function (_super) {
        tslib_1.__extends(SymbolEditor, _super);
        function SymbolEditor(params) {
            var _this = _super.call(this, params) || this;
            _this.shape = null;
            return _this;
        }
        SymbolEditor.prototype.shapeChanged = function () {
            var jsonSymbol = {};
            var val = "";
            if (this.shape !== null) {
                val = this.shape.shapeType;
            }
            switch (val) {
                case "polygon":
                    jsonSymbol = {
                        type: this.shape.shapeSymbol.type,
                        style: this.shape.shapeSymbol.style,
                        color: this.shape.shapeSymbol.color,
                        outline: {
                            color: this.shape.shapeSymbol.outline.color,
                            width: this.shape.shapeSymbol.width,
                        },
                    };
                    break;
                case "polyline":
                    jsonSymbol = {
                        type: this.shape.shapeSymbol.type,
                        style: this.shape.shapeSymbol.style,
                        cap: this.shape.shapeSymbol.cap,
                        join: this.shape.shapeSymbol.join,
                        width: this.shape.shapeSymbol.width,
                        color: this.shape.shapeSymbol.color,
                        xoffset: this.shape.shapeSymbol.xoffset,
                        yoffset: this.shape.shapeSymbol.yoffset,
                    };
                    break;
                case "point":
                    jsonSymbol = {
                        type: this.shape.shapeSymbol.type,
                        style: this.shape.shapeSymbol.style,
                        angle: this.shape.shapeSymbol.angle,
                        size: this.shape.shapeSymbol.size,
                        xoffset: this.shape.shapeSymbol.xoffset,
                        yoffset: this.shape.shapeSymbol.yoffset,
                        color: this.shape.shapeSymbol.color,
                        outline: {
                            style: this.shape.shapeSymbol.outline.style,
                            cap: this.shape.shapeSymbol.outline.cap,
                            join: this.shape.shapeSymbol.outline.join,
                            width: this.shape.shapeSymbol.outline.width,
                            color: this.shape.shapeSymbol.outline.color,
                        },
                    };
                    break;
            }
            this.emit("shapeChanged", {
                id: this.shape.id,
                Symbol: jsonSymbol,
            });
        };
        SymbolEditor.prototype.LabelChanged = function () {
            var jsonSymbol = {
                type: this.shape.labelSymbol.type,
                text: this.shape.labelSymbol.text,
                color: this.shape.labelSymbol.color,
                haloColor: this.shape.labelSymbol.haloColor,
                haloSize: this.shape.labelSymbol.haloSize,
                xoffset: this.shape.labelSymbol.xoffset,
                yoffset: this.shape.labelSymbol.yoffset,
                font: {
                    size: this.shape.labelSymbol.font.size,
                    weight: this.shape.labelSymbol.font.weight,
                },
            };
            this.emit("labelChanged", {
                id: this.shape.id,
                Symbol: jsonSymbol,
            });
        };
        SymbolEditor.prototype.renderNotSelected = function () {
            return ((0, widget_1.tsx)("calcite-block", { width: "200px", key: "nullOption", open: true, heading: "No Shape Selected" },
                (0, widget_1.tsx)("calcite-icon", { scale: "s", slot: "icon", icon: "exclamation-mark-triangle" }),
                (0, widget_1.tsx)("calcite-notice", { open: true },
                    (0, widget_1.tsx)("div", { slot: "message" }, "Please select shape to edit symbols"))));
        };
        SymbolEditor.prototype.renderShape = function () {
            var val = "";
            var renderer = "";
            if (this.shape !== null) {
                val = this.shape.shapeType;
            }
            switch (val) {
                case "polygon":
                    renderer = this.renderPolygon();
                    break;
                case "polyline":
                    renderer = this.renderPolyline();
                    break;
                case "point":
                    renderer = this.renderPoint();
                    break;
            }
            return renderer;
        };
        SymbolEditor.prototype.rgba2hex = function (orig) {
            orig = orig.toString();
            var a, isPercent, rgb = orig
                .replace(/\s/g, "")
                .match(/^rgba?\((\d+),(\d+),(\d+),?([^,\s)]+)?/i), alpha = ((rgb && rgb[4]) || "").trim(), hex = rgb
                    ? (rgb[1] | (1 << 8)).toString(16).slice(1) +
                    (rgb[2] | (1 << 8)).toString(16).slice(1) +
                    (rgb[3] | (1 << 8)).toString(16).slice(1)
                    : orig;
            if (alpha !== "") {
                a = alpha;
            }
            else {
                a = 1;
            }
            return "#" + hex;
        };
        SymbolEditor.prototype.renderPolygon = function () {
            var _this = this;
            return ((0, widget_1.tsx)("calcite-block", { key: "polygonSymbol", collapsible: true, open: true, heading: "Polygon Style" },
                (0, widget_1.tsx)("calcite-icon", { scale: "s", slot: "icon", icon: "polygon" }),
                (0, widget_1.tsx)("calcite-label", null,
                    "style",
                    (0, widget_1.tsx)("select", {
                        onchange: function (evt) {
                            _this.shape.shapeSymbol.style = evt.target.value;
                            _this.shapeChanged();
                        }, value: this.shape.shapeSymbol.style
                    },
                        (0, widget_1.tsx)("option", null, "backward-diagonal"),
                        (0, widget_1.tsx)("option", null, "cross"),
                        (0, widget_1.tsx)("option", null, "diagonal-cross"),
                        (0, widget_1.tsx)("option", null, "forward-diagonal"),
                        (0, widget_1.tsx)("option", null, "horizontal"),
                        (0, widget_1.tsx)("option", null, "none"),
                        (0, widget_1.tsx)("option", null, "solid"),
                        (0, widget_1.tsx)("option", null, "vertical"))),
                (0, widget_1.tsx)("calcite-label", null,
                    "Fill",
                    (0, widget_1.tsx)("input", {
                        type: "color", onchange: function (evt) {
                            _this.shape.shapeSymbol.color = evt.target.value;
                            _this.shapeChanged();
                        }, value: this.rgba2hex(this.shape.shapeSymbol.color)
                    })),
                (0, widget_1.tsx)("calcite-block-section", { open: true, text: "Outline", "toggle-display": "button" },
                    (0, widget_1.tsx)("calcite-notice", { open: true },
                        (0, widget_1.tsx)("div", { slot: "message" },
                            (0, widget_1.tsx)("calcite-label", null,
                                "Color",
                                (0, widget_1.tsx)("input", {
                                    type: "color", onchange: function (evt) {
                                        _this.shape.shapeSymbol.outline.color =
                                            evt.target.value;
                                        _this.shapeChanged();
                                    }, value: this.rgba2hex(this.shape.shapeSymbol.outline.color)
                                })),
                            (0, widget_1.tsx)("calcite-label", null,
                                "Width",
                                (0, widget_1.tsx)("input", {
                                    type: "number", min: "0", step: "0.25", onchange: function (evt) {
                                        _this.shape.shapeSymbol.outline.width =
                                            evt.target.value;
                                        _this.shapeChanged();
                                    }, value: this.shape.shapeSymbol.outline.width
                                })))))));
        };
        SymbolEditor.prototype.renderPolyline = function () {
            var _this = this;
            return ((0, widget_1.tsx)("calcite-block", { key: "polylineSymbol", open: true, heading: "Polyline Style" },
                (0, widget_1.tsx)("calcite-icon", { scale: "s", slot: "icon", icon: "line" }),
                (0, widget_1.tsx)("calcite-label", null,
                    "Style",
                    (0, widget_1.tsx)("select", {
                        onchange: function (evt) {
                            _this.shape.shapeSymbol.style = evt.target.value;
                            _this.shapeChanged();
                        }, value: this.shape.shapeSymbol.style
                    },
                        (0, widget_1.tsx)("option", { value: "dash" }, "dash"),
                        (0, widget_1.tsx)("option", { value: "dash-dot" }, "dash-dot"),
                        (0, widget_1.tsx)("option", { value: "dot" }, "dot"),
                        (0, widget_1.tsx)("option", { value: "long-dash" }, "long-dash"),
                        (0, widget_1.tsx)("option", { value: "long-dash-dot" }, "long-dash-dot"),
                        (0, widget_1.tsx)("option", { value: "long-dash-dot-dot" }, "long-dash-dot-dot"),
                        (0, widget_1.tsx)("option", { value: "none" }, "none"),
                        (0, widget_1.tsx)("option", { value: "short-dash" }, "short-dash"),
                        (0, widget_1.tsx)("option", { value: "short-dash-dot" }, "short-dash-dot"),
                        (0, widget_1.tsx)("option", { value: "short-dash-dot-dot" }, "short-dash-dot-dot"),
                        (0, widget_1.tsx)("option", { value: "short-dot" }, "short-dot"),
                        (0, widget_1.tsx)("option", { value: "solid" }, "solid"))),
                (0, widget_1.tsx)("calcite-label", null,
                    "Cap",
                    (0, widget_1.tsx)("select", {
                        onchange: function (evt) {
                            _this.shape.shapeSymbol.cap = evt.target.value;
                            _this.shapeChanged();
                        }, value: this.shape.shapeSymbol.cap
                    },
                        (0, widget_1.tsx)("option", { value: "butt" }, "butt"),
                        (0, widget_1.tsx)("option", { value: "round" }, "round"),
                        (0, widget_1.tsx)("option", { value: "square" }, "square"))),
                (0, widget_1.tsx)("calcite-label", null,
                    "Join",
                    (0, widget_1.tsx)("select", {
                        onchange: function (evt) {
                            _this.shape.shapeSymbol.join = evt.target.value;
                            _this.shapeChanged();
                        }, value: this.shape.shapeSymbol.join
                    },
                        (0, widget_1.tsx)("option", { value: "miter" }, "miter"),
                        (0, widget_1.tsx)("option", { value: "round" }, "round"),
                        (0, widget_1.tsx)("option", { value: "bevel" }, "bevel"))),
                (0, widget_1.tsx)("calcite-label", null,
                    "Width",
                    (0, widget_1.tsx)("input", {
                        type: "number", min: "0", step: "0.25", onchange: function (evt) {
                            _this.shape.shapeSymbol.width = evt.target.value;
                            _this.shapeChanged();
                        }, value: this.shape.shapeSymbol.width
                    })),
                (0, widget_1.tsx)("calcite-label", null,
                    "Color",
                    (0, widget_1.tsx)("input", {
                        type: "color", onchange: function (evt) {
                            _this.shape.shapeSymbol.color = evt.target.value;
                            _this.shapeChanged();
                        }, value: this.rgba2hex(this.shape.shapeSymbol.color)
                    })),
                (0, widget_1.tsx)("calcite-label", null,
                    "X-Offset",
                    (0, widget_1.tsx)("input", {
                        type: "number", min: "0", step: "1", onchange: function (evt) {
                            _this.shape.shapeSymbol.xoffset = evt.target.value;
                            _this.shapeChanged();
                        }, value: this.shape.shapeSymbol.xoffset
                    })),
                (0, widget_1.tsx)("calcite-label", null,
                    "Y-Offset",
                    (0, widget_1.tsx)("input", {
                        type: "number", min: "0", step: "1", onchange: function (evt) {
                            _this.shape.shapeSymbol.yoffset = evt.target.value;
                            _this.shapeChanged();
                        }, value: this.shape.shapeSymbol.yoffset
                    }))));
        };
        SymbolEditor.prototype.renderPoint = function () {
            var _this = this;
            return ((0, widget_1.tsx)("calcite-block", { key: "PointSymbol", open: true, heading: "Point Style" },
                (0, widget_1.tsx)("calcite-icon", { scale: "s", slot: "icon", icon: "point" }),
                (0, widget_1.tsx)("calcite-label", null,
                    "Style",
                    (0, widget_1.tsx)("select", {
                        onchange: function (evt) {
                            _this.shape.shapeSymbol.style = evt.target.value;
                            _this.shapeChanged();
                        }, value: this.shape.shapeSymbol.style
                    },
                        (0, widget_1.tsx)("option", { value: "circle" }, "circle"),
                        (0, widget_1.tsx)("option", { value: "cross" }, "cross"),
                        (0, widget_1.tsx)("option", { value: "diamond" }, "diamond"),
                        (0, widget_1.tsx)("option", { value: "path" }, "path"),
                        (0, widget_1.tsx)("option", { value: "square" }, "square"),
                        (0, widget_1.tsx)("option", { value: "x" }, "x"))),
                (0, widget_1.tsx)("calcite-label", null,
                    "Angle",
                    (0, widget_1.tsx)("input", {
                        type: "range", placeholder: "0", min: "-360", max: "360", step: "1", onchange: function (evt) {
                            _this.shape.shapeSymbol.angle = evt.target.value;
                            _this.shapeChanged();
                        }, value: this.shape.shapeSymbol.angle
                    })),
                (0, widget_1.tsx)("calcite-label", null,
                    "Size",
                    (0, widget_1.tsx)("input", {
                        type: "number", min: "0", step: "1", onchange: function (evt) {
                            _this.shape.shapeSymbol.size = evt.target.value;
                            _this.shapeChanged();
                        }, value: this.shape.shapeSymbol.size
                    })),
                (0, widget_1.tsx)("calcite-label", null,
                    "X-Offset",
                    (0, widget_1.tsx)("input", {
                        type: "number", min: "0", step: "1", onchange: function (evt) {
                            _this.shape.shapeSymbol.xoffset = evt.target.value;
                            _this.shapeChanged();
                        }, value: this.shape.shapeSymbol.xoffset
                    })),
                (0, widget_1.tsx)("calcite-label", null,
                    "Y-Offset",
                    (0, widget_1.tsx)("input", {
                        type: "number", min: "0", step: "1", onchange: function (evt) {
                            _this.shape.shapeSymbol.yoffset = evt.target.value;
                            _this.shapeChanged();
                        }, value: this.shape.shapeSymbol.yoffset
                    })),
                (0, widget_1.tsx)("calcite-label", null,
                    "Color",
                    (0, widget_1.tsx)("input", {
                        type: "color", onchange: function (evt) {
                            _this.shape.shapeSymbol.color = evt.target.value;
                            _this.shapeChanged();
                        }, value: this.rgba2hex(this.shape.shapeSymbol.color)
                    })),
                (0, widget_1.tsx)("calcite-block-section", { open: true, text: "Outline", "toggle-display": "button" },
                    (0, widget_1.tsx)("calcite-notice", { open: true },
                        (0, widget_1.tsx)("div", { slot: "message" },
                            (0, widget_1.tsx)("calcite-label", null,
                                "Style",
                                (0, widget_1.tsx)("select", {
                                    onchange: function (evt) {
                                        _this.shape.shapeSymbol.outline.style =
                                            evt.target.value;
                                        _this.shapeChanged();
                                    }, value: this.shape.shapeSymbol.outline.style
                                },
                                    (0, widget_1.tsx)("option", { value: "dash" }, "dash"),
                                    (0, widget_1.tsx)("option", { value: "dash-dot" }, "dash-dot"),
                                    (0, widget_1.tsx)("option", { value: "dot" }, "dot"),
                                    (0, widget_1.tsx)("option", { value: "long-dash" }, "long-dash"),
                                    (0, widget_1.tsx)("option", { value: "long-dash-dot" }, "long-dash-dot"),
                                    (0, widget_1.tsx)("option", { value: "long-dash-dot-dot" }, "long-dash-dot-dot"),
                                    (0, widget_1.tsx)("option", { value: "none" }, "none"),
                                    (0, widget_1.tsx)("option", { value: "short-dash" }, "short-dash"),
                                    (0, widget_1.tsx)("option", { value: "short-dash-dot" }, "short-dash-dot"),
                                    (0, widget_1.tsx)("option", { value: "short-dash-dot-dot" }, "short-dash-dot-dot"),
                                    (0, widget_1.tsx)("option", { value: "short-dot" }, "short-dot"),
                                    (0, widget_1.tsx)("option", { value: "solid" }, "solid"))),
                            (0, widget_1.tsx)("calcite-label", null,
                                "Cap",
                                (0, widget_1.tsx)("select", {
                                    onchange: function (evt) {
                                        _this.shape.shapeSymbol.outline.cap =
                                            evt.target.value;
                                        _this.shapeChanged();
                                    }, value: this.shape.shapeSymbol.outline.cap
                                },
                                    (0, widget_1.tsx)("option", { value: "butt" }, "butt"),
                                    (0, widget_1.tsx)("option", { value: "round" }, "round"),
                                    (0, widget_1.tsx)("option", { value: "square" }, "square"))),
                            (0, widget_1.tsx)("calcite-label", null,
                                "Join",
                                (0, widget_1.tsx)("select", {
                                    onchange: function (evt) {
                                        _this.shape.shapeSymbol.outline.join =
                                            evt.target.value;
                                        _this.shapeChanged();
                                    }, value: this.shape.shapeSymbol.outline.join
                                },
                                    (0, widget_1.tsx)("option", { value: "miter" }, "miter"),
                                    (0, widget_1.tsx)("option", { value: "round" }, "round"),
                                    (0, widget_1.tsx)("option", { value: "bevel" }, "bevel"))),
                            (0, widget_1.tsx)("calcite-label", null,
                                "Width",
                                (0, widget_1.tsx)("input", {
                                    type: "number", min: "0", step: "0.25", onchange: function (evt) {
                                        _this.shape.shapeSymbol.outline.width =
                                            evt.target.value;
                                        _this.shapeChanged();
                                    }, value: this.shape.shapeSymbol.outline.width
                                })),
                            (0, widget_1.tsx)("calcite-label", null,
                                "Color",
                                (0, widget_1.tsx)("input", {
                                    type: "color", onchange: function (evt) {
                                        _this.shape.shapeSymbol.outline.color =
                                            evt.target.value;
                                        _this.shapeChanged();
                                    }, value: this.rgba2hex(this.shape.shapeSymbol.outline.color)
                                })))))));
        };
        SymbolEditor.prototype.renderLabel = function () {
            var _this = this;
            return ((0, widget_1.tsx)("calcite-block", { key: "LabelSymbol", collapsible: true, heading: "Label Style", open: true },
                (0, widget_1.tsx)("calcite-icon", { scale: "s", slot: "icon", icon: "label" }),
                (0, widget_1.tsx)("calcite-label", null,
                    "Text",
                    (0, widget_1.tsx)("input", {
                        type: "text", value: this.shape.labelSymbol.text, onchange: function (evt) {
                            _this.shape.labelSymbol.text = evt.target.value;
                            _this.LabelChanged();
                        }
                    })),
                (0, widget_1.tsx)("calcite-label", null,
                    "Color",
                    (0, widget_1.tsx)("input", {
                        type: "color", onchange: function (evt) {
                            _this.shape.labelSymbol.color = evt.target.value;
                            _this.LabelChanged();
                        }, value: this.rgba2hex(this.shape.labelSymbol.color)
                    })),
                (0, widget_1.tsx)("calcite-label", null,
                    "Halo Color",
                    (0, widget_1.tsx)("input", {
                        type: "color", onchange: function (evt) {
                            _this.shape.labelSymbol.haloColor = evt.target.value;
                            _this.LabelChanged();
                        }, value: this.rgba2hex(this.shape.labelSymbol.haloColor)
                    })),
                (0, widget_1.tsx)("calcite-label", null,
                    "Halo Size",
                    (0, widget_1.tsx)("input", {
                        type: "number", min: "0", step: "1", max: this.shape.labelSymbol.font.size, onchange: function (evt) {
                            _this.shape.labelSymbol.haloSize = evt.target.value;
                            _this.LabelChanged();
                        }, value: this.shape.labelSymbol.haloSize
                    })),
                (0, widget_1.tsx)("calcite-label", null,
                    "X-Offset",
                    (0, widget_1.tsx)("input", {
                        type: "number", min: "0", step: "1", onchange: function (evt) {
                            _this.shape.labelSymbol.xoffset = evt.target.value;
                            _this.LabelChanged();
                        }, value: this.shape.labelSymbol.xoffset
                    })),
                (0, widget_1.tsx)("calcite-label", null,
                    "Y-Offset",
                    (0, widget_1.tsx)("input", {
                        type: "number", min: "0", step: "1", onchange: function (evt) {
                            _this.shape.labelSymbol.yoffset = evt.target.value;
                            _this.LabelChanged();
                        }, value: this.shape.labelSymbol.yoffset
                    })),
                (0, widget_1.tsx)("calcite-block-section", { open: true, text: "Font", "toggle-display": "button" },
                    (0, widget_1.tsx)("calcite-notice", { open: true },
                        (0, widget_1.tsx)("div", { slot: "message" },
                            (0, widget_1.tsx)("calcite-label", null,
                                "Size",
                                (0, widget_1.tsx)("input", {
                                    type: "number", min: "0", step: "1", max: 100, onchange: function (evt) {
                                        _this.shape.labelSymbol.font.size =
                                            evt.target.value;
                                        _this.LabelChanged();
                                    }, value: this.shape.labelSymbol.font.size
                                })),
                            (0, widget_1.tsx)("calcite-label", null,
                                "style",
                                (0, widget_1.tsx)("select", {
                                    onchange: function (evt) {
                                        _this.shape.labelSymbol.font.weight =
                                            evt.target.value;
                                        _this.LabelChanged();
                                    }, value: this.shape.labelSymbol.font.weight
                                },
                                    (0, widget_1.tsx)("option", { value: "normal" }, "normal"),
                                    (0, widget_1.tsx)("option", { value: "bold" }, "bold"),
                                    (0, widget_1.tsx)("option", { value: "bolder" }, "bolder"),
                                    (0, widget_1.tsx)("option", { value: "lighter" }, "lighter"))))))));
        };
        SymbolEditor.prototype.render = function () {
            var style = this.shape === null
                ? ""
                : "max-height: 500px;overflow: auto;resize: vertical;";
            return ((0, widget_1.tsx)("div", { style: style },
                (0, widget_1.tsx)("calcite-panel", { heading: "Style" },
                    this.renderShape(),
                    this.shape === null ? this.renderNotSelected() : this.renderLabel())));
        };
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], SymbolEditor.prototype, "shape", void 0);
        tslib_1.__decorate([
            (0, decorators_1.property)()
        ], SymbolEditor.prototype, "view", void 0);
        SymbolEditor = tslib_1.__decorate([
            (0, decorators_1.subclass)("esri.widgets.SelectionWidget")
        ], SymbolEditor);
        return SymbolEditor;
    }(Widget_1.default));
    return SymbolEditor;
});
