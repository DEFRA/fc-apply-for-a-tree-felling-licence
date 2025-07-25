/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import Color from "color";
import { COLORS, CSS } from "./resources";
import { getThemeName } from "../../utils/dom";
export class ColorPickerSwatch {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /**
     * When `true`, the component is active.
     */
    this.active = false;
    /**
     * Specifies the size of the component.
     */
    this.scale = "m";
  }
  handleColorChange(color) {
    this.internalColor = Color(color);
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentWillLoad() {
    this.handleColorChange(this.color);
  }
  render() {
    const { active, el, internalColor } = this;
    const borderRadius = active ? "100%" : "0";
    const hex = internalColor.hex();
    const theme = getThemeName(el);
    const borderColor = theme === "light" ? COLORS.borderLight : COLORS.borderDark;
    return (h("svg", { class: CSS.swatch, xmlns: "http://www.w3.org/2000/svg" }, h("title", null, hex), h("rect", { fill: hex, height: "100%", id: "swatch", rx: borderRadius, stroke: borderColor, "stroke-width": "2", style: { "clip-path": `inset(0 round ${borderRadius})` }, width: "100%" })));
  }
  static get is() { return "calcite-color-picker-swatch"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["color-picker-swatch.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["color-picker-swatch.css"]
    };
  }
  static get properties() {
    return {
      "active": {
        "type": "boolean",
        "mutable": false,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "When `true`, the component is active."
        },
        "attribute": "active",
        "reflect": true,
        "defaultValue": "false"
      },
      "color": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "see",
              "text": "https://developer.mozilla.org/en-US/docs/Web/CSS/color_value"
            }],
          "text": "The color value."
        },
        "attribute": "color",
        "reflect": false
      },
      "scale": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Scale",
          "resolved": "\"l\" | \"m\" | \"s\"",
          "references": {
            "Scale": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the size of the component."
        },
        "attribute": "scale",
        "reflect": true,
        "defaultValue": "\"m\""
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "color",
        "methodName": "handleColorChange"
      }];
  }
}
