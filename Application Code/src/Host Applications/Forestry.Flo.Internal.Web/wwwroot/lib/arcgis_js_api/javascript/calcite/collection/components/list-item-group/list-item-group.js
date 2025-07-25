/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { CSS } from "./resources";
import { HEADING_LEVEL } from "./resources";
import { Heading, constrainHeadingLevel } from "../functional/Heading";
/**
 * @slot - A slot for adding `calcite-list-item` and `calcite-list-item-group` elements.
 */
export class ListItemGroup {
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    var _a;
    const { el, heading, headingLevel } = this;
    const parentLevel = (_a = el.closest("calcite-list, calcite-list-item-group")) === null || _a === void 0 ? void 0 : _a.headingLevel;
    const relativeLevel = parentLevel ? constrainHeadingLevel(parentLevel + 1) : null;
    const level = headingLevel || relativeLevel || HEADING_LEVEL;
    return (h(Host, null, heading ? (h(Heading, { class: CSS.heading, level: level }, heading)) : null, h("div", { class: CSS.container, role: "group" }, h("slot", null))));
  }
  static get is() { return "calcite-list-item-group"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["list-item-group.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["list-item-group.css"]
    };
  }
  static get properties() {
    return {
      "heading": {
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
          "tags": [],
          "text": "The header text for all nested `calcite-list-item` rows."
        },
        "attribute": "heading",
        "reflect": true
      },
      "headingLevel": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "HeadingLevel",
          "resolved": "1 | 2 | 3 | 4 | 5 | 6",
          "references": {
            "HeadingLevel": {
              "location": "import",
              "path": "../functional/Heading"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the number at which section headings should start."
        },
        "attribute": "heading-level",
        "reflect": true
      }
    };
  }
  static get elementRef() { return "el"; }
}
