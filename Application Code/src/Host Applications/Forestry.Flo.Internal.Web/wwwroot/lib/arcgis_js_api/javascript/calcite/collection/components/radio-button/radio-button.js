/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { guid } from "../../utils/guid";
import { focusElement, getElementDir, toAriaBoolean } from "../../utils/dom";
import { connectLabel, disconnectLabel, getLabelText } from "../../utils/label";
import { HiddenFormInputSlot, connectForm, disconnectForm } from "../../utils/form";
import { CSS } from "./resources";
import { getRoundRobinIndex } from "../../utils/array";
import { updateHostInteraction } from "../../utils/interactive";
export class RadioButton {
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
     * The focused state of the component.
     *
     * @internal
     */
    this.focused = false;
    /** When `true`, the component is not displayed and is not focusable or checkable. */
    this.hidden = false;
    /**
     * The hovered state of the component.
     *
     * @internal
     */
    this.hovered = false;
    /** When `true`, the component must have a value selected from the `calcite-radio-button-group` in order for the form to submit. */
    this.required = false;
    /** Specifies the size of the component inherited from the `calcite-radio-button-group`. */
    this.scale = "m";
    //--------------------------------------------------------------------------
    //
    //  Private Methods
    //
    //--------------------------------------------------------------------------
    this.selectItem = (items, selectedIndex) => {
      items[selectedIndex].click();
    };
    this.queryButtons = () => {
      return Array.from(this.rootNode.querySelectorAll("calcite-radio-button:not([hidden])")).filter((radioButton) => radioButton.name === this.name);
    };
    this.isDefaultSelectable = () => {
      const radioButtons = this.queryButtons();
      return !radioButtons.some((radioButton) => radioButton.checked) && radioButtons[0] === this.el;
    };
    this.check = () => {
      if (this.disabled) {
        return;
      }
      this.uncheckAllRadioButtonsInGroup();
      this.checked = true;
      this.focused = true;
      this.calciteRadioButtonChange.emit();
      this.setFocus();
    };
    this.clickHandler = () => {
      this.check();
    };
    this.setContainerEl = (el) => {
      this.containerEl = el;
    };
    this.handleKeyDown = (event) => {
      const keys = ["ArrowLeft", "ArrowUp", "ArrowRight", "ArrowDown", " "];
      const { key } = event;
      const { el } = this;
      if (keys.indexOf(key) === -1) {
        return;
      }
      if (key === " ") {
        this.check();
        event.preventDefault();
        return;
      }
      let adjustedKey = key;
      if (getElementDir(el) === "rtl") {
        if (key === "ArrowRight") {
          adjustedKey = "ArrowLeft";
        }
        if (key === "ArrowLeft") {
          adjustedKey = "ArrowRight";
        }
      }
      const radioButtons = Array.from(this.rootNode.querySelectorAll("calcite-radio-button:not([hidden]")).filter((radioButton) => radioButton.name === this.name);
      let currentIndex = 0;
      const radioButtonsLength = radioButtons.length;
      radioButtons.some((item, index) => {
        if (item.checked) {
          currentIndex = index;
          return true;
        }
      });
      switch (adjustedKey) {
        case "ArrowLeft":
        case "ArrowUp":
          event.preventDefault();
          this.selectItem(radioButtons, getRoundRobinIndex(Math.max(currentIndex - 1, -1), radioButtonsLength));
          return;
        case "ArrowRight":
        case "ArrowDown":
          event.preventDefault();
          this.selectItem(radioButtons, getRoundRobinIndex(currentIndex + 1, radioButtonsLength));
          return;
        default:
          return;
      }
    };
    this.onContainerBlur = () => {
      this.focused = false;
      this.calciteInternalRadioButtonBlur.emit();
    };
    this.onContainerFocus = () => {
      if (!this.disabled) {
        this.focused = true;
        this.calciteInternalRadioButtonFocus.emit();
      }
    };
  }
  checkedChanged(newChecked) {
    if (newChecked) {
      this.uncheckOtherRadioButtonsInGroup();
    }
    this.calciteInternalRadioButtonCheckedChange.emit();
  }
  nameChanged() {
    this.checkLastRadioButton();
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    if (!this.disabled) {
      focusElement(this.containerEl);
    }
  }
  onLabelClick(event) {
    if (!this.disabled && !this.hidden) {
      this.uncheckOtherRadioButtonsInGroup();
      const label = event.currentTarget;
      const radioButton = label.for
        ? this.rootNode.querySelector(`calcite-radio-button[id="${label.for}"]`)
        : label.querySelector(`calcite-radio-button[name="${this.name}"]`);
      if (radioButton) {
        radioButton.checked = true;
        radioButton.focused = true;
      }
      this.calciteRadioButtonChange.emit();
      this.setFocus();
    }
  }
  checkLastRadioButton() {
    const radioButtons = this.queryButtons();
    const checkedRadioButtons = radioButtons.filter((radioButton) => radioButton.checked);
    if ((checkedRadioButtons === null || checkedRadioButtons === void 0 ? void 0 : checkedRadioButtons.length) > 1) {
      const lastCheckedRadioButton = checkedRadioButtons[checkedRadioButtons.length - 1];
      checkedRadioButtons
        .filter((checkedRadioButton) => checkedRadioButton !== lastCheckedRadioButton)
        .forEach((checkedRadioButton) => {
        checkedRadioButton.checked = false;
        checkedRadioButton.emitCheckedChange();
      });
    }
  }
  /** @internal */
  async emitCheckedChange() {
    this.calciteInternalRadioButtonCheckedChange.emit();
  }
  uncheckAllRadioButtonsInGroup() {
    const radioButtons = this.queryButtons();
    radioButtons.forEach((radioButton) => {
      if (radioButton.checked) {
        radioButton.checked = false;
        radioButton.focused = false;
      }
    });
  }
  uncheckOtherRadioButtonsInGroup() {
    const radioButtons = this.queryButtons();
    const otherRadioButtons = radioButtons.filter((radioButton) => radioButton.guid !== this.guid);
    otherRadioButtons.forEach((otherRadioButton) => {
      if (otherRadioButton.checked) {
        otherRadioButton.checked = false;
        otherRadioButton.focused = false;
      }
    });
  }
  getTabIndex() {
    if (this.disabled) {
      return undefined;
    }
    return this.checked || this.isDefaultSelectable() ? 0 : -1;
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  mouseenter() {
    this.hovered = true;
  }
  mouseleave() {
    this.hovered = false;
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    this.rootNode = this.el.getRootNode();
    this.guid = this.el.id || `calcite-radio-button-${guid()}`;
    if (this.name) {
      this.checkLastRadioButton();
    }
    connectLabel(this);
    connectForm(this);
  }
  componentDidLoad() {
    if (this.focused && !this.disabled) {
      this.setFocus();
    }
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
    const tabIndex = this.getTabIndex();
    return (h(Host, { onClick: this.clickHandler, onKeyDown: this.handleKeyDown }, h("div", { "aria-checked": toAriaBoolean(this.checked), "aria-label": getLabelText(this), class: CSS.container, onBlur: this.onContainerBlur, onFocus: this.onContainerFocus, ref: this.setContainerEl, role: "radio", tabIndex: tabIndex }, h("div", { class: "radio" })), h(HiddenFormInputSlot, { component: this })));
  }
  static get is() { return "calcite-radio-button"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["radio-button.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["radio-button.css"]
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
      "focused": {
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
          "text": "The focused state of the component."
        },
        "attribute": "focused",
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
          "text": "The `id` of the component. When omitted, a globally unique identifier is used."
        },
        "attribute": "guid",
        "reflect": true
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
          "text": "When `true`, the component is not displayed and is not focusable or checkable."
        },
        "attribute": "hidden",
        "reflect": true,
        "defaultValue": "false"
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
          "text": "The hovered state of the component."
        },
        "attribute": "hovered",
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
          "text": "Specifies the name of the component, passed from the `calcite-radio-button-group` on form submission."
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
          "text": "When `true`, the component must have a value selected from the `calcite-radio-button-group` in order for the form to submit."
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
          "text": "Specifies the size of the component inherited from the `calcite-radio-button-group`."
        },
        "attribute": "scale",
        "reflect": true,
        "defaultValue": "\"m\""
      },
      "value": {
        "type": "any",
        "mutable": true,
        "complexType": {
          "original": "any",
          "resolved": "any",
          "references": {}
        },
        "required": true,
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
        "method": "calciteInternalRadioButtonBlur",
        "name": "calciteInternalRadioButtonBlur",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": "Fires when the radio button is blurred."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteRadioButtonChange",
        "name": "calciteRadioButtonChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires only when the radio button is checked.  This behavior is identical to the native HTML input element.\nSince this event does not fire when the radio button is unchecked, it's not recommended to attach a listener for this event\ndirectly on the element, but instead either attach it to a node that contains all of the radio buttons in the group\nor use the `calciteRadioButtonGroupChange` event if using this with `calcite-radio-button-group`."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteInternalRadioButtonCheckedChange",
        "name": "calciteInternalRadioButtonCheckedChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": "Fires when the checked property changes.  This is an internal event used for styling purposes only.\nUse calciteRadioButtonChange or calciteRadioButtonGroupChange for responding to changes in the checked value for forms."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteInternalRadioButtonFocus",
        "name": "calciteInternalRadioButtonFocus",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": "Fires when the radio button is focused."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
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
      },
      "emitCheckedChange": {
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
          "text": "",
          "tags": [{
              "name": "internal",
              "text": undefined
            }]
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "checked",
        "methodName": "checkedChanged"
      }, {
        "propName": "name",
        "methodName": "nameChanged"
      }];
  }
  static get listeners() {
    return [{
        "name": "pointerenter",
        "method": "mouseenter",
        "target": undefined,
        "capture": false,
        "passive": true
      }, {
        "name": "pointerleave",
        "method": "mouseleave",
        "target": undefined,
        "capture": false,
        "passive": true
      }];
  }
}
