/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { proxyCustomElement, HTMLElement, createEvent, h, Host, Build } from '@stencil/core/internal/client/index.js';
import { T as TEXT, g as getValueAsDateRange, a as getLocaleData, d as defineCustomElement$8 } from './date-picker.js';
import { d as datePartsFromLocalizedString, a as dateFromLocalizedString, i as inRange, b as dateToISO, s as setEndOfDay, c as dateFromISO, e as dateFromRange } from './date.js';
import { c as connectLabel, d as disconnectLabel, g as getLabelText } from './label2.js';
import { s as submitForm, c as connectForm, d as disconnectForm, H as HiddenFormInputSlot } from './form.js';
import { d as defaultMenuPlacement, f as filterComputedPlacements, c as connectFloatingUI, u as updateAfterClose, r as reposition, a as disconnectFloatingUI, F as FloatingCSS } from './floating-ui.js';
import { u as updateHostInteraction } from './interactive.js';
import { t as toAriaBoolean } from './dom.js';
import { c as connectOpenCloseComponent, d as disconnectOpenCloseComponent } from './openCloseComponent.js';
import { n as numberStringFormatter, c as connectLocalized, d as disconnectLocalized } from './locale.js';
import { n as numberKeys } from './key.js';
import { d as defineCustomElement$7 } from './date-picker-day.js';
import { d as defineCustomElement$6 } from './date-picker-month.js';
import { d as defineCustomElement$5 } from './date-picker-month-header.js';
import { d as defineCustomElement$4 } from './icon.js';
import { d as defineCustomElement$3 } from './input.js';
import { d as defineCustomElement$2 } from './progress.js';

const CSS = {
  menu: "menu-container",
  menuActive: "menu-container--active"
};

const inputDatePickerCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:host{--calcite-icon-size:1rem;--calcite-spacing-eighth:0.125rem;--calcite-spacing-quarter:0.25rem;--calcite-spacing-half:0.5rem;--calcite-spacing-three-quarters:0.75rem;--calcite-spacing:1rem;--calcite-spacing-plus-quarter:1.25rem;--calcite-spacing-plus-half:1.5rem;--calcite-spacing-double:2rem;--calcite-menu-min-width:10rem;--calcite-header-min-height:3rem;--calcite-footer-min-height:3rem}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{position:relative;display:inline-block;inline-size:100%;overflow:visible;vertical-align:top;--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)}:host .menu-container .calcite-floating-ui-anim{position:relative;transition:var(--calcite-floating-ui-transition);transition-property:transform, visibility, opacity;opacity:0;box-shadow:0 0 16px 0 rgba(0, 0, 0, 0.16);z-index:1;border-radius:0.25rem}:host .menu-container[data-placement^=bottom] .calcite-floating-ui-anim{transform:translateY(-5px)}:host .menu-container[data-placement^=top] .calcite-floating-ui-anim{transform:translateY(5px)}:host .menu-container[data-placement^=left] .calcite-floating-ui-anim{transform:translateX(5px)}:host .menu-container[data-placement^=right] .calcite-floating-ui-anim{transform:translateX(-5px)}:host .menu-container[data-placement] .calcite-floating-ui-anim--active{opacity:1;transform:translate(0)}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}.calendar-picker-wrapper{position:static;inline-size:100%;--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow);transform:translate3d(0, 0, 0)}.input-wrapper{position:relative}:host([range]) .input-container{display:flex}:host([range]) .input-wrapper{flex:1 1 auto}:host([range]) .horizontal-arrow-container{display:flex;align-items:center;border-width:1px;border-inline-start-width:0px;border-inline-end-width:0px;border-style:solid;border-color:var(--calcite-ui-border-input);background-color:var(--calcite-ui-background);padding-block:0px;padding-inline:0.25rem}:host([range][layout=vertical]) .input-wrapper{inline-size:100%}:host([range][layout=vertical]) .input-container{flex-direction:column;align-items:flex-start}:host([range][layout=vertical]) .calendar-picker-wrapper--end{transform:translate3d(0, 0, 0)}:host([range][layout=vertical]) .vertical-arrow-container{inset-block-start:1.5rem;position:absolute;z-index:1;margin-inline:1px;background-color:var(--calcite-ui-foreground-1);padding-inline:0.625rem;inset-inline-start:0}:host([scale=s][range]:not([layout=vertical])) .calendar-picker-wrapper{inline-size:216px}:host([scale=m][range]:not([layout=vertical])) .calendar-picker-wrapper{inline-size:286px}:host([scale=l][range]:not([layout=vertical])) .calendar-picker-wrapper{inline-size:398px}.menu-container{display:block;position:absolute;z-index:900;pointer-events:none;visibility:hidden}.menu-container .calcite-floating-ui-anim{position:relative;transition:var(--calcite-floating-ui-transition);transition-property:transform, visibility, opacity;opacity:0;box-shadow:0 0 16px 0 rgba(0, 0, 0, 0.16);z-index:1;border-radius:0.25rem}.menu-container[data-placement^=bottom] .calcite-floating-ui-anim{transform:translateY(-5px)}.menu-container[data-placement^=top] .calcite-floating-ui-anim{transform:translateY(5px)}.menu-container[data-placement^=left] .calcite-floating-ui-anim{transform:translateX(5px)}.menu-container[data-placement^=right] .calcite-floating-ui-anim{transform:translateX(-5px)}.menu-container[data-placement] .calcite-floating-ui-anim--active{opacity:1;transform:translate(0)}:host([open]) .menu-container{visibility:visible}.menu-container--active{visibility:visible}.input .calcite-input__wrapper{margin-block-start:0px}:host([range][layout=vertical][scale=m]) .vertical-arrow-container{inset-block-start:1.5rem;padding-inline-start:0.75rem}:host([range][layout=vertical][scale=m]) .vertical-arrow-container calcite-icon{block-size:0.75rem;inline-size:0.75rem;min-inline-size:0px}:host([range][layout=vertical][scale=l]) .vertical-arrow-container{inset-block-start:2.25rem;padding-inline:0.875rem}:host([range][layout=vertical][open]) .vertical-arrow-container{display:none}::slotted(input[slot=hidden-form-input]){margin:0 !important;opacity:0 !important;outline:none !important;padding:0 !important;position:absolute !important;inset:0 !important;transform:none !important;-webkit-appearance:none !important;z-index:-1 !important}";

