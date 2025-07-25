/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { Host, h } from "@stencil/core";
import { createObserver } from "../../utils/observers";
/**
 * @slot - A slot for adding `calcite-radio-button`s.
 */
export class RadioButtonGroup {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
    this.disabled = false;
    /** When `true`, the component is not displayed and its `calcite-radio-button`s are not focusable or checkable. */
    this.hidden = false;
    /** Defines the layout of the component. */
    this.layout = "horizontal";
    /** When `true`, the component must have a value in order for the form to submit. */
    this.required = false;
    /** Specifies the size of the component. */
    this.scale = "m";
    // --------------------------------------------------------------------------
    //
    //  Private Properties
    //
    // --------------------------------------------------------------------------
    this.mutationObserver = createObserver("mutation", () => this.passPropsToRadioButtons());
    //--------------------------------------------------------------------------
    //
    //  Private Methods
    //
    //--------------------------------------------------------------------------
    this.passPropsToRadioButtons = () => {
      const radioButtons = this.el.querySelectorAll("calcite-radio-button");
      if (radioButtons.length > 0) {
        radioButtons.forEach((radioButton) => {
          radioButton.disabled = this.disabled || radioButton.disabled;
          radioButton.hidden = this.hidden;
          radioButton.name = this.name;
          radioButton.required = this.required;
          radioButton.scale = this.scale;
        });
      }
    };
  }
  onDisabledChange() {
    this.passPropsToRadioButtons();
  }
  onHiddenChange() {
    this.passPropsToRadioButtons();
  }
  onLayoutChange() {
    this.passPropsToRadioButtons();
  }
  onScaleChange() {
    this.passPropsToRadioButtons();
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    var _a;
    this.passPropsToRadioButtons();
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el, { childList: true, subtree: true });
  }
  disconnectedCallback() {
    var _a;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  radioButtonChangeHandler(event) {
    this.calciteRadioButtonGroupChange.emit(event.target.value);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    return (h(Host, { role: "radiogroup" }, h("slot", null)));
  }
  static get is() { return "calcite-radio-button-group"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["radio-button-group.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["radio-button-group.css"]
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
      "hidden": {
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
          "text": "When `true`, the component is not displayed and its `calcite-radio-button`s are not focusable or checkable."
        },
        "attribute": "hidden",
        "reflect": true,
        "defaultValue": "false"
      },
      "layout": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Layout",
          "resolved": "\"grid\" | \"horizontal\" | \"vertical\"",
          "references": {
            "Layout": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Defines the layout of the component."
        },
        "attribute": "layout",
        "reflect": true,
        "defaultValue": "\"horizontal\""
      },
      "name": {
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
          "text": "Specifies the name of the component on form submission. Must be unique to other component instances."
        },
        "attribute": "name",
        "reflect": true
      },
      "required": {
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
          "text": "When `true`, the component must have a value in order for the form to submit."
        },
        "attribute": "required",
        "reflect": true,
        "defaultValue": "false"
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
  static get events() {
    return [{
        "method": "calciteRadioButtonGroupChange",
        "name": "calciteRadioButtonGroupChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component has changed."
        },
        "complexType": {
          "original": "any",
          "resolved": "any",
          "references": {}
        }
      }];
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "disabled",
        "methodName": "onDisabledChange"
      }, {
        "propName": "hidden",
        "methodName": "onHiddenChange"
      }, {
        "propName": "layout",
        "methodName": "onLayoutChange"
      }, {
        "propName": "scale",
        "methodName": "onScaleChange"
      }];
  }
  static get listeners() {
    return [{
        "name": "calciteRadioButtonChange",
        "method": "radioButtonChangeHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }];
  }
}
