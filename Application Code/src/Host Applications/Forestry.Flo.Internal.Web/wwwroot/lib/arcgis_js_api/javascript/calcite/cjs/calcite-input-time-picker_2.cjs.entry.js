/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
'use strict';

Object.defineProperty(exports, '__esModule', { value: true });

const index = require('./index-b27b231c.js');
const guid = require('./guid-acbbb0e7.js');
const locale = require('./locale-f8c619b2.js');
const label = require('./label-5d4931f7.js');
const form = require('./form-45498958.js');
const interactive = require('./interactive-3d681fb9.js');
const key = require('./key-6a28a7af.js');
require('./observers-664fbf90.js');
require('./dom-2b919cb6.js');
require('./resources-3fd6da1b.js');

const maxTenthForMinuteAndSecond = 5;
function createLocaleDateTimeFormatter(locale$1, numberingSystem, includeSeconds = true) {
  try {
    const options = {
      hour: "2-digit",
      minute: "2-digit",
      timeZone: "UTC",
      numberingSystem: locale.getSupportedNumberingSystem(numberingSystem)
    };
    if (includeSeconds) {
      options.second = "2-digit";
    }
    return new Intl.DateTimeFormat(locale.getSupportedLocale(locale$1), options);
  }
  catch (error) {
    throw new Error(`Invalid locale supplied while attempting to create a DateTime formatter: ${locale$1}`);
  }
}
function formatTimePart(number) {
  const numberAsString = number.toString();
  return number >= 0 && number <= 9 ? numberAsString.padStart(2, "0") : numberAsString;
}
function formatTimeString(value) {
  if (!isValidTime(value)) {
    return null;
  }
  const [hourString, minuteString, secondString] = value.split(":");
  const hour = formatTimePart(parseInt(hourString));
  const minute = formatTimePart(parseInt(minuteString));
  if (secondString) {
    const second = formatTimePart(parseInt(secondString));
    return `${hour}:${minute}:${second}`;
  }
  return `${hour}:${minute}`;
}
function getLocaleHourCycle(locale, numberingSystem) {
  const formatter = createLocaleDateTimeFormatter(locale, numberingSystem);
  const parts = formatter.formatToParts(new Date(Date.UTC(0, 0, 0, 0, 0, 0)));
  return getLocalizedTimePart("meridiem", parts) ? "12" : "24";
}
function getLocalizedTimePart(part, parts) {
  var _a, _b, _c, _d;
  if (!part || !parts) {
    return null;
  }
  if (part === "hourSuffix") {
    const hourIndex = parts.indexOf(parts.find(({ type }) => type === "hour"));
    const minuteIndex = parts.indexOf(parts.find(({ type }) => type === "minute"));
    const hourSuffix = parts[hourIndex + 1];
    return hourSuffix && hourSuffix.type === "literal" && minuteIndex - hourIndex === 2
      ? ((_a = hourSuffix.value) === null || _a === void 0 ? void 0 : _a.trim()) || null
      : null;
  }
  if (part === "minuteSuffix") {
    const minuteIndex = parts.indexOf(parts.find(({ type }) => type === "minute"));
    const secondIndex = parts.indexOf(parts.find(({ type }) => type === "second"));
    const minuteSuffix = parts[minuteIndex + 1];
    return minuteSuffix && minuteSuffix.type === "literal" && secondIndex - minuteIndex === 2
      ? ((_b = minuteSuffix.value) === null || _b === void 0 ? void 0 : _b.trim()) || null
      : null;
  }
  if (part === "secondSuffix") {
    const secondIndex = parts.indexOf(parts.find(({ type }) => type === "second"));
    const secondSuffix = parts[secondIndex + 1];
    return secondSuffix && secondSuffix.type === "literal" ? ((_c = secondSuffix.value) === null || _c === void 0 ? void 0 : _c.trim()) || null : null;
  }
  return ((_d = parts.find(({ type }) => (part == "meridiem" ? type === "dayPeriod" : type === part))) === null || _d === void 0 ? void 0 : _d.value) || null;
}
function getMeridiem(hour) {
  if (!locale.isValidNumber(hour)) {
    return null;
  }
  const hourAsNumber = parseInt(hour);
  return hourAsNumber >= 0 && hourAsNumber <= 11 ? "AM" : "PM";
}
function isValidTime(value) {
  if (!value || value.startsWith(":") || value.endsWith(":")) {
    return false;
  }
  const splitValue = value.split(":");
  const validLength = splitValue.length > 1 && splitValue.length < 4;
  if (!validLength) {
    return false;
  }
  const [hour, minute, second] = splitValue;
  const hourAsNumber = parseInt(splitValue[0]);
  const minuteAsNumber = parseInt(splitValue[1]);
  const secondAsNumber = parseInt(splitValue[2]);
  const hourValid = locale.isValidNumber(hour) && hourAsNumber >= 0 && hourAsNumber < 24;
  const minuteValid = locale.isValidNumber(minute) && minuteAsNumber >= 0 && minuteAsNumber < 60;
  const secondValid = locale.isValidNumber(second) && secondAsNumber >= 0 && secondAsNumber < 60;
  if ((hourValid && minuteValid && !second) || (hourValid && minuteValid && secondValid)) {
    return true;
  }
}
function isValidTimePart(value, part) {
  if (part === "meridiem") {
    return value === "AM" || value === "PM";
  }
  if (!locale.isValidNumber(value)) {
    return false;
  }
  const valueAsNumber = Number(value);
  return part === "hour" ? valueAsNumber >= 0 && valueAsNumber < 24 : valueAsNumber >= 0 && valueAsNumber < 60;
}
function localizeTimePart({ value, part, locale, numberingSystem }) {
  if (!isValidTimePart(value, part)) {
    return;
  }
  const valueAsNumber = parseInt(value);
  const date = new Date(Date.UTC(0, 0, 0, part === "hour" ? valueAsNumber : part === "meridiem" ? (value === "AM" ? 0 : 12) : 0, part === "minute" ? valueAsNumber : 0, part === "second" ? valueAsNumber : 0));
  if (!date) {
    return;
  }
  const formatter = createLocaleDateTimeFormatter(locale, numberingSystem);
  const parts = formatter.formatToParts(date);
  return getLocalizedTimePart(part, parts);
}
function localizeTimeString({ value, locale, numberingSystem, includeSeconds = true }) {
  if (!isValidTime(value)) {
    return null;
  }
  const { hour, minute, second = "0" } = parseTimeString(value);
  const dateFromTimeString = new Date(Date.UTC(0, 0, 0, parseInt(hour), parseInt(minute), parseInt(second)));
  const formatter = createLocaleDateTimeFormatter(locale, numberingSystem, includeSeconds);
  return (formatter === null || formatter === void 0 ? void 0 : formatter.format(dateFromTimeString)) || null;
}
function localizeTimeStringToParts({ value, locale, numberingSystem }) {
  if (!isValidTime(value)) {
    return null;
  }
  const { hour, minute, second = "0" } = parseTimeString(value);
  const dateFromTimeString = new Date(Date.UTC(0, 0, 0, parseInt(hour), parseInt(minute), parseInt(second)));
  if (dateFromTimeString) {
    const formatter = createLocaleDateTimeFormatter(locale, numberingSystem);
    const parts = formatter.formatToParts(dateFromTimeString);
    return {
      localizedHour: getLocalizedTimePart("hour", parts),
      localizedHourSuffix: getLocalizedTimePart("hourSuffix", parts),
      localizedMinute: getLocalizedTimePart("minute", parts),
      localizedMinuteSuffix: getLocalizedTimePart("minuteSuffix", parts),
      localizedSecond: getLocalizedTimePart("second", parts),
      localizedSecondSuffix: getLocalizedTimePart("secondSuffix", parts),
      localizedMeridiem: getLocalizedTimePart("meridiem", parts)
    };
  }
  return null;
}
function getTimeParts({ value, locale, numberingSystem }) {
  if (!isValidTime(value)) {
    return null;
  }
  const { hour, minute, second = "0" } = parseTimeString(value);
  const dateFromTimeString = new Date(Date.UTC(0, 0, 0, parseInt(hour), parseInt(minute), parseInt(second)));
  if (dateFromTimeString) {
    const formatter = createLocaleDateTimeFormatter(locale, numberingSystem);
    const parts = formatter.formatToParts(dateFromTimeString);
    return parts;
  }
  return null;
}
function parseTimeString(value) {
  if (isValidTime(value)) {
    const [hour, minute, second] = value.split(":");
    return {
      hour,
      minute,
      second
    };
  }
  return {
    hour: null,
    minute: null,
    second: null
  };
}

const inputTimePickerCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{display:inline-block;-webkit-user-select:none;user-select:none}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}::slotted(input[slot=hidden-form-input]){margin:0 !important;opacity:0 !important;outline:none !important;padding:0 !important;position:absolute !important;inset:0 !important;transform:none !important;-webkit-appearance:none !important;z-index:-1 !important}";

const InputTimePicker = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteInputTimePickerChange = index.createEvent(this, "calciteInputTimePickerChange", 7);
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /**
     * When `true`, the component is active.
     *
     * @deprecated Use `open` instead.
     */
    this.active = false;
    /** When `true`, displays the `calcite-time-picker` component. */
    this.open = false;
    /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
    this.disabled = false;
    /**
     * When `true`, the component's value can be read, but controls are not accessible and the value cannot be modified.
     *
     * @mdn [readOnly](https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/readonly)
     */
    this.readOnly = false;
    /**
     * When `true`, the component must have a value in order for the form to submit.
     *
     * @internal
     */
    this.required = false;
    /** Specifies the size of the component. */
    this.scale = "m";
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
     * Determines where the popover will be positioned relative to the input.
     *
     * @see [LogicalPlacement](https://github.com/Esri/calcite-components/blob/master/src/utils/floating-ui.ts#L25)
     */
    this.placement = "auto";
    /** Specifies the granularity the component's `value` must adhere to (in seconds). */
    this.step = 60;
    /** The component's value in UTC (always 24-hour format). */
    this.value = null;
    /** whether the value of the input was changed as a result of user typing or not */
    this.internalValueChange = false;
    this.previousValidValue = null;
    this.referenceElementId = `input-time-picker-${guid.guid()}`;
    //--------------------------------------------------------------------------
    //
    //  State
    //
    //--------------------------------------------------------------------------
    this.effectiveLocale = "";
    //--------------------------------------------------------------------------
    //
    //  Event Listeners
    //
    //--------------------------------------------------------------------------
    this.calciteInternalInputBlurHandler = () => {
      this.open = false;
      const shouldIncludeSeconds = this.shouldIncludeSeconds();
      const { effectiveLocale: locale$1, numberingSystem, value, calciteInputEl } = this;
      locale.numberStringFormatter.numberFormatOptions = {
        locale: locale$1,
        numberingSystem,
        useGrouping: false
      };
      const delocalizedValue = locale.numberStringFormatter.delocalize(calciteInputEl.value);
      const localizedInputValue = localizeTimeString({
        value: delocalizedValue,
        includeSeconds: shouldIncludeSeconds,
        locale: locale$1,
        numberingSystem
      });
      this.setInputValue(localizedInputValue ||
        localizeTimeString({ value, locale: locale$1, numberingSystem, includeSeconds: shouldIncludeSeconds }));
    };
    this.calciteInternalInputFocusHandler = (event) => {
      if (!this.readOnly) {
        this.open = true;
        event.stopPropagation();
      }
    };
    this.calciteInputInputHandler = (event) => {
      const target = event.target;
      locale.numberStringFormatter.numberFormatOptions = {
        locale: this.effectiveLocale,
        numberingSystem: this.numberingSystem,
        useGrouping: false
      };
      const delocalizedValue = locale.numberStringFormatter.delocalize(target.value);
      this.setValue({ value: delocalizedValue });
      // only translate the numerals until blur
      const localizedValue = delocalizedValue
        .split("")
        .map((char) => key.numberKeys.includes(char)
        ? locale.numberStringFormatter.numberFormatter.format(Number(char))
        : char)
        .join("");
      this.setInputValue(localizedValue);
    };
    this.timePickerChangeHandler = (event) => {
      event.stopPropagation();
      const target = event.target;
      const value = target.value;
      this.setValue({ value, origin: "time-picker" });
    };
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.keyDownHandler = (event) => {
      const { defaultPrevented, key } = event;
      if (defaultPrevented) {
        return;
      }
      if (key === "Enter") {
        if (form.submitForm(this)) {
          event.preventDefault();
        }
      }
      if (key === "Escape" && this.open) {
        this.open = false;
        event.preventDefault();
      }
    };
    this.setCalcitePopoverEl = (el) => {
      this.popoverEl = el;
    };
    this.setCalciteInputEl = (el) => {
      this.calciteInputEl = el;
    };
    this.setCalciteTimePickerEl = (el) => {
      this.calciteTimePickerEl = el;
    };
    this.setInputValue = (newInputValue) => {
      if (!this.calciteInputEl) {
        return;
      }
      this.calciteInputEl.value = newInputValue;
    };
    this.setValue = ({ value, origin = "input" }) => {
      const previousValue = this.value;
      const newValue = formatTimeString(value);
      const newLocalizedValue = localizeTimeString({
        value: newValue,
        locale: this.effectiveLocale,
        numberingSystem: this.numberingSystem,
        includeSeconds: this.shouldIncludeSeconds()
      });
      this.internalValueChange = origin !== "external" && origin !== "loading";
      const shouldEmit = origin !== "loading" &&
        origin !== "external" &&
        ((value !== this.previousValidValue && !value) ||
          !!(!this.previousValidValue && newValue) ||
          (newValue !== this.previousValidValue && newValue));
      if (value) {
        if (shouldEmit) {
          this.previousValidValue = newValue;
        }
        if (newValue && newValue !== this.value) {
          this.value = newValue;
        }
        this.localizedValue = newLocalizedValue;
      }
      else {
        this.value = value;
        this.localizedValue = null;
      }
      if (origin === "time-picker" || origin === "external") {
        this.setInputValue(newLocalizedValue);
      }
      if (shouldEmit) {
        const changeEvent = this.calciteInputTimePickerChange.emit();
        if (changeEvent.defaultPrevented) {
          this.internalValueChange = false;
          this.value = previousValue;
          this.setInputValue(previousValue);
          this.previousValidValue = previousValue;
        }
        else {
          this.previousValidValue = newValue;
        }
      }
    };
  }
  activeHandler(value) {
    this.open = value;
  }
  openHandler(value) {
    this.active = value;
    if (this.disabled || this.readOnly) {
      this.open = false;
      return;
    }
    if (value) {
      this.reposition(true);
    }
  }
  handleDisabledAndReadOnlyChange(value) {
    if (!value) {
      this.open = false;
    }
  }
  localeChanged() {
    locale.updateEffectiveLocale(this);
  }
  valueWatcher(newValue) {
    if (!this.internalValueChange) {
      this.setValue({ value: newValue, origin: "external" });
    }
    this.internalValueChange = false;
  }
  effectiveLocaleWatcher() {
    this.setInputValue(localizeTimeString({
      value: this.value,
      locale: this.effectiveLocale,
      numberingSystem: this.numberingSystem,
      includeSeconds: this.shouldIncludeSeconds()
    }));
  }
  clickHandler(event) {
    if (event.composedPath().includes(this.calciteTimePickerEl)) {
      return;
    }
    this.setFocus();
  }
  timePickerBlurHandler(event) {
    event.preventDefault();
    event.stopPropagation();
    this.open = false;
  }
  timePickerFocusHandler(event) {
    event.preventDefault();
    event.stopPropagation();
    if (!this.readOnly) {
      this.open = true;
    }
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.calciteInputEl) === null || _a === void 0 ? void 0 : _a.setFocus();
  }
  /**
   * Updates the position of the component.
   *
   * @param delayed
   */
  async reposition(delayed = false) {
    var _a;
    (_a = this.popoverEl) === null || _a === void 0 ? void 0 : _a.reposition(delayed);
  }
  onLabelClick() {
    this.setFocus();
  }
  shouldIncludeSeconds() {
    return this.step < 60;
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    locale.connectLocalized(this);
    const { active, open } = this;
    if (this.value) {
      this.setValue({ value: isValidTime(this.value) ? this.value : undefined, origin: "loading" });
    }
    label.connectLabel(this);
    form.connectForm(this);
    if (open) {
      this.active = open;
    }
    else if (active) {
      this.open = active;
    }
  }
  componentDidLoad() {
    this.setInputValue(this.localizedValue);
  }
  disconnectedCallback() {
    label.disconnectLabel(this);
    form.disconnectForm(this);
    locale.disconnectLocalized(this);
  }
  componentDidRender() {
    interactive.updateHostInteraction(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    const popoverId = `${this.referenceElementId}-popover`;
    return (index.h(index.Host, { onKeyDown: this.keyDownHandler }, index.h("div", { "aria-controls": popoverId, "aria-haspopup": "dialog", "aria-label": this.name, "aria-owns": popoverId, id: this.referenceElementId, role: "combobox" }, index.h("calcite-input", { disabled: this.disabled, icon: "clock", label: label.getLabelText(this), onCalciteInputInput: this.calciteInputInputHandler, onCalciteInternalInputBlur: this.calciteInternalInputBlurHandler, onCalciteInternalInputFocus: this.calciteInternalInputFocusHandler, readOnly: this.readOnly, ref: this.setCalciteInputEl, scale: this.scale, step: this.step })), index.h("calcite-popover", { id: popoverId, label: "Time Picker", open: this.open, overlayPositioning: this.overlayPositioning, placement: this.placement, ref: this.setCalcitePopoverEl, referenceElement: this.referenceElementId, triggerDisabled: true }, index.h("calcite-time-picker", { intlHour: this.intlHour, intlHourDown: this.intlHourDown, intlHourUp: this.intlHourUp, intlMeridiem: this.intlMeridiem, intlMeridiemDown: this.intlMeridiemDown, intlMeridiemUp: this.intlMeridiemUp, intlMinute: this.intlMinute, intlMinuteDown: this.intlMinuteDown, intlMinuteUp: this.intlMinuteUp, intlSecond: this.intlSecond, intlSecondDown: this.intlSecondDown, intlSecondUp: this.intlSecondUp, lang: this.effectiveLocale, numberingSystem: this.numberingSystem, onCalciteInternalTimePickerChange: this.timePickerChangeHandler, ref: this.setCalciteTimePickerEl, scale: this.scale, step: this.step, value: this.value })), index.h(form.HiddenFormInputSlot, { component: this })));
  }
  get el() { return index.getElement(this); }
  static get watchers() { return {
    "active": ["activeHandler"],
    "open": ["openHandler"],
    "disabled": ["handleDisabledAndReadOnlyChange"],
    "readOnly": ["handleDisabledAndReadOnlyChange"],
    "locale": ["localeChanged"],
    "value": ["valueWatcher"],
    "effectiveLocale": ["effectiveLocaleWatcher"]
  }; }
};
InputTimePicker.style = inputTimePickerCss;

