/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { CSS } from "./resources";
import { getAncestors, getDepth } from "../combobox/utils";
import { guid } from "../../utils/guid";
import { getElementProp } from "../../utils/dom";
/**
 * @slot - A slot for adding `calcite-combobox-item`s.
 */
export class ComboboxItemGroup {
  constructor() {
    this.guid = guid();
    this.scale = "m";
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    this.ancestors = getAncestors(this.el);
    this.scale = getElementProp(this.el, "scale", this.scale);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    const { el, scale } = this;
    const indent = `${CSS.label}--indent-${getDepth(el)}`;
    return (h("ul", { "aria-labelledby": this.guid, class: { [CSS.list]: true, [`scale--${scale}`]: true }, role: "group" }, h("li", { class: { [CSS.label]: true, [indent]: true }, id: this.guid, role: "presentation" }, h("span", { class: CSS.title }, this.label)), h("slot", null)));
  }
  static get is() { return "calcite-combobox-item-group"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["combobox-item-group.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["combobox-item-group.css"]
    };
  }
  static get properties() {
    return {
      "ancestors": {
        "type": "unknown",
        "mutable": true,
        "complexType": {
          "original": "ComboboxChildElement[]",
          "resolved": "ComboboxChildElement[]",
          "references": {
            "ComboboxChildElement": {
              "location": "import",
              "path": "../combobox/interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the parent and grandparent `calcite-combobox-item`s, which are set on `calcite-combobox`."
        }
      },
      "label": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": true,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the title of the component."
        },
        "attribute": "label",
        "reflect": false
      }
    };
  }
  static get elementRef() { return "el"; }
}
