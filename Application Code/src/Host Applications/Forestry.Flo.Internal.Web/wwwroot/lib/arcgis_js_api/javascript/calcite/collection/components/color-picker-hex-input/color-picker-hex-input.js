/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { hexChar, isLonghandHex, isValidHex, normalizeHex, rgbToHex } from "../color-picker/utils";
import Color from "color";
import { CSS } from "./resources";
import { focusElement } from "../../utils/dom";
import { TEXT } from "../color-picker/resources";
const DEFAULT_COLOR = Color();
export class ColorPickerHexInput {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Public Properties
    //
    //--------------------------------------------------------------------------
    /**
     * When `false`, an empty color (`null`) will be allowed as a `value`. Otherwise, a color value is enforced on the component.
     *
     * When `true`, a color value is enforced, and clearing the input or blurring will restore the last valid `value`. When `false`, an empty color (`null`) will be allowed as a `value`.
     */
    this.allowEmpty = false;
    /**
     * Accessible name for the Hex input.
     *
     * @default "Hex"
     */
    this.intlHex = TEXT.hex;
    /**
     * Accessible name for the Hex input when there is no color selected.
     *
     * @default "No color"
     */
    this.intlNoColor = TEXT.noColor;
    /** Specifies the size of the component. */
    this.scale = "m";
    /**
     * The Hex value.
     */
    this.value = normalizeHex(DEFAULT_COLOR.hex());
    this.onCalciteInternalInputBlur = () => {
      const node = this.inputNode;
      const inputValue = node.value;
      const hex = `#${inputValue}`;
      const willClearValue = this.allowEmpty && !inputValue;
      if (willClearValue || (isValidHex(hex) && isLonghandHex(hex))) {
        return;
      }
      // manipulating DOM directly since rerender doesn't update input value
      node.value =
        this.allowEmpty && !this.internalColor
          ? ""
          : this.formatForInternalInput(rgbToHex(this.internalColor.object()));
    };
    this.onInputChange = () => {
      this.internalSetValue(this.inputNode.value, this.value);
    };
    /**
     * The last valid/selected color. Used as a fallback if an invalid hex code is entered.
     */
    this.internalColor = DEFAULT_COLOR;
    this.previousNonNullValue = this.value;
    this.storeInputRef = (node) => {
      this.inputNode = node;
    };
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    const { allowEmpty, value } = this;
    if (value) {
      const normalized = normalizeHex(value);
      if (isValidHex(normalized)) {
        this.internalSetValue(normalized, normalized, false);
      }
      return;
    }
    if (allowEmpty) {
      this.internalSetValue(null, null, false);
    }
  }
  handleValueChange(value, oldValue) {
    this.internalSetValue(value, oldValue, false);
  }
  // using @Listen as a workaround for VDOM listener not firing
  onInputKeyDown(event) {
    const { altKey, ctrlKey, metaKey, shiftKey } = event;
    const { internalColor, value } = this;
    const { key } = event;
    if (key === "Tab" || key === "Enter") {
      this.onInputChange();
      return;
    }
    const isNudgeKey = key === "ArrowDown" || key === "ArrowUp";
    const oldValue = this.value;
    if (isNudgeKey) {
      if (!value) {
        this.internalSetValue(this.previousNonNullValue, oldValue);
        event.preventDefault();
        return;
      }
      const direction = key === "ArrowUp" ? 1 : -1;
      const bump = shiftKey ? 10 : 1;
      this.internalSetValue(normalizeHex(this.nudgeRGBChannels(internalColor, bump * direction).hex()), oldValue);
      event.preventDefault();
      return;
    }
    const withModifiers = altKey || ctrlKey || metaKey;
    const singleChar = key.length === 1;
    const validHexChar = hexChar.test(key);
    if (singleChar && !withModifiers && !validHexChar) {
      event.preventDefault();
    }
  }
  onPaste(event) {
    const hex = event.clipboardData.getData("text");
    if (isValidHex(hex)) {
      event.preventDefault();
      this.inputNode.value = hex.slice(1);
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  render() {
    const { intlHex, value } = this;
    const hexInputValue = this.formatForInternalInput(value);
    return (h("div", { class: CSS.container }, h("calcite-input", { class: CSS.input, label: intlHex, maxLength: 6, numberingSystem: this.numberingSystem, onCalciteInputChange: this.onInputChange, onCalciteInternalInputBlur: this.onCalciteInternalInputBlur, onKeyDown: this.handleKeyDown, onPaste: this.onPaste, prefixText: "#", ref: this.storeInputRef, scale: this.scale, value: hexInputValue }), hexInputValue ? (h("calcite-color-picker-swatch", { active: true, class: CSS.preview, color: `#${hexInputValue}`, scale: this.scale })) : null));
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    focusElement(this.inputNode);
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  internalSetValue(value, oldValue, emit = true) {
    if (value) {
      const normalized = normalizeHex(value);
      if (isValidHex(normalized)) {
        const { internalColor } = this;
        const changed = !internalColor || normalized !== normalizeHex(internalColor.hex());
        this.internalColor = Color(normalized);
        this.previousNonNullValue = normalized;
        this.value = normalized;
        if (changed && emit) {
          this.calciteColorPickerHexInputChange.emit();
        }
        return;
      }
    }
    else if (this.allowEmpty) {
      this.internalColor = null;
      this.value = null;
      if (emit) {
        this.calciteColorPickerHexInputChange.emit();
      }
      return;
    }
    this.value = oldValue;
  }
  formatForInternalInput(hex) {
    return hex ? hex.replace("#", "") : "";
  }
  nudgeRGBChannels(color, amount) {
    return Color.rgb(color.array().map((channel) => channel + amount));
  }
  handleKeyDown(event) {
    if (event.key === "Enter") {
      event.preventDefault();
    }
  }
  static get is() { return "calcite-color-picker-hex-input"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["color-picker-hex-input.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["color-picker-hex-input.css"]
    };
  }
  static get properties() {
    return {
      "allowEmpty": {
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
          "text": "When `false`, an empty color (`null`) will be allowed as a `value`. Otherwise, a color value is enforced on the component.\n\nWhen `true`, a color value is enforced, and clearing the input or blurring will restore the last valid `value`. When `false`, an empty color (`null`) will be allowed as a `value`."
        },
        "attribute": "allow-empty",
        "reflect": false,
        "defaultValue": "false"
      },
      "intlHex": {
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
              "text": "\"Hex\""
            }],
          "text": "Accessible name for the Hex input."
        },
        "attribute": "intl-hex",
        "reflect": false,
        "defaultValue": "TEXT.hex"
      },
      "intlNoColor": {
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
              "text": "\"No color\""
            }],
          "text": "Accessible name for the Hex input when there is no color selected."
        },
        "attribute": "intl-no-color",
        "reflect": false,
        "defaultValue": "TEXT.noColor"
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
          "text": "The Hex value."
        },
        "attribute": "value",
        "reflect": true,
        "defaultValue": "normalizeHex(DEFAULT_COLOR.hex())"
      },
      "numberingSystem": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "NumberingSystem",
          "resolved": "\"arab\" | \"arabext\" | \"bali\" | \"beng\" | \"deva\" | \"fullwide\" | \"gujr\" | \"guru\" | \"hanidec\" | \"khmr\" | \"knda\" | \"laoo\" | \"latn\" | \"limb\" | \"mlym\" | \"mong\" | \"mymr\" | \"orya\" | \"tamldec\" | \"telu\" | \"thai\" | \"tibt\"",
          "references": {
            "NumberingSystem": {
              "location": "import",
              "path": "../../utils/locale"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies the Unicode numeral system used by the component for localization."
        },
        "attribute": "numbering-system",
        "reflect": false
      }
    };
  }
  static get states() {
    return {
      "internalColor": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteColorPickerHexInputChange",
        "name": "calciteColorPickerHexInputChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Emitted when the hex value changes."
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
        "methodName": "handleValueChange"
      }];
  }
  static get listeners() {
    return [{
        "name": "keydown",
        "method": "onInputKeyDown",
        "target": undefined,
        "capture": true,
        "passive": false
      }];
  }
}
