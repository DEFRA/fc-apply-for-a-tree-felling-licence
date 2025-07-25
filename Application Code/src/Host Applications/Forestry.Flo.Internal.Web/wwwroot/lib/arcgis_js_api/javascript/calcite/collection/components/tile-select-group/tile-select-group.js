/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { updateHostInteraction } from "../../utils/interactive";
/**
 * @slot - A slot for adding `calcite-tile-select`s.
 */
export class TileSelectGroup {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
    this.disabled = false;
    /**
     * Defines the layout of the component.
     *
     * Use `"horizontal"` for rows, and `"vertical"` for a single column.
     */
    this.layout = "horizontal";
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentDidRender() {
    updateHostInteraction(this);
  }
  render() {
    return h("slot", null);
  }
  static get is() { return "calcite-tile-select-group"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["tile-select-group.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["tile-select-group.css"]
    };
  }
  static get properties() {
    return {
      "disabled": {
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
          "text": "When `true`, interaction is prevented and the component is displayed with lower opacity."
        },
        "attribute": "disabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "layout": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "TileSelectGroupLayout",
          "resolved": "\"horizontal\" | \"vertical\"",
          "references": {
            "TileSelectGroupLayout": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Defines the layout of the component.\n\nUse `\"horizontal\"` for rows, and `\"vertical\"` for a single column."
        },
        "attribute": "layout",
        "reflect": true,
        "defaultValue": "\"horizontal\""
      }
    };
  }
  static get elementRef() { return "el"; }
}