const CSS = {
  button: "button",
  buttonBottomLeft: "button--bottom-left",
  buttonBottomRight: "button--bottom-right",
  buttonHourDown: "button--hour-down",
  buttonHourUp: "button--hour-up",
  buttonMeridiemDown: "button--meridiem-down",
  buttonMeridiemUp: "button--meridiem-up",
  buttonMinuteDown: "button--minute-down",
  buttonMinuteUp: "button--minute-up",
  buttonSecondDown: "button--second-down",
  buttonSecondUp: "button--second-up",
  buttonTopLeft: "button--top-left",
  buttonTopRight: "button--top-right",
  column: "column",
  delimiter: "delimiter",
  hour: "hour",
  input: "input",
  meridiem: "meridiem",
  minute: "minute",
  second: "second",
  showMeridiem: "show-meridiem",
  showSecond: "show-second",
  "scale-s": "scale-s",
  "scale-m": "scale-m",
  "scale-l": "scale-l",
  timePicker: "time-picker",
  meridiemStart: "meridiem--start"
};
const TEXT = {
  hour: "Hour",
  hourDown: "Decrease hour",
  hourUp: "Increase hour",
  meridiem: "AM/PM",
  meridiemDown: "Decrease AM/PM",
  meridiemUp: "Increase AM/PM",
  minute: "Minute",
  minuteDown: "Decrease minute",
  minuteUp: "Increase minute",
  second: "Second",
  secondDown: "Decrease second",
  secondUp: "Increase second"
};

const timePickerCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host{display:inline-block}.time-picker{display:flex;-webkit-user-select:none;user-select:none;align-items:center;background-color:var(--calcite-ui-foreground-1);font-weight:var(--calcite-font-weight-medium);color:var(--calcite-ui-text-1);--tw-shadow:0 6px 20px -4px rgba(0, 0, 0, 0.1), 0 4px 12px -2px rgba(0, 0, 0, 0.08);--tw-shadow-colored:0 6px 20px -4px var(--tw-shadow-color), 0 4px 12px -2px var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow);border-radius:var(--calcite-border-radius)}.time-picker .column{display:flex;flex-direction:column}.time-picker .meridiem--start{order:-1}.time-picker .button{display:inline-flex;cursor:pointer;align-items:center;justify-content:center;background-color:var(--calcite-ui-foreground-1)}.time-picker .button:hover,.time-picker .button:focus{background-color:var(--calcite-ui-foreground-2);outline:2px solid transparent;outline-offset:2px}.time-picker .button:active{background-color:var(--calcite-ui-foreground-3)}.time-picker .button.top-left{border-start-start-radius:var(--calcite-border-radius)}.time-picker .button.bottom-left{border-end-start-radius:var(--calcite-border-radius)}.time-picker .button.top-right{border-start-end-radius:var(--calcite-border-radius)}.time-picker .button.bottom-right{border-end-end-radius:var(--calcite-border-radius)}.time-picker .button calcite-icon{color:var(--calcite-ui-text-3)}.time-picker .input{display:inline-flex;cursor:pointer;align-items:center;justify-content:center;background-color:var(--calcite-ui-foreground-1);font-weight:var(--calcite-font-weight-medium)}.time-picker .input:hover{box-shadow:inset 0 0 0 2px var(--calcite-ui-foreground-2)}.time-picker .input:focus,.time-picker .input:hover:focus{outline:2px solid transparent;outline-offset:2px;box-shadow:inset 0 0 0 2px var(--calcite-ui-brand)}.time-picker.scale-s{font-size:var(--calcite-font-size--1)}.time-picker.scale-s .button,.time-picker.scale-s .input{padding-inline:0.75rem;padding-block:0.25rem}.time-picker.scale-s:not(.show-meridiem) .delimiter:last-child{padding-inline-end:0.75rem}.time-picker.scale-m{font-size:var(--calcite-font-size-0)}.time-picker.scale-m .button,.time-picker.scale-m .input{padding-inline:1rem;padding-block:0.5rem}.time-picker.scale-m:not(.show-meridiem) .delimiter:last-child{padding-inline-end:1rem}.time-picker.scale-l{font-size:var(--calcite-font-size-1)}.time-picker.scale-l .button,.time-picker.scale-l .input{padding-inline:1.25rem;padding-block:0.75rem}.time-picker.scale-l:not(.show-meridiem) .delimiter:last-child{padding-inline-end:1.25rem}";

