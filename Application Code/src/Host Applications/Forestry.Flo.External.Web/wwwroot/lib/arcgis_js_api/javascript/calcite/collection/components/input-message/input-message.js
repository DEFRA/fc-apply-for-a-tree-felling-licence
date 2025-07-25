/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { Host, h } from "@stencil/core";
import { getElementProp, setRequestedIcon } from "../../utils/dom";
import { StatusIconDefaults } from "./interfaces";
/**
 * @slot - A slot for adding text.
 */
export class InputMessage {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /** When `true`, the component is active. */
    this.active = false;
    /** Specifies the size of the component. */
    this.scale = "m";
    /** Specifies the status of the input field, which determines message and icons. */
    this.status = "idle";
  }
  handleIconEl() {
    this.requestedIcon = setRequestedIcon(StatusIconDefaults, this.icon, this.status);
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    this.status = getElementProp(this.el, "status", this.status);
    this.scale = getElementProp(this.el, "scale", this.scale);
    this.requestedIcon = setRequestedIcon(StatusIconDefaults, this.icon, this.status);
  }
  render() {
    const hidden = !this.active;
    return (h(Host, { "calcite-hydrated-hidden": hidden }, this.renderIcon(this.requestedIcon), h("slot", null)));
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  renderIcon(iconName) {
    if (iconName) {
      return h("calcite-icon", { class: "calcite-input-message-icon", icon: iconName, scale: "s" });
    }
  }
  static get is() { return "calcite-input-message"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["input-message.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["input-message.css"]
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
      "icon": {
        "type": "any",
        "mutable": false,
        "complexType": {
          "original": "boolean | string",
          "resolved": "boolean | string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies an icon to display."
        },
        "attribute": "icon",
        "reflect": true
      },
      "scale": {
        "type": "string",
        "mutable": true,
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
      },
      "status": {
        "type": "string",
        "mutable": true,
        "complexType": {
          "original": "Status",
          "resolved": "\"idle\" | \"invalid\" | \"valid\"",
          "references": {
            "Status": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the status of the input field, which determines message and icons."
        },
        "attribute": "status",
        "reflect": true,
        "defaultValue": "\"idle\""
      },
      "type": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "\"default\"",
          "resolved": "\"default\"",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "The `\"floating\"` type is no longer supported."
            }],
          "text": "Specifies the appearance of a slotted message - `\"default\"` (displayed under the component), or `\"floating\"` (positioned absolutely under the component)."
        },
        "attribute": "type",
        "reflect": true
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "status",
        "methodName": "handleIconEl"
      }, {
        "propName": "icon",
        "methodName": "handleIconEl"
      }];
  }
}
