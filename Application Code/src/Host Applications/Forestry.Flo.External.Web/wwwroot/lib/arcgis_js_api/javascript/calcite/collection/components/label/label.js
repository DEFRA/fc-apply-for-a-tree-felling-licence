/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { labelDisconnectedEvent, labelConnectedEvent } from "../../utils/label";
import { CSS } from "./resources";
/**
 * @slot - A slot for adding text and a component that can be labeled.
 */
export class Label {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /** Specifies the text alignment of the component. */
    this.alignment = "start";
    /**
     * Specifies the status of the component and any child input, or input messages.
     *
     * @deprecated Set directly on the component the label is bound to instead.
     */
    this.status = "idle";
    /** Specifies the size of the component. */
    this.scale = "m";
    /** Defines the layout of the label in relation to the component. Use `"inline"` positions to wrap the label and component on the same line. */
    this.layout = "default";
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     *
     * @deprecated Use the `disabled` property on the component the label is bound to instead.
     */
    this.disabled = false;
    /**
     * When `true`, disables the component's spacing.
     *
     * @deprecated Set the `--calcite-label-margin-bottom` css variable to `0` instead.
     */
    this.disableSpacing = false;
    //--------------------------------------------------------------------------
    //
    //  Private Methods
    //
    //--------------------------------------------------------------------------
    this.labelClickHandler = (event) => {
      this.calciteInternalLabelClick.emit({
        sourceEvent: event
      });
    };
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    document.dispatchEvent(new CustomEvent(labelConnectedEvent));
  }
  disconnectedCallback() {
    document.dispatchEvent(new CustomEvent(labelDisconnectedEvent));
  }
  render() {
    return (h(Host, { onClick: this.labelClickHandler }, h("div", { class: CSS.container }, h("slot", null))));
  }
  static get is() { return "calcite-label"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["label.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["label.css"]
    };
  }
  static get properties() {
    return {
      "alignment": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Alignment",
          "resolved": "\"center\" | \"end\" | \"start\"",
          "references": {
            "Alignment": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the text alignment of the component."
        },
        "attribute": "alignment",
        "reflect": true,
        "defaultValue": "\"start\""
      },
      "status": {
        "type": "string",
        "mutable": false,
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
          "tags": [{
              "name": "deprecated",
              "text": "Set directly on the component the label is bound to instead."
            }],
          "text": "Specifies the status of the component and any child input, or input messages."
        },
        "attribute": "status",
        "reflect": true,
        "defaultValue": "\"idle\""
      },
      "for": {
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
          "text": "Specifies the `id` of the component the label is bound to. Use when the component the label is bound to does not reside within the component."
        },
        "attribute": "for",
        "reflect": true
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
      },
      "layout": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "\"inline\" | \"inline-space-between\" | \"default\"",
          "resolved": "\"default\" | \"inline\" | \"inline-space-between\"",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Defines the layout of the label in relation to the component. Use `\"inline\"` positions to wrap the label and component on the same line."
        },
        "attribute": "layout",
        "reflect": true,
        "defaultValue": "\"default\""
      },
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
          "tags": [{
              "name": "deprecated",
              "text": "Use the `disabled` property on the component the label is bound to instead."
            }],
          "text": "When `true`, interaction is prevented and the component is displayed with lower opacity."
        },
        "attribute": "disabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "disableSpacing": {
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
          "tags": [{
              "name": "deprecated",
              "text": "Set the `--calcite-label-margin-bottom` css variable to `0` instead."
            }],
          "text": "When `true`, disables the component's spacing."
        },
        "attribute": "disable-spacing",
        "reflect": true,
        "defaultValue": "false"
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteInternalLabelClick",
        "name": "calciteInternalLabelClick",
        "bubbles": false,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": ""
        },
        "complexType": {
          "original": "{\n    sourceEvent: MouseEvent;\n  }",
          "resolved": "{ sourceEvent: MouseEvent; }",
          "references": {
            "MouseEvent": {
              "location": "global"
            }
          }
        }
      }];
  }
  static get elementRef() { return "el"; }
}
