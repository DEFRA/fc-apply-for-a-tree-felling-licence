/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host, Build } from "@stencil/core";
import { getLocaleData, getValueAsDateRange } from "../date-picker/utils";
import { dateFromRange, inRange, dateFromISO, dateToISO, setEndOfDay, dateFromLocalizedString, datePartsFromLocalizedString } from "../../utils/date";
import { TEXT } from "../date-picker/resources";
import { CSS } from "./resources";
import { connectLabel, disconnectLabel, getLabelText } from "../../utils/label";
import { connectForm, disconnectForm, HiddenFormInputSlot, submitForm } from "../../utils/form";
import { FloatingCSS, connectFloatingUI, disconnectFloatingUI, defaultMenuPlacement, filterComputedPlacements, reposition, updateAfterClose } from "../../utils/floating-ui";
import { updateHostInteraction } from "../../utils/interactive";
import { toAriaBoolean } from "../../utils/dom";
import { connectOpenCloseComponent, disconnectOpenCloseComponent } from "../../utils/openCloseComponent";
import { connectLocalized, disconnectLocalized, numberStringFormatter } from "../../utils/locale";
import { numberKeys } from "../../utils/key";
export class InputDatePicker {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Public Properties
    //
    //--------------------------------------------------------------------------
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    /**
     * When `true`, the component's value can be read, but controls are not accessible and the value cannot be modified.
     *
     * @mdn [readOnly](https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/readonly)
     */
    this.readOnly = false;
    /** Selected date as a string in ISO format (YYYY-MM-DD) */
    this.value = "";
    /**
     * When `true`, the component is active.
     *
     * @deprecated use `open` instead.
     */
    this.active = false;
    /** When `true`, displays the `calcite-date-picker` component. */
    this.open = false;
    /**
     * Accessible name for the component's previous month button.
     *
     * @default "Previous month"
     */
    this.intlPrevMonth = TEXT.prevMonth;
    /**
     * Accessible name for the component's next month button.
     *
     * @default "Next month"
     */
    this.intlNextMonth = TEXT.nextMonth;
    /**
     * Accessible name for the component's year input.
     *
     * @default "Year"
     */
    this.intlYear = TEXT.year;
    /** Specifies the size of the component. */
    this.scale = "m";
    /**
     * Specifies the placement of the `calcite-date-picker` relative to the component.
     *
     * @default "bottom-start"
     */
    this.placement = defaultMenuPlacement;
    /** When `true`, activates a range for the component. */
    this.range = false;
    /**
     * When `true`, the component must have a value in order for the form to submit.
     *
     * @internal
     */
    this.required = false;
    /**
     * Determines the type of positioning to use for the overlaid content.
     *
     * Using `"absolute"` will work for most cases. The component will be positioned inside of overflowing parent containers and will affect the container's layout.
     *
     * `"fixed"` should be used to escape an overflowing parent container, or when the reference element's `position` CSS property is `"fixed"`.
     *
     */
    this.overlayPositioning = "absolute";
    /**
     * When `true`, disables the default behavior on the third click of narrowing or extending the range.
     * Instead starts a new range.
     */
    this.proximitySelectionDisabled = false;
    /** Defines the layout of the component. */
    this.layout = "horizontal";
    this.calciteInternalInputInputHandler = (event) => {
      const target = event.target;
      const value = target.value;
      const { year } = datePartsFromLocalizedString(value, this.localeData);
      if (year && year.length < 4) {
        return;
      }
      const date = dateFromLocalizedString(value, this.localeData);
      if (inRange(date, this.min, this.max)) {
        this.datePickerActiveDate = date;
      }
    };
    this.calciteInternalInputBlurHandler = () => {
      this.commitValue();
    };
    this.effectiveLocale = "";
    this.focusedInput = "start";
    this.globalAttributes = {};
    this.userChangedValue = false;
    this.openTransitionProp = "opacity";
    //--------------------------------------------------------------------------
    //
    //  Private Methods
    //
    //--------------------------------------------------------------------------
    this.setFilteredPlacements = () => {
      const { el, flipPlacements } = this;
      this.filteredFlipPlacements = flipPlacements
        ? filterComputedPlacements(flipPlacements, el)
        : null;
    };
    this.setTransitionEl = (el) => {
      this.transitionEl = el;
      connectOpenCloseComponent(this);
    };
    this.setStartInput = (el) => {
      this.startInput = el;
    };
    this.setEndInput = (el) => {
      this.endInput = el;
    };
    this.deactivate = () => {
      this.open = false;
    };
    this.keyDownHandler = (event) => {
      var _a, _b;
      const { defaultPrevented, key } = event;
      if (key === "Enter" && !defaultPrevented) {
        this.commitValue();
        if (this.shouldFocusRangeEnd()) {
          (_a = this.endInput) === null || _a === void 0 ? void 0 : _a.setFocus();
        }
        else if (this.shouldFocusRangeStart()) {
          (_b = this.startInput) === null || _b === void 0 ? void 0 : _b.setFocus();
        }
        if (submitForm(this)) {
          event.preventDefault();
        }
      }
      else if (key === "Escape" && !defaultPrevented) {
        this.active = false;
        this.open = false;
        event.preventDefault();
      }
    };
    this.startInputFocus = () => {
      if (!this.readOnly) {
        this.open = true;
      }
      this.focusedInput = "start";
    };
    this.endInputFocus = () => {
      if (!this.readOnly) {
        this.open = true;
      }
      this.focusedInput = "end";
    };
    this.setFloatingEl = (el) => {
      if (el) {
        this.floatingEl = el;
        connectFloatingUI(this, this.referenceEl, this.floatingEl);
      }
    };
    this.setStartWrapper = (el) => {
      this.startWrapper = el;
      this.setReferenceEl();
    };
    this.setEndWrapper = (el) => {
      this.endWrapper = el;
      this.setReferenceEl();
    };
    /**
     * Event handler for when the selected date changes
     *
     * @param event CalciteDatePicker custom change event
     */
    this.handleDateChange = (event) => {
      if (this.range) {
        return;
      }
      event.stopPropagation();
      this.setValue(event.detail);
      this.localizeInputValues();
    };
    this.handleDateRangeChange = (event) => {
      var _a, _b;
      if (!this.range || !event.detail) {
        return;
      }
      event.stopPropagation();
      const { startDate, endDate } = event.detail;
      this.setRangeValue([startDate, endDate]);
      this.localizeInputValues();
      if (this.shouldFocusRangeEnd()) {
        (_a = this.endInput) === null || _a === void 0 ? void 0 : _a.setFocus();
      }
      else if (this.shouldFocusRangeStart()) {
        (_b = this.startInput) === null || _b === void 0 ? void 0 : _b.setFocus();
      }
    };
    this.setInputValue = (newValue, input = "start") => {
      const inputEl = this[`${input}Input`];
      if (!inputEl) {
        return;
      }
      inputEl.value = newValue;
    };
    this.setRangeValue = (value) => {
      if (!this.range) {
        return;
      }
      const { value: oldValue } = this;
      const oldValueIsArray = Array.isArray(oldValue);
      const valueIsArray = Array.isArray(value);
      const newStartDate = valueIsArray ? value[0] : "";
      const newStartDateISO = valueIsArray ? dateToISO(newStartDate) : "";
      const newEndDate = valueIsArray ? value[1] : "";
      const newEndDateISO = valueIsArray ? dateToISO(newEndDate) : "";
      const newValue = newStartDateISO || newEndDateISO ? [newStartDateISO, newEndDateISO] : "";
      if (newValue === oldValue) {
        return;
      }
      this.userChangedValue = true;
      this.value = newValue;
      this.valueAsDate = newValue ? getValueAsDateRange(newValue) : undefined;
      this.start = newStartDateISO;
      this.end = newEndDateISO;
      const eventDetail = {
        startDate: newStartDate,
        endDate: newEndDate ? setEndOfDay(newEndDate) : null
      };
      const changeEvent = this.calciteInputDatePickerChange.emit();
      const rangeChangeEvent = this.calciteDatePickerRangeChange.emit(eventDetail);
      if ((changeEvent && changeEvent.defaultPrevented) ||
        (rangeChangeEvent && rangeChangeEvent.defaultPrevented)) {
        this.value = oldValue;
        if (oldValueIsArray) {
          this.setInputValue(oldValue[0], "start");
          this.setInputValue(oldValue[1], "end");
        }
        else {
          this.value = oldValue;
          this.setInputValue(oldValue);
        }
      }
    };
    this.setValue = (value) => {
      if (this.range) {
        return;
      }
      const oldValue = this.value;
      const newValue = dateToISO(value);
      if (newValue === oldValue) {
        return;
      }
      this.userChangedValue = true;
      this.valueAsDate = newValue ? dateFromISO(newValue) : undefined;
      this.value = newValue || "";
      const changeEvent = this.calciteInputDatePickerChange.emit();
      const deprecatedDatePickerChangeEvent = this.calciteDatePickerChange.emit(value);
      if (changeEvent.defaultPrevented || deprecatedDatePickerChangeEvent.defaultPrevented) {
        this.value = oldValue;
        this.setInputValue(oldValue);
      }
    };
    this.commonDateSeparators = [".", "-", "/"];
    this.formatNumerals = (value) => value
      ? value
        .split("")
        .map((char) => 
      // convert common separators to the locale's
      this.commonDateSeparators.includes(char)
        ? this.localeData.separator
        : numberKeys.includes(char)
          ? numberStringFormatter.numberFormatter.format(Number(char))
          : char)
        .join("")
      : "";
  }
  handleDisabledAndReadOnlyChange(value) {
    if (!value) {
      this.open = false;
    }
  }
  valueWatcher(newValue) {
    if (!this.userChangedValue) {
      if (Array.isArray(newValue)) {
        this.valueAsDate = getValueAsDateRange(newValue);
        this.start = newValue[0];
        this.end = newValue[1];
      }
      else if (newValue) {
        this.valueAsDate = dateFromISO(newValue);
      }
      else {
        this.valueAsDate = undefined;
      }
      this.localizeInputValues();
    }
    this.userChangedValue = false;
  }
  valueAsDateWatcher(valueAsDate) {
    this.datePickerActiveDate = valueAsDate;
  }
  flipPlacementsHandler() {
    this.setFilteredPlacements();
    this.reposition(true);
  }
  onMinChanged(min) {
    if (min) {
      this.minAsDate = dateFromISO(min);
    }
  }
  onMaxChanged(max) {
    if (max) {
      this.maxAsDate = dateFromISO(max);
    }
  }
  activeHandler(value) {
    this.open = value;
  }
  openHandler(value) {
    this.active = value;
    if (this.disabled || this.readOnly) {
      if (!value) {
        updateAfterClose(this.floatingEl);
      }
      this.open = false;
      return;
    }
    if (value) {
      this.reposition(true);
    }
    else {
      updateAfterClose(this.floatingEl);
    }
  }
  overlayPositioningHandler() {
    this.reposition(true);
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  calciteDaySelectHandler() {
    if (this.shouldFocusRangeStart() || this.shouldFocusRangeEnd()) {
      return;
    }
    this.open = false;
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.startInput) === null || _a === void 0 ? void 0 : _a.setFocus();
  }
  /**
   * Updates the position of the component.
   *
   * @param delayed
   */
  async reposition(delayed = false) {
    const { floatingEl, referenceEl, placement, overlayPositioning, filteredFlipPlacements } = this;
    return reposition(this, {
      floatingEl,
      referenceEl,
      overlayPositioning,
      placement,
      flipPlacements: filteredFlipPlacements,
      type: "menu"
    }, delayed);
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    connectLocalized(this);
    const isOpen = this.active || this.open;
    isOpen && this.activeHandler(isOpen);
    isOpen && this.openHandler(isOpen);
    if (Array.isArray(this.value)) {
      this.valueAsDate = getValueAsDateRange(this.value);
      this.start = this.value[0];
      this.end = this.value[1];
    }
    else if (this.value) {
      try {
        this.valueAsDate = dateFromISO(this.value);
      }
      catch (error) {
        this.warnAboutInvalidValue(this.value);
        this.value = "";
      }
      this.start = "";
      this.end = "";
    }
    if (this.start) {
      this.startAsDate = dateFromISO(this.start);
    }
    if (this.end) {
      this.endAsDate = setEndOfDay(dateFromISO(this.end));
    }
    if (this.min) {
      this.minAsDate = dateFromISO(this.min);
    }
    if (this.max) {
      this.maxAsDate = dateFromISO(this.max);
    }
    connectLabel(this);
    connectForm(this);
    connectOpenCloseComponent(this);
    this.setFilteredPlacements();
    this.reposition(true);
    numberStringFormatter.numberFormatOptions = {
      numberingSystem: this.numberingSystem,
      locale: this.effectiveLocale,
      useGrouping: false
    };
  }
  async componentWillLoad() {
    await this.loadLocaleData();
    this.onMinChanged(this.min);
    this.onMaxChanged(this.max);
  }
  componentDidLoad() {
    this.localizeInputValues();
    this.reposition(true);
  }
  disconnectedCallback() {
    disconnectLabel(this);
    disconnectForm(this);
    disconnectFloatingUI(this, this.referenceEl, this.floatingEl);
    disconnectOpenCloseComponent(this);
    disconnectLocalized(this);
  }
  componentDidRender() {
    updateHostInteraction(this);
  }
  render() {
    var _a, _b;
    const { disabled, effectiveLocale, numberingSystem, readOnly } = this;
    numberStringFormatter.numberFormatOptions = {
      numberingSystem,
      locale: effectiveLocale,
      useGrouping: false
    };
    return (h(Host, { onBlur: this.deactivate, onKeyDown: this.keyDownHandler, role: "application" }, this.localeData && (h("div", { "aria-expanded": toAriaBoolean(this.open), class: "input-container", role: "application" }, h("div", { class: "input-wrapper", ref: this.setStartWrapper }, h("calcite-input", { class: `input ${this.layout === "vertical" && this.range ? `no-bottom-border` : ``}`, disabled: disabled, icon: "calendar", label: getLabelText(this), lang: effectiveLocale, "number-button-type": "none", numberingSystem: numberingSystem, onCalciteInputInput: this.calciteInternalInputInputHandler, onCalciteInternalInputBlur: this.calciteInternalInputBlurHandler, onCalciteInternalInputFocus: this.startInputFocus, placeholder: (_a = this.localeData) === null || _a === void 0 ? void 0 : _a.placeholder, readOnly: readOnly, ref: this.setStartInput, scale: this.scale, type: "text" })), h("div", { "aria-hidden": toAriaBoolean(!this.open), class: {
        [CSS.menu]: true,
        [CSS.menuActive]: this.open
      }, ref: this.setFloatingEl }, h("div", { class: {
        ["calendar-picker-wrapper"]: true,
        ["calendar-picker-wrapper--end"]: this.focusedInput === "end",
        [FloatingCSS.animation]: true,
        [FloatingCSS.animationActive]: this.open
      }, ref: this.setTransitionEl }, h("calcite-date-picker", { activeDate: this.datePickerActiveDate, activeRange: this.focusedInput, endAsDate: this.endAsDate, headingLevel: this.headingLevel, intlNextMonth: this.intlNextMonth, intlPrevMonth: this.intlPrevMonth, intlYear: this.intlYear, lang: effectiveLocale, max: this.max, maxAsDate: this.maxAsDate, min: this.min, minAsDate: this.minAsDate, numberingSystem: numberingSystem, onCalciteDatePickerChange: this.handleDateChange, onCalciteDatePickerRangeChange: this.handleDateRangeChange, proximitySelectionDisabled: this.proximitySelectionDisabled, range: this.range, scale: this.scale, startAsDate: this.startAsDate, tabIndex: 0, valueAsDate: this.valueAsDate }))), this.range && this.layout === "horizontal" && (h("div", { class: "horizontal-arrow-container" }, h("calcite-icon", { flipRtl: true, icon: "arrow-right", scale: "s" }))), this.range && this.layout === "vertical" && this.scale !== "s" && (h("div", { class: "vertical-arrow-container" }, h("calcite-icon", { icon: "arrow-down", scale: "s" }))), this.range && (h("div", { class: "input-wrapper", ref: this.setEndWrapper }, h("calcite-input", { class: {
        input: true,
        "border-top-color-one": this.layout === "vertical" && this.range
      }, disabled: disabled, icon: "calendar", lang: effectiveLocale, "number-button-type": "none", numberingSystem: numberingSystem, onCalciteInputInput: this.calciteInternalInputInputHandler, onCalciteInternalInputBlur: this.calciteInternalInputBlurHandler, onCalciteInternalInputFocus: this.endInputFocus, placeholder: (_b = this.localeData) === null || _b === void 0 ? void 0 : _b.placeholder, readOnly: readOnly, ref: this.setEndInput, scale: this.scale, type: "text" }))))), h(HiddenFormInputSlot, { component: this })));
  }
  setReferenceEl() {
    const { focusedInput, layout, endWrapper, startWrapper } = this;
    this.referenceEl =
      focusedInput === "end" || layout === "vertical"
        ? endWrapper || startWrapper
        : startWrapper || endWrapper;
    connectFloatingUI(this, this.referenceEl, this.floatingEl);
  }
  onLabelClick() {
    this.setFocus();
  }
  onBeforeOpen() {
    this.calciteInputDatePickerBeforeOpen.emit();
  }
  onOpen() {
    this.calciteInputDatePickerOpen.emit();
  }
  onBeforeClose() {
    this.calciteInputDatePickerBeforeClose.emit();
  }
  onClose() {
    this.calciteInputDatePickerClose.emit();
  }
  commitValue() {
    const { focusedInput, value } = this;
    const focusedInputName = `${focusedInput}Input`;
    const focusedInputValue = this[focusedInputName].value;
    const date = dateFromLocalizedString(focusedInputValue, this.localeData);
    const dateAsISO = dateToISO(date);
    const valueIsArray = Array.isArray(value);
    if (this.range) {
      const focusedInputValueIndex = focusedInput === "start" ? 0 : 1;
      if (valueIsArray) {
        if (dateAsISO === value[focusedInputValueIndex]) {
          return;
        }
        if (date) {
          this.setRangeValue([
            focusedInput === "start" ? date : dateFromISO(value[0]),
            focusedInput === "end" ? date : dateFromISO(value[1])
          ]);
          this.localizeInputValues();
        }
        else {
          this.setRangeValue([
            focusedInput === "end" && dateFromISO(value[0]),
            focusedInput === "start" && dateFromISO(value[1])
          ]);
        }
      }
      else {
        if (date) {
          this.setRangeValue([
            focusedInput === "start" ? date : dateFromISO(value[0]),
            focusedInput === "end" ? date : dateFromISO(value[1])
          ]);
          this.localizeInputValues();
        }
      }
    }
    else {
      if (dateAsISO === value) {
        return;
      }
      this.setValue(date);
      this.localizeInputValues();
    }
  }
  startWatcher(start) {
    this.startAsDate = dateFromISO(start);
  }
  endWatcher(end) {
    this.endAsDate = end ? setEndOfDay(dateFromISO(end)) : dateFromISO(end);
  }
  async loadLocaleData() {
    if (!Build.isBrowser) {
      return;
    }
    this.localeData = await getLocaleData(this.effectiveLocale);
  }
  shouldFocusRangeStart() {
    return !!(this.endAsDate &&
      !this.startAsDate &&
      this.focusedInput === "end" &&
      this.startInput);
  }
  shouldFocusRangeEnd() {
    return !!(this.startAsDate &&
      !this.endAsDate &&
      this.focusedInput === "start" &&
      this.endInput);
  }
  localizeInputValues() {
    const date = dateFromRange(this.range ? this.startAsDate : this.valueAsDate, this.minAsDate, this.maxAsDate);
    const endDate = this.range
      ? dateFromRange(this.endAsDate, this.minAsDate, this.maxAsDate)
      : null;
    const localizedDate = date && this.formatNumerals(date.toLocaleDateString(this.effectiveLocale));
    const localizedEndDate = endDate && this.formatNumerals(endDate.toLocaleDateString(this.effectiveLocale));
    localizedDate && this.setInputValue(localizedDate, "start");
    this.range && localizedEndDate && this.setInputValue(localizedEndDate, "end");
  }
  warnAboutInvalidValue(value) {
    console.warn(`The specified value "${value}" does not conform to the required format, "yyyy-MM-dd".`);
  }
  static get is() { return "calcite-input-date-picker"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["input-date-picker.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["input-date-picker.css"]
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
          "text": "When `true`, the component's value can be read, but controls are not accessible and the value cannot be modified."
        },
        "attribute": "read-only",
        "reflect": true,
        "defaultValue": "false"
      },
      "value": {
        "type": "string",
        "mutable": true,
        "complexType": {
          "original": "string | string[]",
          "resolved": "string | string[]",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Selected date as a string in ISO format (YYYY-MM-DD)"
        },
        "attribute": "value",
        "reflect": false,
        "defaultValue": "\"\""
      },
      "flipPlacements": {
        "type": "unknown",
        "mutable": false,
        "complexType": {
          "original": "EffectivePlacement[]",
          "resolved": "Placement[]",
          "references": {
            "EffectivePlacement": {
              "location": "import",
              "path": "../../utils/floating-ui"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Defines the available placements that can be used when a flip occurs."
        }
      },
      "headingLevel": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "HeadingLevel",
          "resolved": "1 | 2 | 3 | 4 | 5 | 6",
          "references": {
            "HeadingLevel": {
              "location": "import",
              "path": "../functional/Heading"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the number at which section headings should start."
        },
        "attribute": "heading-level",
        "reflect": true
      },
      "valueAsDate": {
        "type": "unknown",
        "mutable": true,
        "complexType": {
          "original": "Date | Date[]",
          "resolved": "Date | Date[]",
          "references": {
            "Date": {
              "location": "global"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "The component's value as a full date object."
        }
      },
      "startAsDate": {
        "type": "unknown",
        "mutable": true,
        "complexType": {
          "original": "Date",
          "resolved": "Date",
          "references": {
            "Date": {
              "location": "global"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use `valueAsDate` instead."
            }],
          "text": "The component's start date as a full date object."
        }
      },
      "endAsDate": {
        "type": "unknown",
        "mutable": true,
        "complexType": {
          "original": "Date",
          "resolved": "Date",
          "references": {
            "Date": {
              "location": "global"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use `valueAsDate` instead."
            }],
          "text": "The component's end date as a full date object."
        }
      },
      "minAsDate": {
        "type": "unknown",
        "mutable": true,
        "complexType": {
          "original": "Date",
          "resolved": "Date",
          "references": {
            "Date": {
              "location": "global"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies the earliest allowed date as a full date object."
        }
      },
      "maxAsDate": {
        "type": "unknown",
        "mutable": true,
        "complexType": {
          "original": "Date",
          "resolved": "Date",
          "references": {
            "Date": {
              "location": "global"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies the latest allowed date as a full date object."
        }
      },
      "min": {
        "type": "string",
        "mutable": true,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies the earliest allowed date (\"yyyy-mm-dd\")."
        },
        "attribute": "min",
        "reflect": false
      },
      "max": {
        "type": "string",
        "mutable": true,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies the latest allowed date (\"yyyy-mm-dd\")."
        },
        "attribute": "max",
        "reflect": false
      },
      "active": {
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
              "name": "deprecated",
              "text": "use `open` instead."
            }],
          "text": "When `true`, the component is active."
        },
        "attribute": "active",
        "reflect": true,
        "defaultValue": "false"
      },
      "open": {
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
          "text": "When `true`, displays the `calcite-date-picker` component."
        },
        "attribute": "open",
        "reflect": true,
        "defaultValue": "false"
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
      "intlPrevMonth": {
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
              "text": "\"Previous month\""
            }],
          "text": "Accessible name for the component's previous month button."
        },
        "attribute": "intl-prev-month",
        "reflect": false,
        "defaultValue": "TEXT.prevMonth"
      },
      "intlNextMonth": {
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
              "text": "\"Next month\""
            }],
          "text": "Accessible name for the component's next month button."
        },
        "attribute": "intl-next-month",
        "reflect": false,
        "defaultValue": "TEXT.nextMonth"
      },
      "intlYear": {
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
              "text": "\"Year\""
            }],
          "text": "Accessible name for the component's year input."
        },
        "attribute": "intl-year",
        "reflect": false,
        "defaultValue": "TEXT.year"
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
        "optional": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "set the global `lang` attribute on the element instead."
            }, {
              "name": "mdn",
              "text": "[lang](https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/lang)"
            }],
          "text": "Specifies the BCP 47 language tag for the desired language and country format."
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
          "text": "Specifies the Unicode numeral system used by the component for localization. This property cannot be dynamically changed."
        },
        "attribute": "numbering-system",
        "reflect": true
      },
      "scale": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "\"s\" | \"m\" | \"l\"",
          "resolved": "\"l\" | \"m\" | \"s\"",
          "references": {}
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
      "placement": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "MenuPlacement",
          "resolved": "\"bottom\" | \"bottom-end\" | \"bottom-leading\" | \"bottom-start\" | \"bottom-trailing\" | \"top\" | \"top-end\" | \"top-leading\" | \"top-start\" | \"top-trailing\"",
          "references": {
            "MenuPlacement": {
              "location": "import",
              "path": "../../utils/floating-ui"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"bottom-start\""
            }],
          "text": "Specifies the placement of the `calcite-date-picker` relative to the component."
        },
        "attribute": "placement",
        "reflect": true,
        "defaultValue": "defaultMenuPlacement"
      },
      "range": {
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
          "text": "When `true`, activates a range for the component."
        },
        "attribute": "range",
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
      "start": {
        "type": "string",
        "mutable": true,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use `value` instead."
            }],
          "text": "The component's start date."
        },
        "attribute": "start",
        "reflect": true
      },
      "end": {
        "type": "string",
        "mutable": true,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use `value` instead."
            }],
          "text": "The component's end date."
        },
        "attribute": "end",
        "reflect": true
      },
      "overlayPositioning": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "OverlayPositioning",
          "resolved": "\"absolute\" | \"fixed\"",
          "references": {
            "OverlayPositioning": {
              "location": "import",
              "path": "../../utils/floating-ui"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Determines the type of positioning to use for the overlaid content.\n\nUsing `\"absolute\"` will work for most cases. The component will be positioned inside of overflowing parent containers and will affect the container's layout.\n\n`\"fixed\"` should be used to escape an overflowing parent container, or when the reference element's `position` CSS property is `\"fixed\"`."
        },
        "attribute": "overlay-positioning",
        "reflect": true,
        "defaultValue": "\"absolute\""
      },
      "proximitySelectionDisabled": {
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
          "text": "When `true`, disables the default behavior on the third click of narrowing or extending the range.\nInstead starts a new range."
        },
        "attribute": "proximity-selection-disabled",
        "reflect": false,
        "defaultValue": "false"
      },
      "layout": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "\"horizontal\" | \"vertical\"",
          "resolved": "\"horizontal\" | \"vertical\"",
          "references": {}
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
      }
    };
  }
  static get states() {
    return {
      "datePickerActiveDate": {},
      "effectiveLocale": {},
      "focusedInput": {},
      "globalAttributes": {},
      "localeData": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteDatePickerChange",
        "name": "calciteDatePickerChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use `calciteInputDatePickerChange` instead."
            }],
          "text": "Fires when a user changes the date."
        },
        "complexType": {
          "original": "Date",
          "resolved": "Date",
          "references": {
            "Date": {
              "location": "global"
            }
          }
        }
      }, {
        "method": "calciteDatePickerRangeChange",
        "name": "calciteDatePickerRangeChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "see",
              "text": "[DateRangeChange](https://github.com/Esri/calcite-components/blob/master/src/components/date-picker/interfaces.ts#L1)"
            }, {
              "name": "deprecated",
              "text": "use `calciteInputDatePickerChange` instead."
            }],
          "text": "Fires when a user changes the date range."
        },
        "complexType": {
          "original": "DateRangeChange",
          "resolved": "DateRangeChange",
          "references": {
            "DateRangeChange": {
              "location": "import",
              "path": "../date-picker/interfaces"
            }
          }
        }
      }, {
        "method": "calciteInputDatePickerChange",
        "name": "calciteInputDatePickerChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component's value changes."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteInputDatePickerBeforeClose",
        "name": "calciteInputDatePickerBeforeClose",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is requested to be closed and before the closing transition begins."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteInputDatePickerClose",
        "name": "calciteInputDatePickerClose",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is closed and animation is complete."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteInputDatePickerBeforeOpen",
        "name": "calciteInputDatePickerBeforeOpen",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is added to the DOM but not rendered, and before the opening transition begins."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteInputDatePickerOpen",
        "name": "calciteInputDatePickerOpen",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is open and animation is complete."
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
      "reposition": {
        "complexType": {
          "signature": "(delayed?: boolean) => Promise<void>",
          "parameters": [{
              "tags": [{
                  "name": "param",
                  "text": "delayed"
                }],
              "text": ""
            }],
          "references": {
            "Promise": {
              "location": "global"
            }
          },
          "return": "Promise<void>"
        },
        "docs": {
          "text": "Updates the position of the component.",
          "tags": [{
              "name": "param",
              "text": "delayed"
            }]
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "disabled",
        "methodName": "handleDisabledAndReadOnlyChange"
      }, {
        "propName": "readOnly",
        "methodName": "handleDisabledAndReadOnlyChange"
      }, {
        "propName": "value",
        "methodName": "valueWatcher"
      }, {
        "propName": "valueAsDate",
        "methodName": "valueAsDateWatcher"
      }, {
        "propName": "flipPlacements",
        "methodName": "flipPlacementsHandler"
      }, {
        "propName": "min",
        "methodName": "onMinChanged"
      }, {
        "propName": "max",
        "methodName": "onMaxChanged"
      }, {
        "propName": "active",
        "methodName": "activeHandler"
      }, {
        "propName": "open",
        "methodName": "openHandler"
      }, {
        "propName": "overlayPositioning",
        "methodName": "overlayPositioningHandler"
      }, {
        "propName": "layout",
        "methodName": "setReferenceEl"
      }, {
        "propName": "focusedInput",
        "methodName": "setReferenceEl"
      }, {
        "propName": "start",
        "methodName": "startWatcher"
      }, {
        "propName": "end",
        "methodName": "endWatcher"
      }, {
        "propName": "effectiveLocale",
        "methodName": "loadLocaleData"
      }];
  }
  static get listeners() {
    return [{
        "name": "calciteDaySelect",
        "method": "calciteDaySelectHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }];
  }
}
