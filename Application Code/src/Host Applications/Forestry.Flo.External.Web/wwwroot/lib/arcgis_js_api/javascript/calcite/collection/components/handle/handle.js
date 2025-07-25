/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { toAriaBoolean } from "../../utils/dom";
import { CSS, ICONS } from "./resources";
export class Handle {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * @internal
     */
    this.activated = false;
    /**
     * Value for the button title attribute
     */
    this.textTitle = "handle";
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.handleKeyDown = (event) => {
      switch (event.key) {
        case " ":
          this.activated = !this.activated;
          event.preventDefault();
          break;
        case "ArrowUp":
        case "ArrowDown":
          if (!this.activated) {
            return;
          }
          event.preventDefault();
          const direction = event.key.toLowerCase().replace("arrow", "");
          this.calciteHandleNudge.emit({ handle: this.el, direction });
          break;
      }
    };
    this.handleBlur = () => {
      this.activated = false;
    };
  }
  // --------------------------------------------------------------------------
  //
  //  Methods
  //
  // --------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.handleButton) === null || _a === void 0 ? void 0 : _a.focus();
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    return (
    // Needs to be a span because of https://github.com/SortableJS/Sortable/issues/1486
    h("span", { "aria-pressed": toAriaBoolean(this.activated), class: { [CSS.handle]: true, [CSS.handleActivated]: this.activated }, onBlur: this.handleBlur, onKeyDown: this.handleKeyDown, ref: (el) => {
        this.handleButton = el;
      }, role: "button", tabindex: "0", title: this.textTitle }, h("calcite-icon", { icon: ICONS.drag, scale: "s" })));
  }
  static get is() { return "calcite-handle"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["handle.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["handle.css"]
    };
  }
  static get properties() {
    return {
      "activated": {
        "type": "boolean",
        "mutable": true,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": ""
        },
        "attribute": "activated",
        "reflect": true,
        "defaultValue": "false"
      },
      "textTitle": {
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
          "text": "Value for the button title attribute"
        },
        "attribute": "text-title",
        "reflect": true,
        "defaultValue": "\"handle\""
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteHandleNudge",
        "name": "calciteHandleNudge",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Emitted when the handle is activated and the up or down arrow key is pressed.\n\n**Note:**: The `handle` event payload prop is deprecated, please use the event's `target`/`currentTarget` instead"
        },
        "complexType": {
          "original": "DeprecatedEventPayload",
          "resolved": "any",
          "references": {
            "DeprecatedEventPayload": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        }
      }];
  }
  static get methods() {
    return {
      "setFocus": {
        "complexType": {
          "signature": "() => Promise<void>",
          "parameters": [],
          "references": {
            "Promise": {
              "location": "global"
            }
          },
          "return": "Promise<void>"
        },
        "docs": {
          "text": "Sets focus on the component.",
          "tags": []
        }
      }
    };
  }
  static get elementRef() { return "el"; }
}