function capitalize(str) {
  return str.charAt(0).toUpperCase() + str.slice(1);
}
const TimePicker = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteInternalTimePickerBlur = index.createEvent(this, "calciteInternalTimePickerBlur", 6);
    this.calciteInternalTimePickerChange = index.createEvent(this, "calciteInternalTimePickerChange", 6);
    this.calciteInternalTimePickerFocus = index.createEvent(this, "calciteInternalTimePickerFocus", 6);
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /**
     * Accessible name for the component's hour input.
     *
     * @default "Hour"
     */
    this.intlHour = TEXT.hour;
    /**
     * Accessible name for the component's hour down button.
     *
     * @default "Decrease hour"
     */
    this.intlHourDown = TEXT.hourDown;
    /**
     * Accessible name for the component's hour up button.
     *
     * @default "Increase hour"
     */
    this.intlHourUp = TEXT.hourUp;
    /**
     * Accessible name for the component's meridiem (AM/PM) input.
     *
     * @default "AM/PM"
     */
    this.intlMeridiem = TEXT.meridiem;
    /**
     * Accessible name for the component's meridiem (AM/PM) down button.
     *
     * @default "Decrease AM/PM"
     */
    this.intlMeridiemDown = TEXT.meridiemDown;
    /**
     * Accessible name for the component's meridiem (AM/PM) up button.
     *
     * @default "Increase AM/PM"
     */
    this.intlMeridiemUp = TEXT.meridiemUp;
    /**
     * Accessible name for the component's minute input.
     *
     * @default "Minute"
     */
    this.intlMinute = TEXT.minute;
    /**
     * Accessible name for the component's minute down button.
     *
     * @default "Decrease minute"
     */
    this.intlMinuteDown = TEXT.minuteDown;
    /**
     * Accessible name for the component's minute up button.
     *
     * @default "Increase minute"
     */
    this.intlMinuteUp = TEXT.minuteUp;
    /**
     * Accessible name for the component's second input.
     *
     * @default "Second"
     */
    this.intlSecond = TEXT.second;
    /**
     * Accessible name for the component's second down button.
     *
     * @default "Decrease second"
     */
    this.intlSecondDown = TEXT.secondDown;
    /**
     * Accessible name for the component's second up button.
     *
     * @default "Increase second"
     */
    this.intlSecondUp = TEXT.secondUp;
    /** Specifies the size of the component. */
    this.scale = "m";
    /** Specifies the granularity the `value` must adhere to (in seconds). */
    this.step = 60;
    /** The component's value in UTC (always 24-hour format). */
    this.value = null;
    // --------------------------------------------------------------------------
    //
    //  State
    //
    // --------------------------------------------------------------------------
    this.effectiveLocale = "";
    this.showSecond = this.step < 60;
    this.decrementHour = () => {
      const newHour = !this.hour ? 0 : this.hour === "00" ? 23 : parseInt(this.hour) - 1;
      this.setValuePart("hour", newHour);
    };
    this.decrementMeridiem = () => {
      const newMeridiem = this.meridiem === "PM" ? "AM" : "PM";
      this.setValuePart("meridiem", newMeridiem);
    };
    this.decrementMinuteOrSecond = (key) => {
      let newValue;
      if (locale.isValidNumber(this[key])) {
        const valueAsNumber = parseInt(this[key]);
        newValue = valueAsNumber === 0 ? 59 : valueAsNumber - 1;
      }
      else {
        newValue = 59;
      }
      this.setValuePart(key, newValue);
    };
    this.decrementMinute = () => {
      this.decrementMinuteOrSecond("minute");
    };
    this.decrementSecond = () => {
      this.decrementMinuteOrSecond("second");
    };
    this.focusHandler = (event) => {
      this.activeEl = event.currentTarget;
    };
    this.hourDownButtonKeyDownHandler = (event) => {
      if (this.buttonActivated(event)) {
        this.decrementHour();
      }
    };
    this.hourKeyDownHandler = (event) => {
      const { key: key$1 } = event;
      if (key.numberKeys.includes(key$1)) {
        const keyAsNumber = parseInt(key$1);
        let newHour;
        if (locale.isValidNumber(this.hour)) {
          switch (this.hourCycle) {
            case "12":
              newHour =
                this.hour === "01" && keyAsNumber >= 0 && keyAsNumber <= 2
                  ? `1${keyAsNumber}`
                  : keyAsNumber;
              break;
            case "24":
              if (this.hour === "01") {
                newHour = `1${keyAsNumber}`;
              }
              else if (this.hour === "02" && keyAsNumber >= 0 && keyAsNumber <= 3) {
                newHour = `2${keyAsNumber}`;
              }
              else {
                newHour = keyAsNumber;
              }
              break;
          }
        }
        else {
          newHour = keyAsNumber;
        }
        this.setValuePart("hour", newHour);
      }
      else {
        switch (key$1) {
          case "Backspace":
          case "Delete":
            this.setValuePart("hour", null);
            break;
          case "ArrowDown":
            event.preventDefault();
            this.decrementHour();
            break;
          case "ArrowUp":
            event.preventDefault();
            this.incrementHour();
            break;
          case " ":
            event.preventDefault();
            break;
        }
      }
    };
    this.hourUpButtonKeyDownHandler = (event) => {
      if (this.buttonActivated(event)) {
        this.incrementHour();
      }
    };
    this.incrementMeridiem = () => {
      const newMeridiem = this.meridiem === "AM" ? "PM" : "AM";
      this.setValuePart("meridiem", newMeridiem);
    };
    this.incrementHour = () => {
      const newHour = locale.isValidNumber(this.hour)
        ? this.hour === "23"
          ? 0
          : parseInt(this.hour) + 1
        : 1;
      this.setValuePart("hour", newHour);
    };
    this.incrementMinuteOrSecond = (key) => {
      const newValue = locale.isValidNumber(this[key])
        ? this[key] === "59"
          ? 0
          : parseInt(this[key]) + 1
        : 0;
      this.setValuePart(key, newValue);
    };
    this.incrementMinute = () => {
      this.incrementMinuteOrSecond("minute");
    };
    this.incrementSecond = () => {
      this.incrementMinuteOrSecond("second");
    };
    this.meridiemDownButtonKeyDownHandler = (event) => {
      if (this.buttonActivated(event)) {
        this.decrementMeridiem();
      }
    };
    this.meridiemKeyDownHandler = (event) => {
      switch (event.key) {
        case "a":
          this.setValuePart("meridiem", "AM");
          break;
        case "p":
          this.setValuePart("meridiem", "PM");
          break;
        case "Backspace":
        case "Delete":
          this.setValuePart("meridiem", null);
          break;
        case "ArrowUp":
          event.preventDefault();
          this.incrementMeridiem();
          break;
        case "ArrowDown":
          event.preventDefault();
          this.decrementMeridiem();
          break;
        case " ":
          event.preventDefault();
          break;
      }
    };
    this.meridiemUpButtonKeyDownHandler = (event) => {
      if (this.buttonActivated(event)) {
        this.incrementMeridiem();
      }
    };
    this.minuteDownButtonKeyDownHandler = (event) => {
      if (this.buttonActivated(event)) {
        this.decrementMinute();
      }
    };
    this.minuteKeyDownHandler = (event) => {
      const { key: key$1 } = event;
      if (key.numberKeys.includes(key$1)) {
        const keyAsNumber = parseInt(key$1);
        let newMinute;
        if (locale.isValidNumber(this.minute) && this.minute.startsWith("0")) {
          const minuteAsNumber = parseInt(this.minute);
          newMinute =
            minuteAsNumber > maxTenthForMinuteAndSecond
              ? keyAsNumber
              : `${minuteAsNumber}${keyAsNumber}`;
        }
        else {
          newMinute = keyAsNumber;
        }
        this.setValuePart("minute", newMinute);
      }
      else {
        switch (key$1) {
          case "Backspace":
          case "Delete":
            this.setValuePart("minute", null);
            break;
          case "ArrowDown":
            event.preventDefault();
            this.decrementMinute();
            break;
          case "ArrowUp":
            event.preventDefault();
            this.incrementMinute();
            break;
          case " ":
            event.preventDefault();
            break;
        }
      }
    };
    this.minuteUpButtonKeyDownHandler = (event) => {
      if (this.buttonActivated(event)) {
        this.incrementMinute();
      }
    };
    this.secondDownButtonKeyDownHandler = (event) => {
      if (this.buttonActivated(event)) {
        this.decrementSecond();
      }
    };
    this.secondKeyDownHandler = (event) => {
      const { key: key$1 } = event;
      if (key.numberKeys.includes(key$1)) {
        const keyAsNumber = parseInt(key$1);
        let newSecond;
        if (locale.isValidNumber(this.second) && this.second.startsWith("0")) {
          const secondAsNumber = parseInt(this.second);
          newSecond =
            secondAsNumber > maxTenthForMinuteAndSecond
              ? keyAsNumber
              : `${secondAsNumber}${keyAsNumber}`;
        }
        else {
          newSecond = keyAsNumber;
        }
        this.setValuePart("second", newSecond);
      }
      else {
        switch (key$1) {
          case "Backspace":
          case "Delete":
            this.setValuePart("second", null);
            break;
          case "ArrowDown":
            event.preventDefault();
            this.decrementSecond();
            break;
          case "ArrowUp":
            event.preventDefault();
            this.incrementSecond();
            break;
          case " ":
            event.preventDefault();
            break;
        }
      }
    };
    this.secondUpButtonKeyDownHandler = (event) => {
      if (this.buttonActivated(event)) {
        this.incrementSecond();
      }
    };
    this.setHourEl = (el) => (this.hourEl = el);
    this.setMeridiemEl = (el) => (this.meridiemEl = el);
    this.setMinuteEl = (el) => (this.minuteEl = el);
    this.setSecondEl = (el) => (this.secondEl = el);
    this.setValue = (value, emit = true) => {
      if (isValidTime(value)) {
        const { hour, minute, second } = parseTimeString(value);
        const { effectiveLocale: locale, numberingSystem } = this;
        const { localizedHour, localizedHourSuffix, localizedMinute, localizedMinuteSuffix, localizedSecond, localizedSecondSuffix, localizedMeridiem } = localizeTimeStringToParts({ value, locale, numberingSystem });
        this.localizedHour = localizedHour;
        this.localizedHourSuffix = localizedHourSuffix;
        this.localizedMinute = localizedMinute;
        this.localizedMinuteSuffix = localizedMinuteSuffix;
        this.localizedSecond = localizedSecond;
        this.localizedSecondSuffix = localizedSecondSuffix;
        this.hour = hour;
        this.minute = minute;
        this.second = second;
        if (localizedMeridiem) {
          this.localizedMeridiem = localizedMeridiem;
          this.meridiem = getMeridiem(this.hour);
          const formatParts = getTimeParts({ value, locale, numberingSystem });
          this.meridiemOrder = this.getMeridiemOrder(formatParts);
        }
      }
      else {
        this.hour = null;
        this.localizedHour = null;
        this.localizedHourSuffix = null;
        this.localizedMeridiem = null;
        this.localizedMinute = null;
        this.localizedMinuteSuffix = null;
        this.localizedSecond = null;
        this.localizedSecondSuffix = null;
        this.meridiem = null;
        this.minute = null;
        this.second = null;
        this.value = null;
      }
      if (emit) {
        this.calciteInternalTimePickerChange.emit();
      }
    };
    this.setValuePart = (key, value, emit = true) => {
      var _a;
      const { effectiveLocale: locale$1, numberingSystem } = this;
      if (key === "meridiem") {
        this.meridiem = value;
        if (locale.isValidNumber(this.hour)) {
          const hourAsNumber = parseInt(this.hour);
          switch (value) {
            case "AM":
              if (hourAsNumber >= 12) {
                this.hour = formatTimePart(hourAsNumber - 12);
              }
              break;
            case "PM":
              if (hourAsNumber < 12) {
                this.hour = formatTimePart(hourAsNumber + 12);
              }
              break;
          }
          this.localizedHour = localizeTimePart({
            value: this.hour,
            part: "hour",
            locale: locale$1,
            numberingSystem
          });
        }
      }
      else {
        this[key] = typeof value === "number" ? formatTimePart(value) : value;
        this[`localized${capitalize(key)}`] = localizeTimePart({
          value: this[key],
          part: key,
          locale: locale$1,
          numberingSystem
        });
      }
      if (this.hour && this.minute) {
        const showSeconds = this.second && this.showSecond;
        this.value = `${this.hour}:${this.minute}:${showSeconds ? this.second : "00"}`;
      }
      else {
        this.value = null;
      }
      this.localizedMeridiem = this.value
        ? ((_a = localizeTimeStringToParts({ value: this.value, locale: locale$1, numberingSystem })) === null || _a === void 0 ? void 0 : _a.localizedMeridiem) || null
        : localizeTimePart({ value: this.meridiem, part: "meridiem", locale: locale$1, numberingSystem });
      if (emit) {
        this.calciteInternalTimePickerChange.emit();
      }
    };
  }
  localeChanged() {
    locale.updateEffectiveLocale(this);
  }
  valueWatcher(newValue) {
    this.setValue(newValue, false);
  }
  effectiveLocaleWatcher() {
    this.updateLocale();
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  hostBlurHandler() {
    this.calciteInternalTimePickerBlur.emit();
  }
  hostFocusHandler() {
    this.calciteInternalTimePickerFocus.emit();
  }
  keyDownHandler(event) {
    const { defaultPrevented, key } = event;
    if (defaultPrevented) {
      return;
    }
    switch (this.activeEl) {
      case this.hourEl:
        if (key === "ArrowRight") {
          this.setFocus("minute");
          event.preventDefault();
        }
        break;
      case this.minuteEl:
        switch (key) {
          case "ArrowLeft":
            this.setFocus("hour");
            event.preventDefault();
            break;
          case "ArrowRight":
            if (this.step !== 60) {
              this.setFocus("second");
              event.preventDefault();
            }
            else if (this.hourCycle === "12") {
              this.setFocus("meridiem");
              event.preventDefault();
            }
            break;
        }
        break;
      case this.secondEl:
        switch (key) {
          case "ArrowLeft":
            this.setFocus("minute");
            event.preventDefault();
            break;
          case "ArrowRight":
            if (this.hourCycle === "12") {
              this.setFocus("meridiem");
              event.preventDefault();
            }
            break;
        }
        break;
      case this.meridiemEl:
        switch (key) {
          case "ArrowLeft":
            if (this.step !== 60) {
              this.setFocus("second");
              event.preventDefault();
            }
            else {
              this.setFocus("minute");
              event.preventDefault();
            }
            break;
        }
        break;
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /**
   * Sets focus on the component.
   *
   * @param target
   */
  async setFocus(target) {
    var _a;
    (_a = this[`${target || "hour"}El`]) === null || _a === void 0 ? void 0 : _a.focus();
  }
  // --------------------------------------------------------------------------
  //
  //  Private Methods
  //
  // --------------------------------------------------------------------------
  buttonActivated(event) {
    const { key: key$1 } = event;
    if (key$1 === " ") {
      event.preventDefault();
    }
    return key.isActivationKey(key$1);
  }
  getMeridiemOrder(formatParts) {
    const locale = this.effectiveLocale;
    const isRTLKind = locale === "ar" || locale === "he";
    if (formatParts && !isRTLKind) {
      const index = formatParts.findIndex((parts) => {
        return parts.value === this.localizedMeridiem;
      });
      return index;
    }
    return 0;
  }
  updateLocale() {
    this.hourCycle = getLocaleHourCycle(this.effectiveLocale, this.numberingSystem);
    this.setValue(this.value, false);
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    locale.connectLocalized(this);
    this.updateLocale();
    this.meridiemOrder = this.getMeridiemOrder(getTimeParts({
      value: "0:00:00",
      locale: this.effectiveLocale,
      numberingSystem: this.numberingSystem
    }));
  }
  disconnectedCallback() {
    locale.disconnectLocalized(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    const hourIsNumber = locale.isValidNumber(this.hour);
    const iconScale = this.scale === "s" || this.scale === "m" ? "s" : "m";
    const minuteIsNumber = locale.isValidNumber(this.minute);
    const secondIsNumber = locale.isValidNumber(this.second);
    const showMeridiem = this.hourCycle === "12";
    return (index.h("div", { class: {
        [CSS.timePicker]: true,
        [CSS.showMeridiem]: showMeridiem,
        [CSS.showSecond]: this.showSecond,
        [CSS[`scale-${this.scale}`]]: true
      }, dir: "ltr" }, index.h("div", { class: CSS.column, role: "group" }, index.h("span", { "aria-label": this.intlHourUp, class: {
        [CSS.button]: true,
        [CSS.buttonHourUp]: true,
        [CSS.buttonTopLeft]: true
      }, onClick: this.incrementHour, onKeyDown: this.hourUpButtonKeyDownHandler, role: "button", tabIndex: -1 }, index.h("calcite-icon", { icon: "chevron-up", scale: iconScale })), index.h("span", { "aria-label": this.intlHour, "aria-valuemax": "23", "aria-valuemin": "1", "aria-valuenow": (hourIsNumber && parseInt(this.hour)) || "0", "aria-valuetext": this.hour, class: {
        [CSS.input]: true,
        [CSS.hour]: true
      }, onFocus: this.focusHandler, onKeyDown: this.hourKeyDownHandler, ref: this.setHourEl, role: "spinbutton", tabIndex: 0 }, this.localizedHour || "--"), index.h("span", { "aria-label": this.intlHourDown, class: {
        [CSS.button]: true,
        [CSS.buttonHourDown]: true,
        [CSS.buttonBottomLeft]: true
      }, onClick: this.decrementHour, onKeyDown: this.hourDownButtonKeyDownHandler, role: "button", tabIndex: -1 }, index.h("calcite-icon", { icon: "chevron-down", scale: iconScale }))), index.h("span", { class: CSS.delimiter }, this.localizedHourSuffix), index.h("div", { class: CSS.column, role: "group" }, index.h("span", { "aria-label": this.intlMinuteUp, class: {
        [CSS.button]: true,
        [CSS.buttonMinuteUp]: true
      }, onClick: this.incrementMinute, onKeyDown: this.minuteUpButtonKeyDownHandler, role: "button", tabIndex: -1 }, index.h("calcite-icon", { icon: "chevron-up", scale: iconScale })), index.h("span", { "aria-label": this.intlMinute, "aria-valuemax": "12", "aria-valuemin": "1", "aria-valuenow": (minuteIsNumber && parseInt(this.minute)) || "0", "aria-valuetext": this.minute, class: {
        [CSS.input]: true,
        [CSS.minute]: true
      }, onFocus: this.focusHandler, onKeyDown: this.minuteKeyDownHandler, ref: this.setMinuteEl, role: "spinbutton", tabIndex: 0 }, this.localizedMinute || "--"), index.h("span", { "aria-label": this.intlMinuteDown, class: {
        [CSS.button]: true,
        [CSS.buttonMinuteDown]: true
      }, onClick: this.decrementMinute, onKeyDown: this.minuteDownButtonKeyDownHandler, role: "button", tabIndex: -1 }, index.h("calcite-icon", { icon: "chevron-down", scale: iconScale }))), this.showSecond && index.h("span", { class: CSS.delimiter }, this.localizedMinuteSuffix), this.showSecond && (index.h("div", { class: CSS.column, role: "group" }, index.h("span", { "aria-label": this.intlSecondUp, class: {
        [CSS.button]: true,
        [CSS.buttonSecondUp]: true
      }, onClick: this.incrementSecond, onKeyDown: this.secondUpButtonKeyDownHandler, role: "button", tabIndex: -1 }, index.h("calcite-icon", { icon: "chevron-up", scale: iconScale })), index.h("span", { "aria-label": this.intlSecond, "aria-valuemax": "59", "aria-valuemin": "0", "aria-valuenow": (secondIsNumber && parseInt(this.second)) || "0", "aria-valuetext": this.second, class: {
        [CSS.input]: true,
        [CSS.second]: true
      }, onFocus: this.focusHandler, onKeyDown: this.secondKeyDownHandler, ref: this.setSecondEl, role: "spinbutton", tabIndex: 0 }, this.localizedSecond || "--"), index.h("span", { "aria-label": this.intlSecondDown, class: {
        [CSS.button]: true,
        [CSS.buttonSecondDown]: true
      }, onClick: this.decrementSecond, onKeyDown: this.secondDownButtonKeyDownHandler, role: "button", tabIndex: -1 }, index.h("calcite-icon", { icon: "chevron-down", scale: iconScale })))), this.localizedSecondSuffix && (index.h("span", { class: CSS.delimiter }, this.localizedSecondSuffix)), showMeridiem && (index.h("div", { class: {
        [CSS.column]: true,
        [CSS.meridiemStart]: this.meridiemOrder === 0
      }, role: "group" }, index.h("span", { "aria-label": this.intlMeridiemUp, class: {
        [CSS.button]: true,
        [CSS.buttonMeridiemUp]: true,
        [CSS.buttonTopRight]: true
      }, onClick: this.incrementMeridiem, onKeyDown: this.meridiemUpButtonKeyDownHandler, role: "button", tabIndex: -1 }, index.h("calcite-icon", { icon: "chevron-up", scale: iconScale })), index.h("span", { "aria-label": this.intlMeridiem, "aria-valuemax": "2", "aria-valuemin": "1", "aria-valuenow": (this.meridiem === "PM" && "2") || "1", "aria-valuetext": this.meridiem, class: {
        [CSS.input]: true,
        [CSS.meridiem]: true
      }, onFocus: this.focusHandler, onKeyDown: this.meridiemKeyDownHandler, ref: this.setMeridiemEl, role: "spinbutton", tabIndex: 0 }, this.localizedMeridiem || "--"), index.h("span", { "aria-label": this.intlMeridiemDown, class: {
        [CSS.button]: true,
        [CSS.buttonMeridiemDown]: true,
        [CSS.buttonBottomRight]: true
      }, onClick: this.decrementMeridiem, onKeyDown: this.meridiemDownButtonKeyDownHandler, role: "button", tabIndex: -1 }, index.h("calcite-icon", { icon: "chevron-down", scale: iconScale }))))));
  }
  get el() { return index.getElement(this); }
  static get watchers() { return {
    "locale": ["localeChanged"],
    "value": ["valueWatcher"],
    "effectiveLocale": ["effectiveLocaleWatcher"]
  }; }
};
TimePicker.style = timePickerCss;

exports.calcite_input_time_picker = InputTimePicker;
exports.calcite_time_picker = TimePicker;
