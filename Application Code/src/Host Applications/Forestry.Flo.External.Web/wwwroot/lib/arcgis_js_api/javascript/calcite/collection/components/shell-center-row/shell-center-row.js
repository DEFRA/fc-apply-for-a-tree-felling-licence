/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Fragment } from "@stencil/core";
import { CSS, SLOTS } from "./resources";
import { getSlotted } from "../../utils/dom";
import { connectConditionalSlotComponent, disconnectConditionalSlotComponent } from "../../utils/conditionalSlot";
/**
 * @slot - A slot for adding content to the `calcite-shell-panel`.
 * @slot action-bar - A slot for adding a `calcite-action-bar` to the `calcite-shell-panel`.
 */
export class ShellCenterRow {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, the content area displays like a floating panel.
     */
    this.detached = false;
    /**
     * Specifies the maximum height of the component.
     */
    this.heightScale = "s";
    /**
     * Specifies the component's position. Will be flipped when the element direction is right-to-left (`"rtl"`).
     */
    this.position = "end";
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    connectConditionalSlotComponent(this);
  }
  disconnectedCallback() {
    disconnectConditionalSlotComponent(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    const { el } = this;
    const contentNode = (h("div", { class: CSS.content }, h("slot", null)));
    const actionBar = getSlotted(el, SLOTS.actionBar);
    const actionBarNode = actionBar ? (h("div", { class: CSS.actionBarContainer, key: "action-bar" }, h("slot", { name: SLOTS.actionBar }))) : null;
    const children = [actionBarNode, contentNode];
    if ((actionBar === null || actionBar === void 0 ? void 0 : actionBar.position) === "end") {
      children.reverse();
    }
    return h(Fragment, null, children);
  }
  static get is() { return "calcite-shell-center-row"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["shell-center-row.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["shell-center-row.css"]
    };
  }
  static get properties() {
    return {
      "detached": {
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
          "text": "When `true`, the content area displays like a floating panel."
        },
        "attribute": "detached",
        "reflect": true,
        "defaultValue": "false"
      },
      "heightScale": {
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
          "text": "Specifies the maximum height of the component."
        },
        "attribute": "height-scale",
        "reflect": true,
        "defaultValue": "\"s\""
      },
      "position": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Position",
          "resolved": "\"end\" | \"start\"",
          "references": {
            "Position": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the component's position. Will be flipped when the element direction is right-to-left (`\"rtl\"`)."
        },
        "attribute": "position",
        "reflect": true,
        "defaultValue": "\"end\""
      }
    };
  }
  static get elementRef() { return "el"; }
}