const InputDatePicker = /*@__PURE__*/ proxyCustomElement(class extends HTMLElement {
  constructor() {
    super();
    this.__registerHost();
    this.__attachShadow();
    this.calciteDatePickerChange = createEvent(this, "calciteDatePickerChange", 6);
    this.calciteDatePickerRangeChange = createEvent(this, "calciteDatePickerRangeChange", 6);
    this.calciteInputDatePickerChange = createEvent(this, "calciteInputDatePickerChange", 6);
    this.calciteInputDatePickerBeforeClose = createEvent(this, "calciteInputDatePickerBeforeClose", 6);
    this.calciteInputDatePickerClose = createEvent(this, "calciteInputDatePickerClose", 6);
    this.calciteInputDatePickerBeforeOpen = createEvent(this, "calciteInputDatePickerBeforeOpen", 6);
    this.calciteInputDatePickerOpen = createEvent(this, "calciteInputDatePickerOpen", 6);
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
  get el() { return this; }
  static get watchers() { return {
    "disabled": ["handleDisabledAndReadOnlyChange"],
    "readOnly": ["handleDisabledAndReadOnlyChange"],
    "value": ["valueWatcher"],
    "valueAsDate": ["valueAsDateWatcher"],
    "flipPlacements": ["flipPlacementsHandler"],
    "min": ["onMinChanged"],
    "max": ["onMaxChanged"],
    "active": ["activeHandler"],
    "open": ["openHandler"],
    "overlayPositioning": ["overlayPositioningHandler"],
    "layout": ["setReferenceEl"],
    "focusedInput": ["setReferenceEl"],
    "start": ["startWatcher"],
    "end": ["endWatcher"],
    "effectiveLocale": ["loadLocaleData"]
  }; }
  static get style() { return inputDatePickerCss; }
}, [1, "calcite-input-date-picker", {
    "disabled": [516],
    "readOnly": [516, "read-only"],
    "value": [1025],
    "flipPlacements": [16],
    "headingLevel": [514, "heading-level"],
    "valueAsDate": [1040],
    "startAsDate": [1040],
    "endAsDate": [1040],
    "minAsDate": [1040],
    "maxAsDate": [1040],
    "min": [1025],
    "max": [1025],
    "active": [1540],
    "open": [1540],
    "name": [513],
    "intlPrevMonth": [1, "intl-prev-month"],
    "intlNextMonth": [1, "intl-next-month"],
    "intlYear": [1, "intl-year"],
    "locale": [1],
    "numberingSystem": [513, "numbering-system"],
    "scale": [513],
    "placement": [513],
    "range": [516],
    "required": [516],
    "start": [1537],
    "end": [1537],
    "overlayPositioning": [513, "overlay-positioning"],
    "proximitySelectionDisabled": [4, "proximity-selection-disabled"],
    "layout": [513],
    "datePickerActiveDate": [32],
    "effectiveLocale": [32],
    "focusedInput": [32],
    "globalAttributes": [32],
    "localeData": [32],
    "setFocus": [64],
    "reposition": [64]
  }, [[0, "calciteDaySelect", "calciteDaySelectHandler"]]]);
function defineCustomElement$1() {
  if (typeof customElements === "undefined") {
    return;
  }
  const components = ["calcite-input-date-picker", "calcite-date-picker", "calcite-date-picker-day", "calcite-date-picker-month", "calcite-date-picker-month-header", "calcite-icon", "calcite-input", "calcite-progress"];
  components.forEach(tagName => { switch (tagName) {
    case "calcite-input-date-picker":
      if (!customElements.get(tagName)) {
        customElements.define(tagName, InputDatePicker);
      }
      break;
    case "calcite-date-picker":
      if (!customElements.get(tagName)) {
        defineCustomElement$8();
      }
      break;
    case "calcite-date-picker-day":
      if (!customElements.get(tagName)) {
        defineCustomElement$7();
      }
      break;
    case "calcite-date-picker-month":
      if (!customElements.get(tagName)) {
        defineCustomElement$6();
      }
      break;
    case "calcite-date-picker-month-header":
      if (!customElements.get(tagName)) {
        defineCustomElement$5();
      }
      break;
    case "calcite-icon":
      if (!customElements.get(tagName)) {
        defineCustomElement$4();
      }
      break;
    case "calcite-input":
      if (!customElements.get(tagName)) {
        defineCustomElement$3();
      }
      break;
    case "calcite-progress":
      if (!customElements.get(tagName)) {
        defineCustomElement$2();
      }
      break;
  } });
}
defineCustomElement$1();

const CalciteInputDatePicker = InputDatePicker;
const defineCustomElement = defineCustomElement$1;

export { CalciteInputDatePicker, defineCustomElement };
