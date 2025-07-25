/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
/**
 * @slot - A slot for adding elements that reference a `calcite-tooltip` by the `selector` property.
 * @deprecated No longer required for tooltip usage.
 */
export class TooltipManager {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * CSS Selector to match reference elements for tooltips. Reference elements will be identified by this selector in order to open their associated tooltip.
     *
     * @default `[data-calcite-tooltip-reference]`
     */
    this.selector = "[data-calcite-tooltip-reference]";
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    return h("slot", null);
  }
  static get is() { return "calcite-tooltip-manager"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["tooltip-manager.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["tooltip-manager.css"]
    };
  }
  static get properties() {
    return {
      "selector": {
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
              "name": "default",
              "text": "`[data-calcite-tooltip-reference]`"
            }],
          "text": "CSS Selector to match reference elements for tooltips. Reference elements will be identified by this selector in order to open their associated tooltip."
        },
        "attribute": "selector",
        "reflect": true,
        "defaultValue": "\"[data-calcite-tooltip-reference]\""
      }
    };
  }
}
