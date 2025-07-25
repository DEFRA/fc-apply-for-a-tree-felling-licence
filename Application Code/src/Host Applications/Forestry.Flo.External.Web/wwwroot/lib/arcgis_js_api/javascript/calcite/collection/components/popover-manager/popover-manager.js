/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { createObserver } from "../../utils/observers";
/**
 * @slot - A slot for adding elements that reference a 'calcite-popover' by the 'selector' property.
 * @deprecated No longer required for popover usage.
 */
export class PopoverManager {
  constructor() {
    this.mutationObserver = createObserver("mutation", () => this.setAutoClose());
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * CSS Selector to match reference elements for popovers. Reference elements will be identified by this selector in order to open their associated popover.
     *
     * @default `[data-calcite-popover-reference]`
     */
    this.selector = "[data-calcite-popover-reference]";
    /**
     * Automatically closes any currently open popovers when clicking outside of a popover.
     */
    this.autoClose = false;
  }
  autoCloseHandler() {
    this.setAutoClose();
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    var _a;
    this.setAutoClose();
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el, { childList: true, subtree: true });
  }
  disconnectedCallback() {
    var _a;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    return h("slot", null);
  }
  // --------------------------------------------------------------------------
  //
  //  Private Methods
  //
  // --------------------------------------------------------------------------
  setAutoClose() {
    const { autoClose, el } = this;
    el.querySelectorAll("calcite-popover").forEach((popover) => (popover.autoClose = autoClose));
  }
  static get is() { return "calcite-popover-manager"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["popover-manager.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["popover-manager.css"]
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
              "text": "`[data-calcite-popover-reference]`"
            }],
          "text": "CSS Selector to match reference elements for popovers. Reference elements will be identified by this selector in order to open their associated popover."
        },
        "attribute": "selector",
        "reflect": true,
        "defaultValue": "\"[data-calcite-popover-reference]\""
      },
      "autoClose": {
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
          "text": "Automatically closes any currently open popovers when clicking outside of a popover."
        },
        "attribute": "auto-close",
        "reflect": true,
        "defaultValue": "false"
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "autoClose",
        "methodName": "autoCloseHandler"
      }];
  }
}
