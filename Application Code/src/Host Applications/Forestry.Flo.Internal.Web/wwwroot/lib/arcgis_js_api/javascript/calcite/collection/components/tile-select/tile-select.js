/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { guid } from "../../utils/guid";
import { CSS } from "./resources";
import { updateHostInteraction } from "../../utils/interactive";
/**
 * @slot - A slot for adding custom content.
 */
export class TileSelect {
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
    /** When `true`, the component is not displayed and is not focusable or checkable. */
    this.hidden = false;
    /** When `true`, displays an interactive input based on the `type` property. */
    this.inputEnabled = false;
    /** When `inputEnabled` is `true`, specifies the placement of the interactive input on the component. */
    this.inputAlignment = "start";
    /**
     * The selection mode of the component.
     *
     * Use radio for single selection, and checkbox for multiple selections.
     */
    this.type = "radio";
    /** Specifies the width of the component. */
    this.width = "auto";
    this.guid = `calcite-tile-select-${guid()}`;
    //--------------------------------------------------------------------------
    //
    //  State
    //
    //--------------------------------------------------------------------------
    /** The focused state of the tile-select. */
    this.focused = false;
  }
  checkedChanged(newChecked) {
    this.input.checked = newChecked;
  }
  nameChanged(newName) {
    this.input.name = newName;
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.input) === null || _a === void 0 ? void 0 : _a.setFocus();
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  checkboxChangeHandler(event) {
    const checkbox = event.target;
    if (checkbox === this.input) {
      this.checked = checkbox.checked;
    }
    event.stopPropagation();
    this.calciteTileSelectChange.emit();
  }
  checkboxFocusBlurHandler(event) {
    const checkbox = event.target;
    if (checkbox === this.input) {
      this.focused = event.detail;
    }
    event.stopPropagation();
  }
  radioButtonChangeHandler(event) {
    const radioButton = event.target;
    if (radioButton === this.input) {
      this.checked = radioButton.checked;
    }
    event.stopPropagation();
    this.calciteTileSelectChange.emit();
  }
  radioButtonCheckedChangeHandler(event) {
    const radioButton = event.target;
    if (radioButton === this.input) {
      this.checked = radioButton.checked;
    }
    event.stopPropagation();
  }
  radioButtonFocusBlurHandler(event) {
    const radioButton = event.target;
    if (radioButton === this.input) {
      this.focused = radioButton.focused;
    }
    event.stopPropagation();
  }
  click(event) {
    const target = event.target;
    const targets = ["calcite-tile", "calcite-tile-select"];
    if (targets.includes(target.localName)) {
      this.input.click();
    }
  }
  mouseenter() {
    if (this.input.localName === "calcite-radio-button") {
      this.input.hovered = true;
    }
    if (this.input.localName === "calcite-checkbox") {
      this.input.hovered = true;
    }
  }
  mouseleave() {
    if (this.input.localName === "calcite-radio-button") {
      this.input.hovered = false;
    }
    if (this.input.localName === "calcite-checkbox") {
      this.input.hovered = false;
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    this.renderInput();
  }
  disconnectedCallback() {
    this.input.parentNode.removeChild(this.input);
  }
  componentDidRender() {
    updateHostInteraction(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderInput() {
    this.input = document.createElement(this.type === "radio" ? "calcite-radio-button" : "calcite-checkbox");
    this.input.checked = this.checked;
    this.input.disabled = this.disabled;
    this.input.hidden = this.hidden;
    this.input.id = this.guid;
    this.input.label = this.heading || this.name || "";
    if (this.name) {
      this.input.name = this.name;
    }
    if (this.value) {
      this.input.value = this.value != null ? this.value.toString() : "";
    }
    this.el.insertAdjacentElement("beforeend", this.input);
  }
  render() {
    const { checked, description, disabled, focused, heading, icon, inputAlignment, inputEnabled, width } = this;
    return (h("div", { class: {
        checked,
        container: true,
        [CSS.description]: Boolean(description),
        [CSS.descriptionOnly]: Boolean(!heading && !icon && description),
        disabled,
        focused,
        [CSS.heading]: Boolean(heading),
        [CSS.headingOnly]: heading && !icon && !description,
        [CSS.icon]: Boolean(icon),
        [CSS.iconOnly]: !heading && icon && !description,
        [CSS.inputAlignmentEnd]: inputAlignment === "end",
        [CSS.inputAlignmentStart]: inputAlignment === "start",
        [CSS.inputEnabled]: inputEnabled,
        [CSS.largeVisual]: heading && icon && !description,
        [CSS.widthAuto]: width === "auto",
        [CSS.widthFull]: width === "full"
      } }, h("calcite-tile", { active: checked, description: description, embed: true, heading: heading, icon: icon }), h("slot", null)));
  }
  static get is() { return "calcite-tile-select"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["tile-select.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["tile-select.css"]
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
      "description": {
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
          "tags": [],
          "text": "A description for the component, which displays below the heading."
        },
        "attribute": "description",
        "reflect": true
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
      "heading": {
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
          "tags": [],
          "text": "The component header text, which displays between the icon and description."
        },
        "attribute": "heading",
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
      "icon": {
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
          "tags": [],
          "text": "Specifies an icon to display."
        },
        "attribute": "icon",
        "reflect": true
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
      "inputEnabled": {
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
          "text": "When `true`, displays an interactive input based on the `type` property."
        },
        "attribute": "input-enabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "inputAlignment": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Extract<\"end\" | \"start\", Alignment>",
          "resolved": "\"end\" | \"start\"",
          "references": {
            "Extract": {
              "location": "global"
            },
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
          "text": "When `inputEnabled` is `true`, specifies the placement of the interactive input on the component."
        },
        "attribute": "input-alignment",
        "reflect": true,
        "defaultValue": "\"start\""
      },
      "type": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "TileSelectType",
          "resolved": "\"checkbox\" | \"radio\"",
          "references": {
            "TileSelectType": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "The selection mode of the component.\n\nUse radio for single selection, and checkbox for multiple selections."
        },
        "attribute": "type",
        "reflect": true,
        "defaultValue": "\"radio\""
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
        "optional": true,
        "docs": {
          "tags": [],
          "text": "The component's value."
        },
        "attribute": "value",
        "reflect": false
      },
      "width": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Extract<\"auto\" | \"full\", Width>",
          "resolved": "\"auto\" | \"full\"",
          "references": {
            "Extract": {
              "location": "global"
            },
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
  static get states() {
    return {
      "focused": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteTileSelectChange",
        "name": "calciteTileSelectChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Emits a custom change event.\n\nFor checkboxes it emits when checked or unchecked.\n\nFor radios it only emits when checked."
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
        "propName": "checked",
        "methodName": "checkedChanged"
      }, {
        "propName": "name",
        "methodName": "nameChanged"
      }];
  }
  static get listeners() {
    return [{
        "name": "calciteCheckboxChange",
        "method": "checkboxChangeHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalCheckboxFocus",
        "method": "checkboxFocusBlurHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalCheckboxBlur",
        "method": "checkboxFocusBlurHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteRadioButtonChange",
        "method": "radioButtonChangeHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalRadioButtonCheckedChange",
        "method": "radioButtonCheckedChangeHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalRadioButtonFocus",
        "method": "radioButtonFocusBlurHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalRadioButtonBlur",
        "method": "radioButtonFocusBlurHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "click",
        "method": "click",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
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
