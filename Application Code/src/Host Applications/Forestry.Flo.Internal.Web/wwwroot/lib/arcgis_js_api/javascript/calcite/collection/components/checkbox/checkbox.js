/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { guid } from "../../utils/guid";
import { HiddenFormInputSlot } from "../../utils/form";
import { connectLabel, disconnectLabel, getLabelText } from "../../utils/label";
import { connectForm, disconnectForm } from "../../utils/form";
import { updateHostInteraction } from "../../utils/interactive";
import { toAriaBoolean } from "../../utils/dom";
import { isActivationKey } from "../../utils/key";
export class Checkbox {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /** When `true`, the component is checked. */
    this.checked = false;
    /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
    this.disabled = false;
    /**
     * The hovered state of the checkbox.
     *
     * @internal
     */
    this.hovered = false;
    /**
     * When `true`, the component is initially indeterminate, which is independent from its `checked` value.
     *
     * The state is visual only, and can look different across browsers.
     *
     * @mdn [indeterminate](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input/checkbox#indeterminate_state_checkboxes)
     */
    this.indeterminate = false;
    /**
     * When `true`, the component must have a value in order for the form to submit.
     *
     * @internal
     */
    this.required = false;
    /** Specifies the size of the component. */
    this.scale = "m";
    //--------------------------------------------------------------------------
    //
    //  Private Properties
    //
    //--------------------------------------------------------------------------
    this.checkedPath = "M5.5 12L2 8.689l.637-.636L5.5 10.727l8.022-7.87.637.637z";
    this.indeterminatePath = "M13 8v1H3V8z";
    //--------------------------------------------------------------------------
    //
    //  Private Methods
    //
    //--------------------------------------------------------------------------
    this.getPath = () => this.indeterminate ? this.indeterminatePath : this.checked ? this.checkedPath : "";
    this.toggle = () => {
      if (!this.disabled) {
        this.checked = !this.checked;
        this.setFocus();
        this.indeterminate = false;
        this.calciteCheckboxChange.emit();
      }
    };
    this.keyDownHandler = (event) => {
      if (isActivationKey(event.key)) {
        this.toggle();
        event.preventDefault();
      }
    };
    this.clickHandler = () => {
      this.toggle();
    };
    //--------------------------------------------------------------------------
    //
    //  Event Listeners
    //
    //--------------------------------------------------------------------------
    this.onToggleBlur = () => {
      this.calciteInternalCheckboxBlur.emit(false);
    };
    this.onToggleFocus = () => {
      this.calciteInternalCheckboxFocus.emit(true);
    };
    this.onLabelClick = () => {
      this.toggle();
    };
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.toggleEl) === null || _a === void 0 ? void 0 : _a.focus();
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    this.guid = this.el.id || `calcite-checkbox-${guid()}`;
    connectLabel(this);
    connectForm(this);
  }
  disconnectedCallback() {
    disconnectLabel(this);
    disconnectForm(this);
  }
  componentDidRender() {
    updateHostInteraction(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    return (h(Host, { onClick: this.clickHandler, onKeyDown: this.keyDownHandler }, h("div", { "aria-checked": toAriaBoolean(this.checked), "aria-label": getLabelText(this), class: "toggle", onBlur: this.onToggleBlur, onFocus: this.onToggleFocus, ref: (toggleEl) => (this.toggleEl = toggleEl), role: "checkbox", tabIndex: this.disabled ? undefined : 0 }, h("svg", { "aria-hidden": "true", class: "check-svg", viewBox: "0 0 16 16" }, h("path", { d: this.getPath() })), h("slot", null)), h(HiddenFormInputSlot, { component: this })));
  }
  static get is() { return "calcite-checkbox"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["checkbox.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["checkbox.css"]
    };
  }
  static get properties() {
    return {
      "checked": {
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
          "tags": [],
          "text": "When `true`, the component is checked."
        },
        "attribute": "checked",
        "reflect": true,
        "defaultValue": "false"
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
          "tags": [],
          "text": "When `true`, interaction is prevented and the component is displayed with lower opacity."
        },
        "attribute": "disabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "guid": {
        "type": "string",
        "mutable": true,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "The `id` attribute of the component. When omitted, a globally unique identifier is used."
        },
        "attribute": "guid",
        "reflect": true
      },
      "hovered": {
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
          "text": "The hovered state of the checkbox."
        },
        "attribute": "hovered",
        "reflect": true,
        "defaultValue": "false"
      },
      "indeterminate": {
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
              "name": "mdn",
              "text": "[indeterminate](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input/checkbox#indeterminate_state_checkboxes)"
            }],
          "text": "When `true`, the component is initially indeterminate, which is independent from its `checked` value.\n\nThe state is visual only, and can look different across browsers."
        },
        "attribute": "indeterminate",
        "reflect": true,
        "defaultValue": "false"
      },
      "label": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": "Accessible name for the component."
        },
        "attribute": "label",
        "reflect": false
      },
      "name": {
        "type": "any",
        "mutable": false,
        "complexType": {
          "original": "any",
          "resolved": "any",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the name of the component on form submission."
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
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
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
      },
      "value": {
        "type": "any",
        "mutable": false,
        "complexType": {
          "original": "any",
          "resolved": "any",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "The component's value."
        },
        "attribute": "value",
        "reflect": false
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteInternalCheckboxBlur",
        "name": "calciteInternalCheckboxBlur",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": "Emits when the component is blurred."
        },
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        }
      }, {
        "method": "calciteCheckboxChange",
        "name": "calciteCheckboxChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Emits when the component's `checked` status changes."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteInternalCheckboxFocus",
        "name": "calciteInternalCheckboxFocus",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": "Emits when the component is focused."
        },
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
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
