/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { Fragment, h } from "@stencil/core";
import { focusElement } from "../../utils/dom";
import { connectLabel, disconnectLabel } from "../../utils/label";
import { afterConnectDefaultValueSet, connectForm, disconnectForm, HiddenFormInputSlot } from "../../utils/form";
import { CSS } from "./resources";
import { createObserver } from "../../utils/observers";
import { updateHostInteraction } from "../../utils/interactive";
function isOption(optionOrGroup) {
  return optionOrGroup.tagName === "CALCITE-OPTION";
}
function isOptionGroup(optionOrGroup) {
  return optionOrGroup.tagName === "CALCITE-OPTION-GROUP";
}
/**
 * @slot - A slot for adding `calcite-option`s.
 */
export class Select {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    /**
     * When `true`, the component must have a value in order for the form to submit.
     *
     * @internal
     */
    this.required = false;
    /**
     * Specifies the size of the component.
     */
    this.scale = "m";
    /** The component's `selectedOption` value. */
    this.value = null;
    /**
     * Specifies the width of the component.
     */
    this.width = "auto";
    this.componentToNativeEl = new Map();
    this.mutationObserver = createObserver("mutation", () => this.populateInternalSelect());
    this.handleInternalSelectChange = () => {
      const selected = this.selectEl.selectedOptions[0];
      this.selectFromNativeOption(selected);
      requestAnimationFrame(() => this.emitChangeEvent());
    };
    this.populateInternalSelect = () => {
      const optionsAndGroups = Array.from(this.el.children).filter((child) => child.tagName === "CALCITE-OPTION" || child.tagName === "CALCITE-OPTION-GROUP");
      this.clearInternalSelect();
      optionsAndGroups.forEach((optionOrGroup) => { var _a; return (_a = this.selectEl) === null || _a === void 0 ? void 0 : _a.append(this.toNativeElement(optionOrGroup)); });
    };
    this.storeSelectRef = (node) => {
      this.selectEl = node;
      this.populateInternalSelect();
      const selected = this.selectEl.selectedOptions[0];
      this.selectFromNativeOption(selected);
    };
    this.emitChangeEvent = () => {
      this.calciteSelectChange.emit();
    };
  }
  valueHandler(value) {
    const items = this.el.querySelectorAll("calcite-option");
    items.forEach((item) => (item.selected = item.value === value));
  }
  selectedOptionHandler(selectedOption) {
    this.value = selectedOption === null || selectedOption === void 0 ? void 0 : selectedOption.value;
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    var _a;
    const { el } = this;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(el, {
      subtree: true,
      childList: true
    });
    connectLabel(this);
    connectForm(this);
  }
  disconnectedCallback() {
    var _a;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
    disconnectLabel(this);
    disconnectForm(this);
  }
  componentDidLoad() {
    var _a, _b;
    afterConnectDefaultValueSet(this, (_b = (_a = this.selectedOption) === null || _a === void 0 ? void 0 : _a.value) !== null && _b !== void 0 ? _b : "");
  }
  componentDidRender() {
    updateHostInteraction(this);
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    focusElement(this.selectEl);
  }
  handleOptionOrGroupChange(event) {
    event.stopPropagation();
    const optionOrGroup = event.target;
    const nativeEl = this.componentToNativeEl.get(optionOrGroup);
    if (!nativeEl) {
      return;
    }
    this.updateNativeElement(optionOrGroup, nativeEl);
    if (isOption(optionOrGroup) && optionOrGroup.selected) {
      this.deselectAllExcept(optionOrGroup);
      this.selectedOption = optionOrGroup;
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  onLabelClick() {
    this.setFocus();
  }
  updateNativeElement(optionOrGroup, nativeOptionOrGroup) {
    nativeOptionOrGroup.disabled = optionOrGroup.disabled;
    nativeOptionOrGroup.label = optionOrGroup.label;
    if (isOption(optionOrGroup)) {
      const option = nativeOptionOrGroup;
      option.selected = optionOrGroup.selected;
      option.value = optionOrGroup.value;
      // need to set innerText for mobile
      // see https://stackoverflow.com/questions/35021620/ios-safari-not-showing-all-options-for-select-menu/41749701
      option.innerText = optionOrGroup.label;
    }
  }
  clearInternalSelect() {
    this.componentToNativeEl.forEach((value) => value.remove());
    this.componentToNativeEl.clear();
  }
  selectFromNativeOption(nativeOption) {
    if (!nativeOption) {
      return;
    }
    let futureSelected;
    this.componentToNativeEl.forEach((nativeOptionOrGroup, optionOrGroup) => {
      if (isOption(optionOrGroup) && nativeOptionOrGroup === nativeOption) {
        optionOrGroup.selected = true;
        futureSelected = optionOrGroup;
        this.deselectAllExcept(optionOrGroup);
      }
    });
    if (futureSelected) {
      this.selectedOption = futureSelected;
    }
  }
  toNativeElement(optionOrGroup) {
    if (isOption(optionOrGroup)) {
      const option = document.createElement("option");
      this.updateNativeElement(optionOrGroup, option);
      this.componentToNativeEl.set(optionOrGroup, option);
      return option;
    }
    if (isOptionGroup(optionOrGroup)) {
      const group = document.createElement("optgroup");
      this.updateNativeElement(optionOrGroup, group);
      Array.from(optionOrGroup.children).forEach((option) => {
        const nativeOption = this.toNativeElement(option);
        group.append(nativeOption);
        this.componentToNativeEl.set(optionOrGroup, nativeOption);
      });
      this.componentToNativeEl.set(optionOrGroup, group);
      return group;
    }
    throw new Error("unsupported element child provided");
  }
  deselectAllExcept(except) {
    this.el.querySelectorAll("calcite-option").forEach((option) => {
      if (option === except) {
        return;
      }
      option.selected = false;
    });
  }
  //--------------------------------------------------------------------------
  //
  //  Render Methods
  //
  //--------------------------------------------------------------------------
  renderChevron() {
    return (h("div", { class: CSS.iconContainer }, h("calcite-icon", { class: CSS.icon, icon: "chevron-down", scale: "s" })));
  }
  render() {
    return (h(Fragment, null, h("select", { "aria-label": this.label, class: CSS.select, disabled: this.disabled, onChange: this.handleInternalSelectChange, ref: this.storeSelectRef }, h("slot", null)), this.renderChevron(), h(HiddenFormInputSlot, { component: this })));
  }
  static get is() { return "calcite-select"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["select.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["select.css"]
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
      "label": {
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
          "text": "The component's `selectedOption` value."
        },
        "attribute": "value",
        "reflect": false,
        "defaultValue": "null"
      },
      "selectedOption": {
        "type": "unknown",
        "mutable": true,
        "complexType": {
          "original": "HTMLCalciteOptionElement",
          "resolved": "HTMLCalciteOptionElement",
          "references": {
            "HTMLCalciteOptionElement": {
              "location": "global"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "readonly",
              "text": undefined
            }],
          "text": "The component's selected option `HTMLElement`."
        }
      },
      "width": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Width",
          "resolved": "\"auto\" | \"full\" | \"half\"",
          "references": {
            "Width": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the width of the component."
        },
        "attribute": "width",
        "reflect": true,
        "defaultValue": "\"auto\""
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteSelectChange",
        "name": "calciteSelectChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the `selectedOption` changes."
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
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "value",
        "methodName": "valueHandler"
      }, {
        "propName": "selectedOption",
        "methodName": "selectedOptionHandler"
      }];
  }
  static get listeners() {
    return [{
        "name": "calciteInternalOptionChange",
        "method": "handleOptionOrGroupChange",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalOptionGroupChange",
        "method": "handleOptionOrGroupChange",
        "target": undefined,
        "capture": false,
        "passive": false
      }];
  }
}
