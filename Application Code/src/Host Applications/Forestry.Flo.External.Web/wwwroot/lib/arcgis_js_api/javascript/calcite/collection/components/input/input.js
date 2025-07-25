/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { getElementDir, getElementProp, getSlotted, isPrimaryPointerButton, setRequestedIcon } from "../../utils/dom";
import { CSS, INPUT_TYPE_ICONS, SLOTS, TEXT } from "./resources";
import { connectLabel, disconnectLabel, getLabelText } from "../../utils/label";
import { connectForm, disconnectForm, HiddenFormInputSlot, submitForm } from "../../utils/form";
import { defaultNumberingSystem, numberStringFormatter, disconnectLocalized, connectLocalized, updateEffectiveLocale } from "../../utils/locale";
import { numberKeys } from "../../utils/key";
import { isValidNumber, parseNumberString, sanitizeNumberString } from "../../utils/number";
import { CSS_UTILITY, TEXT as COMMON_TEXT } from "../../utils/resources";
import { decimalPlaces } from "../../utils/math";
import { createObserver } from "../../utils/observers";
import { updateHostInteraction } from "../../utils/interactive";
/**
 * @slot action - A slot for positioning a `calcite-button` next to the component.
 */
export class Input {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /** Specifies the text alignment of the component's value. */
    this.alignment = "start";
    /**
     * When `true`, the component is focused on page load.
     *
     * @mdn [autofocus](https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/autofocus)
     */
    this.autofocus = false;
    /**
     * When `true`, a clear button is displayed when the component has a value. The clear button shows by default for `"search"`, `"time"`, and `"date"` types, and will not display for the `"textarea"` type.
     */
    this.clearable = false;
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     *
     * @mdn [disabled](https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/disabled)
     */
    this.disabled = false;
    /**
     * When `true`, number values are displayed with a group separator corresponding to the language and country format.
     */
    this.groupSeparator = false;
    /**
     * When `true`, the component will not be visible.
     *
     * @mdn [hidden](https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/hidden)
     */
    this.hidden = false;
    /**
     * Accessible name when the component is loading.
     *
     * @default "Loading"
     */
    this.intlLoading = COMMON_TEXT.loading;
    /** When `true`, the icon will be flipped when the element direction is right-to-left (`"rtl"`). */
    this.iconFlipRtl = false;
    /** When `true`, a busy indicator is displayed. */
    this.loading = false;
    /**
     * When `true`, uses locale formatting for numbers.
     *
     * @internal
     */
    this.localeFormat = false;
    /** Specifies the placement of the buttons for `type="number"`. */
    this.numberButtonType = "vertical";
    /**
     * When `true`, the component's value can be read, but cannot be modified.
     *
     * @mdn [readOnly](https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/readonly)
     */
    this.readOnly = false;
    /** When `true`, the component must have a value in order for the form to submit. */
    this.required = false;
    /** Specifies the size of the component. */
    this.scale = "m";
    /** Specifies the status of the input field, which determines message and icons. */
    this.status = "idle";
    /**
     * @internal
     */
    this.editingEnabled = false;
    /**
     * Specifies the component type.
     *
     * Note that the following `type`s add type-specific icons by default: `"date"`, `"email"`, `"password"`, `"search"`, `"tel"`, `"time"`.
     */
    this.type = "text";
    /** The component's value. */
    this.value = "";
    /** keep track of the rendered child type */
    this.childElType = "input";
    this.previousValueOrigin = "initial";
    this.mutationObserver = createObserver("mutation", () => this.setDisabledAction());
    this.userChangedValue = false;
    //--------------------------------------------------------------------------
    //
    //  State
    //
    //--------------------------------------------------------------------------
    this.effectiveLocale = "";
    //--------------------------------------------------------------------------
    //
    //  Private Methods
    //
    //--------------------------------------------------------------------------
    this.keyDownHandler = (event) => {
      if (this.readOnly || this.disabled) {
        return;
      }
      if (this.isClearable && event.key === "Escape") {
        this.clearInputValue(event);
        event.preventDefault();
      }
      if (event.key === "Enter" && !event.defaultPrevented) {
        if (submitForm(this)) {
          event.preventDefault();
        }
      }
    };
    this.clearInputValue = (nativeEvent) => {
      this.setValue({
        committing: true,
        nativeEvent,
        origin: "user",
        value: ""
      });
    };
    this.emitChangeIfUserModified = () => {
      if (this.previousValueOrigin === "user" && this.value !== this.previousEmittedValue) {
        this.calciteInputChange.emit();
      }
      this.previousEmittedValue = this.value;
    };
    this.inputBlurHandler = () => {
      this.calciteInternalInputBlur.emit();
      this.emitChangeIfUserModified();
    };
    this.inputFocusHandler = (event) => {
      const slottedActionEl = getSlotted(this.el, "action");
      if (event.target !== slottedActionEl) {
        this.setFocus();
      }
      this.calciteInternalInputFocus.emit();
    };
    this.inputInputHandler = (nativeEvent) => {
      if (this.disabled || this.readOnly) {
        return;
      }
      this.setValue({
        nativeEvent,
        origin: "user",
        value: nativeEvent.target.value
      });
    };
    this.inputKeyDownHandler = (event) => {
      if (this.disabled || this.readOnly) {
        return;
      }
      if (event.key === "Enter") {
        this.emitChangeIfUserModified();
      }
    };
    this.inputNumberInputHandler = (nativeEvent) => {
      if (this.disabled || this.readOnly) {
        return;
      }
      const value = nativeEvent.target.value;
      numberStringFormatter.numberFormatOptions = {
        locale: this.effectiveLocale,
        numberingSystem: this.numberingSystem,
        useGrouping: this.groupSeparator
      };
      const delocalizedValue = numberStringFormatter.delocalize(value);
      if (nativeEvent.inputType === "insertFromPaste") {
        if (!isValidNumber(delocalizedValue)) {
          nativeEvent.preventDefault();
        }
        this.setValue({
          nativeEvent,
          origin: "user",
          value: parseNumberString(delocalizedValue)
        });
        this.childNumberEl.value = this.localizedValue;
      }
      else {
        this.setValue({
          nativeEvent,
          origin: "user",
          value: delocalizedValue
        });
      }
    };
    this.inputNumberKeyDownHandler = (event) => {
      if (this.type !== "number" || this.disabled || this.readOnly) {
        return;
      }
      if (event.key === "ArrowUp") {
        /* prevent default behavior of moving cursor to the beginning of the input when holding down ArrowUp */
        event.preventDefault();
        this.nudgeNumberValue("up", event);
        return;
      }
      if (event.key === "ArrowDown") {
        this.nudgeNumberValue("down", event);
        return;
      }
      const supportedKeys = [
        ...numberKeys,
        "ArrowLeft",
        "ArrowRight",
        "Backspace",
        "Delete",
        "Enter",
        "Escape",
        "Tab"
      ];
      if (event.altKey || event.ctrlKey || event.metaKey) {
        return;
      }
      const isShiftTabEvent = event.shiftKey && event.key === "Tab";
      if (supportedKeys.includes(event.key) && (!event.shiftKey || isShiftTabEvent)) {
        if (event.key === "Enter") {
          this.emitChangeIfUserModified();
        }
        return;
      }
      numberStringFormatter.numberFormatOptions = {
        locale: this.effectiveLocale,
        numberingSystem: this.numberingSystem,
        useGrouping: this.groupSeparator
      };
      if (event.key === numberStringFormatter.decimal) {
        if (!this.value && !this.childNumberEl.value) {
          return;
        }
        if (this.value && this.childNumberEl.value.indexOf(numberStringFormatter.decimal) === -1) {
          return;
        }
      }
      if (/[eE]/.test(event.key)) {
        if (!this.value && !this.childNumberEl.value) {
          return;
        }
        if (this.value && !/[eE]/.test(this.childNumberEl.value)) {
          return;
        }
      }
      if (event.key === "-") {
        if (!this.value && !this.childNumberEl.value) {
          return;
        }
        if (this.value && this.childNumberEl.value.split("-").length <= 2) {
          return;
        }
      }
      event.preventDefault();
    };
    this.nudgeNumberValue = (direction, nativeEvent) => {
      if ((nativeEvent instanceof KeyboardEvent && nativeEvent.repeat) || this.type !== "number") {
        return;
      }
      const inputMax = this.maxString ? parseFloat(this.maxString) : null;
      const inputMin = this.minString ? parseFloat(this.minString) : null;
      const valueNudgeDelayInMs = 100;
      this.incrementOrDecrementNumberValue(direction, inputMax, inputMin, nativeEvent);
      if (this.nudgeNumberValueIntervalId) {
        window.clearInterval(this.nudgeNumberValueIntervalId);
      }
      let firstValueNudge = true;
      this.nudgeNumberValueIntervalId = window.setInterval(() => {
        if (firstValueNudge) {
          firstValueNudge = false;
          return;
        }
        this.incrementOrDecrementNumberValue(direction, inputMax, inputMin, nativeEvent);
      }, valueNudgeDelayInMs);
    };
    this.numberButtonPointerUpAndOutHandler = () => {
      window.clearInterval(this.nudgeNumberValueIntervalId);
    };
    this.numberButtonPointerDownHandler = (event) => {
      if (!isPrimaryPointerButton(event)) {
        return;
      }
      event.preventDefault();
      const direction = event.target.dataset.adjustment;
      if (!this.disabled) {
        this.nudgeNumberValue(direction, event);
      }
    };
    this.hiddenInputChangeHandler = (event) => {
      if (event.target.name === this.name) {
        const hiddenInputValue = event.target.value;
        const value = this.type === "number"
          ? isValidNumber(hiddenInputValue)
            ? hiddenInputValue
            : ""
          : hiddenInputValue;
        this.setValue({
          value,
          origin: "direct"
        });
      }
      event.stopPropagation();
    };
    this.setChildElRef = (el) => {
      this.childEl = el;
    };
    this.setChildNumberElRef = (el) => {
      this.childNumberEl = el;
    };
    this.setInputValue = (newInputValue) => {
      if (this.type === "text" && !this.childEl) {
        return;
      }
      if (this.type === "number" && !this.childNumberEl) {
        return;
      }
      this[`child${this.type === "number" ? "Number" : ""}El`].value = newInputValue;
    };
    this.setPreviousEmittedValue = (newPreviousEmittedValue) => {
      this.previousEmittedValue =
        this.type === "number"
          ? isValidNumber(newPreviousEmittedValue)
            ? newPreviousEmittedValue
            : ""
          : newPreviousEmittedValue;
    };
    this.setPreviousValue = (newPreviousValue) => {
      this.previousValue =
        this.type === "number"
          ? isValidNumber(newPreviousValue)
            ? newPreviousValue
            : ""
          : newPreviousValue;
    };
    this.setValue = ({ committing = false, nativeEvent, origin, previousValue, value }) => {
      numberStringFormatter.numberFormatOptions = {
        locale: this.effectiveLocale,
        numberingSystem: this.numberingSystem,
        useGrouping: this.groupSeparator
      };
      if (this.type === "number") {
        const delocalizedValue = (this.numberingSystem && this.numberingSystem !== "latn") ||
          defaultNumberingSystem !== "latn"
          ? numberStringFormatter.delocalize(value)
          : value;
        const sanitizedValue = sanitizeNumberString(delocalizedValue);
        const newValue = (value && !sanitizedValue) || [".", "-"].includes(sanitizedValue)
          ? isValidNumber(this.previousValue)
            ? this.previousValue
            : ""
          : sanitizedValue;
        const newLocalizedValue = numberStringFormatter.localize(newValue);
        this.localizedValue = newLocalizedValue;
        this.userChangedValue = origin === "user" && this.value !== newValue;
        this.value = newValue;
        origin === "direct" && this.setInputValue(newLocalizedValue);
      }
      else {
        this.userChangedValue = origin === "user" && this.value !== value;
        this.value = value;
        origin === "direct" && this.setInputValue(value);
      }
      this.setPreviousValue(previousValue || this.value);
      this.previousValueOrigin = origin;
      if (nativeEvent) {
        const calciteInputInputEvent = this.calciteInputInput.emit({
          element: this.childEl,
          nativeEvent,
          value: this.value
        });
        if (calciteInputInputEvent.defaultPrevented) {
          this.value = this.previousValue;
          this.localizedValue =
            this.type === "number"
              ? numberStringFormatter.localize(this.previousValue)
              : this.previousValue;
        }
        else if (committing) {
          this.emitChangeIfUserModified();
        }
      }
    };
    this.inputKeyUpHandler = () => {
      window.clearInterval(this.nudgeNumberValueIntervalId);
    };
  }
  disabledWatcher() {
    this.setDisabledAction();
  }
  localeChanged() {
    updateEffectiveLocale(this);
  }
  /** watcher to update number-to-string for max */
  maxWatcher() {
    var _a;
    this.maxString = ((_a = this.max) === null || _a === void 0 ? void 0 : _a.toString()) || null;
  }
  /** watcher to update number-to-string for min */
  minWatcher() {
    var _a;
    this.minString = ((_a = this.min) === null || _a === void 0 ? void 0 : _a.toString()) || null;
  }
  valueWatcher(newValue, previousValue) {
    if (!this.userChangedValue) {
      this.setValue({
        origin: "direct",
        previousValue,
        value: newValue == null || newValue == ""
          ? ""
          : this.type === "number"
            ? isValidNumber(newValue)
              ? newValue
              : this.previousValue || ""
            : newValue
      });
      this.warnAboutInvalidNumberValue(newValue);
    }
    this.userChangedValue = false;
  }
  updateRequestedIcon() {
    this.requestedIcon = setRequestedIcon(INPUT_TYPE_ICONS, this.icon, this.type);
  }
  get isClearable() {
    return !this.isTextarea && (this.clearable || this.type === "search") && this.value.length > 0;
  }
  get isTextarea() {
    return this.childElType === "textarea";
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    var _a;
    connectLocalized(this);
    this.scale = getElementProp(this.el, "scale", this.scale);
    this.status = getElementProp(this.el, "status", this.status);
    this.inlineEditableEl = this.el.closest("calcite-inline-editable");
    if (this.inlineEditableEl) {
      this.editingEnabled = this.inlineEditableEl.editingEnabled || false;
    }
    connectLabel(this);
    connectForm(this);
    this.setPreviousEmittedValue(this.value);
    this.setPreviousValue(this.value);
    if (this.type === "number") {
      this.warnAboutInvalidNumberValue(this.value);
      this.setValue({
        origin: "connected",
        value: isValidNumber(this.value) ? this.value : ""
      });
    }
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el, { childList: true });
    this.setDisabledAction();
    this.el.addEventListener("calciteInternalHiddenInputChange", this.hiddenInputChangeHandler);
  }
  disconnectedCallback() {
    var _a;
    disconnectLabel(this);
    disconnectForm(this);
    disconnectLocalized(this);
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
    this.el.removeEventListener("calciteInternalHiddenInputChange", this.hiddenInputChangeHandler);
  }
  componentWillLoad() {
    var _a, _b;
    this.childElType = this.type === "textarea" ? "textarea" : "input";
    this.maxString = (_a = this.max) === null || _a === void 0 ? void 0 : _a.toString();
    this.minString = (_b = this.min) === null || _b === void 0 ? void 0 : _b.toString();
    this.requestedIcon = setRequestedIcon(INPUT_TYPE_ICONS, this.icon, this.type);
  }
  componentShouldUpdate(newValue, oldValue, property) {
    if (this.type === "number" && property === "value" && newValue && !isValidNumber(newValue)) {
      this.setValue({
        origin: "reset",
        value: oldValue
      });
      return false;
    }
    return true;
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
    var _a, _b;
    if (this.type === "number") {
      (_a = this.childNumberEl) === null || _a === void 0 ? void 0 : _a.focus();
    }
    else {
      (_b = this.childEl) === null || _b === void 0 ? void 0 : _b.focus();
    }
  }
  /** Selects all text of the component's `value`. */
  async selectText() {
    var _a, _b;
    if (this.type === "number") {
      (_a = this.childNumberEl) === null || _a === void 0 ? void 0 : _a.select();
    }
    else {
      (_b = this.childEl) === null || _b === void 0 ? void 0 : _b.select();
    }
  }
  onLabelClick() {
    this.setFocus();
  }
  incrementOrDecrementNumberValue(direction, inputMax, inputMin, nativeEvent) {
    const { value } = this;
    const inputStep = this.step === "any" ? 1 : Math.abs(this.step || 1);
    const inputVal = value && value !== "" ? parseFloat(value) : 0;
    const adjustment = direction === "up" ? 1 : -1;
    const nudgedValue = inputVal + inputStep * adjustment;
    const finalValue = (typeof inputMin === "number" && !isNaN(inputMin) && nudgedValue < inputMin) ||
      (typeof inputMax === "number" && !isNaN(inputMax) && nudgedValue > inputMax)
      ? inputVal
      : nudgedValue;
    const inputValPlaces = decimalPlaces(inputVal);
    const inputStepPlaces = decimalPlaces(inputStep);
    this.setValue({
      committing: true,
      nativeEvent,
      origin: "user",
      value: finalValue.toFixed(Math.max(inputValPlaces, inputStepPlaces))
    });
  }
  onFormReset() {
    this.setValue({
      origin: "reset",
      value: this.defaultValue
    });
  }
  syncHiddenFormInput(input) {
    var _a, _b, _c, _d;
    if (this.type === "number") {
      input.type = "number";
      input.min = (_b = (_a = this.min) === null || _a === void 0 ? void 0 : _a.toString(10)) !== null && _b !== void 0 ? _b : "";
      input.max = (_d = (_c = this.max) === null || _c === void 0 ? void 0 : _c.toString(10)) !== null && _d !== void 0 ? _d : "";
    }
    else if (this.type === "text") {
      input.type = "text";
      if (this.minLength != null) {
        input.minLength = this.minLength;
      }
      if (this.maxLength != null) {
        input.maxLength = this.maxLength;
      }
    }
    else if (this.type === "password") {
      input.type = "password";
    }
  }
  setDisabledAction() {
    const slottedActionEl = getSlotted(this.el, "action");
    if (!slottedActionEl) {
      return;
    }
    this.disabled
      ? slottedActionEl.setAttribute("disabled", "")
      : slottedActionEl.removeAttribute("disabled");
  }
  warnAboutInvalidNumberValue(value) {
    if (this.type === "number" && value && !isValidNumber(value)) {
      console.warn(`The specified value "${value}" cannot be parsed, or is out of range.`);
    }
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    const dir = getElementDir(this.el);
    const loader = (h("div", { class: CSS.loader }, h("calcite-progress", { label: this.intlLoading, type: "indeterminate" })));
    const inputClearButton = (h("button", { "aria-label": this.intlClear || TEXT.clear, class: CSS.clearButton, disabled: this.disabled || this.readOnly, onClick: this.clearInputValue, tabIndex: -1, type: "button" }, h("calcite-icon", { icon: "x", scale: "s" })));
    const iconEl = (h("calcite-icon", { class: CSS.inputIcon, flipRtl: this.iconFlipRtl, icon: this.requestedIcon, scale: "s" }));
    const isHorizontalNumberButton = this.numberButtonType === "horizontal";
    const numberButtonsHorizontalUp = (h("button", { "aria-hidden": "true", class: {
        [CSS.numberButtonItem]: true,
        [CSS.buttonItemHorizontal]: isHorizontalNumberButton
      }, "data-adjustment": "up", disabled: this.disabled || this.readOnly, onPointerDown: this.numberButtonPointerDownHandler, onPointerOut: this.numberButtonPointerUpAndOutHandler, onPointerUp: this.numberButtonPointerUpAndOutHandler, tabIndex: -1, type: "button" }, h("calcite-icon", { icon: "chevron-up", scale: "s" })));
    const numberButtonsHorizontalDown = (h("button", { "aria-hidden": "true", class: {
        [CSS.numberButtonItem]: true,
        [CSS.buttonItemHorizontal]: isHorizontalNumberButton
      }, "data-adjustment": "down", disabled: this.disabled || this.readOnly, onPointerDown: this.numberButtonPointerDownHandler, onPointerOut: this.numberButtonPointerUpAndOutHandler, onPointerUp: this.numberButtonPointerUpAndOutHandler, tabIndex: -1, type: "button" }, h("calcite-icon", { icon: "chevron-down", scale: "s" })));
    const numberButtonsVertical = (h("div", { class: CSS.numberButtonWrapper }, numberButtonsHorizontalUp, numberButtonsHorizontalDown));
    const prefixText = h("div", { class: CSS.prefix }, this.prefixText);
    const suffixText = h("div", { class: CSS.suffix }, this.suffixText);
    const localeNumberInput = this.type === "number" ? (h("input", { "aria-label": getLabelText(this), autofocus: this.autofocus ? true : null, defaultValue: this.defaultValue, disabled: this.disabled ? true : null, enterKeyHint: this.el.enterKeyHint, inputMode: this.el.inputMode, key: "localized-input", maxLength: this.maxLength, minLength: this.minLength, name: undefined, onBlur: this.inputBlurHandler, onFocus: this.inputFocusHandler, onInput: this.inputNumberInputHandler, onKeyDown: this.inputNumberKeyDownHandler, onKeyUp: this.inputKeyUpHandler, placeholder: this.placeholder || "", readOnly: this.readOnly, ref: this.setChildNumberElRef, type: "text", value: this.localizedValue })) : null;
    const childEl = this.type !== "number"
      ? [
        h(this.childElType, { "aria-label": getLabelText(this), autofocus: this.autofocus ? true : null, class: {
            [CSS.editingEnabled]: this.editingEnabled,
            [CSS.inlineChild]: !!this.inlineEditableEl
          }, defaultValue: this.defaultValue, disabled: this.disabled ? true : null, enterKeyHint: this.el.enterKeyHint, inputMode: this.el.inputMode, max: this.maxString, maxLength: this.maxLength, min: this.minString, minLength: this.minLength, name: this.name, onBlur: this.inputBlurHandler, onFocus: this.inputFocusHandler, onInput: this.inputInputHandler, onKeyDown: this.inputKeyDownHandler, onKeyUp: this.inputKeyUpHandler, placeholder: this.placeholder || "", readOnly: this.readOnly, ref: this.setChildElRef, required: this.required ? true : null, step: this.step, tabIndex: this.disabled || (this.inlineEditableEl && !this.editingEnabled) ? -1 : null, type: this.type, value: this.value }),
        this.isTextarea ? (h("div", { class: CSS.resizeIconWrapper }, h("calcite-icon", { icon: "chevron-down", scale: "s" }))) : null
      ]
      : null;
    return (h(Host, { onClick: this.inputFocusHandler, onKeyDown: this.keyDownHandler }, h("div", { class: { [CSS.inputWrapper]: true, [CSS_UTILITY.rtl]: dir === "rtl" } }, this.type === "number" && this.numberButtonType === "horizontal" && !this.readOnly
      ? numberButtonsHorizontalDown
      : null, this.prefixText ? prefixText : null, h("div", { class: CSS.wrapper }, localeNumberInput, childEl, this.isClearable ? inputClearButton : null, this.requestedIcon ? iconEl : null, this.loading ? loader : null), h("div", { class: CSS.actionWrapper }, h("slot", { name: SLOTS.action })), this.type === "number" && this.numberButtonType === "vertical" && !this.readOnly
      ? numberButtonsVertical
      : null, this.suffixText ? suffixText : null, this.type === "number" && this.numberButtonType === "horizontal" && !this.readOnly
      ? numberButtonsHorizontalUp
      : null, h(HiddenFormInputSlot, { component: this }))));
  }
  static get is() { return "calcite-input"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["input.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["input.css"]
    };
  }
  static get properties() {
    return {
      "alignment": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Position",
          "resolved": "\"end\" | \"start\"",
          "references": {
            "Position": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the text alignment of the component's value."
        },
        "attribute": "alignment",
        "reflect": true,
        "defaultValue": "\"start\""
      },
      "autofocus": {
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
              "name": "mdn",
              "text": "[autofocus](https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/autofocus)"
            }],
          "text": "When `true`, the component is focused on page load."
        },
        "attribute": "autofocus",
        "reflect": true,
        "defaultValue": "false"
      },
      "clearable": {
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
          "text": "When `true`, a clear button is displayed when the component has a value. The clear button shows by default for `\"search\"`, `\"time\"`, and `\"date\"` types, and will not display for the `\"textarea\"` type."
        },
        "attribute": "clearable",
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
          "tags": [{
              "name": "mdn",
              "text": "[disabled](https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/disabled)"
            }],
          "text": "When `true`, interaction is prevented and the component is displayed with lower opacity."
        },
        "attribute": "disabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "groupSeparator": {
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
          "text": "When `true`, number values are displayed with a group separator corresponding to the language and country format."
        },
        "attribute": "group-separator",
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
          "tags": [{
              "name": "mdn",
              "text": "[hidden](https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/hidden)"
            }],
          "text": "When `true`, the component will not be visible."
        },
        "attribute": "hidden",
        "reflect": true,
        "defaultValue": "false"
      },
      "icon": {
        "type": "any",
        "mutable": false,
        "complexType": {
          "original": "string | boolean",
          "resolved": "boolean | string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "When `true`, shows a default recommended icon. Alternatively, pass a Calcite UI Icon name to display a specific icon."
        },
        "attribute": "icon",
        "reflect": true
      },
      "intlClear": {
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
          "text": "Accessible name for the component's clear button."
        },
        "attribute": "intl-clear",
        "reflect": false
      },
      "intlLoading": {
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
              "name": "default",
              "text": "\"Loading\""
            }],
          "text": "Accessible name when the component is loading."
        },
        "attribute": "intl-loading",
        "reflect": false,
        "defaultValue": "COMMON_TEXT.loading"
      },
      "iconFlipRtl": {
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
          "text": "When `true`, the icon will be flipped when the element direction is right-to-left (`\"rtl\"`)."
        },
        "attribute": "icon-flip-rtl",
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
          "tags": [],
          "text": "Accessible name for the component."
        },
        "attribute": "label",
        "reflect": false
      },
      "loading": {
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
          "text": "When `true`, a busy indicator is displayed."
        },
        "attribute": "loading",
        "reflect": true,
        "defaultValue": "false"
      },
      "locale": {
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
              "name": "deprecated",
              "text": "set the global `lang` attribute on the element instead."
            }, {
              "name": "mdn",
              "text": "[lang](https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/lang)"
            }],
          "text": "BCP 47 language tag for desired language and country format"
        },
        "attribute": "locale",
        "reflect": false
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
        "reflect": true
      },
      "localeFormat": {
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
          "text": "When `true`, uses locale formatting for numbers."
        },
        "attribute": "locale-format",
        "reflect": false,
        "defaultValue": "false"
      },
      "max": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "number",
          "resolved": "number",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [{
              "name": "mdn",
              "text": "[max](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input#max)"
            }],
          "text": "Specifies the maximum value for type \"number\"."
        },
        "attribute": "max",
        "reflect": true
      },
      "min": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "number",
          "resolved": "number",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [{
              "name": "mdn",
              "text": "[min](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input#min)"
            }],
          "text": "Specifies the minimum value for `type=\"number\"`."
        },
        "attribute": "min",
        "reflect": true
      },
      "maxlength": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "number",
          "resolved": "number",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use `maxLength` instead."
            }],
          "text": "Specifies the maximum length of text for the component's value."
        },
        "attribute": "maxlength",
        "reflect": true
      },
      "maxLength": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "number",
          "resolved": "number",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [{
              "name": "mdn",
              "text": "[maxlength](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input#maxlength)"
            }],
          "text": "Specifies the maximum length of text for the component's value."
        },
        "attribute": "max-length",
        "reflect": true
      },
      "minLength": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "number",
          "resolved": "number",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [{
              "name": "mdn",
              "text": "[minlength](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input#minlength)"
            }],
          "text": "Specifies the minimum length of text for the component's value."
        },
        "attribute": "min-length",
        "reflect": true
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
          "tags": [{
              "name": "mdn",
              "text": "[name](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input#name)"
            }],
          "text": "Specifies the name of the component on form submission."
        },
        "attribute": "name",
        "reflect": true
      },
      "numberButtonType": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "InputPlacement",
          "resolved": "\"horizontal\" | \"none\" | \"vertical\"",
          "references": {
            "InputPlacement": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies the placement of the buttons for `type=\"number\"`."
        },
        "attribute": "number-button-type",
        "reflect": true,
        "defaultValue": "\"vertical\""
      },
      "placeholder": {
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
              "name": "mdn",
              "text": "[placeholder](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input#placeholder)"
            }],
          "text": "Specifies placeholder text for the component."
        },
        "attribute": "placeholder",
        "reflect": false
      },
      "prefixText": {
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
          "text": "Adds text to the start of the component."
        },
        "attribute": "prefix-text",
        "reflect": false
      },
      "readOnly": {
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
              "name": "mdn",
              "text": "[readOnly](https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/readonly)"
            }],
          "text": "When `true`, the component's value can be read, but cannot be modified."
        },
        "attribute": "read-only",
        "reflect": true,
        "defaultValue": "false"
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
      "step": {
        "type": "any",
        "mutable": false,
        "complexType": {
          "original": "number | \"any\"",
          "resolved": "\"any\" | number",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [{
              "name": "mdn",
              "text": "[step](https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/step)"
            }],
          "text": "Specifies the granularity the component's value must adhere to."
        },
        "attribute": "step",
        "reflect": true
      },
      "suffixText": {
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
          "text": "Adds text to the end of the component."
        },
        "attribute": "suffix-text",
        "reflect": false
      },
      "editingEnabled": {
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
        "attribute": "editing-enabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "type": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "| \"color\"\n    | \"date\"\n    | \"datetime-local\"\n    | \"email\"\n    | \"file\"\n    | \"image\"\n    | \"month\"\n    | \"number\"\n    | \"password\"\n    | \"search\"\n    | \"tel\"\n    | \"text\"\n    | \"textarea\"\n    | \"time\"\n    | \"url\"\n    | \"week\"",
          "resolved": "\"color\" | \"date\" | \"datetime-local\" | \"email\" | \"file\" | \"image\" | \"month\" | \"number\" | \"password\" | \"search\" | \"tel\" | \"text\" | \"textarea\" | \"time\" | \"url\" | \"week\"",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the component type.\n\nNote that the following `type`s add type-specific icons by default: `\"date\"`, `\"email\"`, `\"password\"`, `\"search\"`, `\"tel\"`, `\"time\"`."
        },
        "attribute": "type",
        "reflect": true,
        "defaultValue": "\"text\""
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
          "text": "The component's value."
        },
        "attribute": "value",
        "reflect": false,
        "defaultValue": "\"\""
      }
    };
  }
  static get states() {
    return {
      "effectiveLocale": {},
      "localizedValue": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteInternalInputFocus",
        "name": "calciteInternalInputFocus",
        "bubbles": true,
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
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteInternalInputBlur",
        "name": "calciteInternalInputBlur",
        "bubbles": true,
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
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteInputInput",
        "name": "calciteInputInput",
        "bubbles": true,
        "cancelable": true,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires each time a new `value` is typed.\n\n**Note:**: The `el` and `value` event payload properties are deprecated, use the event's `target`/`currentTarget` instead."
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
      }, {
        "method": "calciteInputChange",
        "name": "calciteInputChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires each time a new `value` is typed and committed."
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
      "selectText": {
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
          "text": "Selects all text of the component's `value`.",
          "tags": []
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "disabled",
        "methodName": "disabledWatcher"
      }, {
        "propName": "locale",
        "methodName": "localeChanged"
      }, {
        "propName": "max",
        "methodName": "maxWatcher"
      }, {
        "propName": "min",
        "methodName": "minWatcher"
      }, {
        "propName": "value",
        "methodName": "valueWatcher"
      }, {
        "propName": "icon",
        "methodName": "updateRequestedIcon"
      }, {
        "propName": "type",
        "methodName": "updateRequestedIcon"
      }];
  }
}
