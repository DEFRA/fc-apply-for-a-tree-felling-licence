/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { proxyCustomElement, HTMLElement, createEvent, h, Host } from '@stencil/core/internal/client/index.js';
import { o as closestElementCrossShadowBoundary, c as getElementDir, C as CSS_UTILITY } from './dom.js';
import { u as updateHostInteraction } from './interactive.js';
import { i as isActivationKey } from './key.js';
import { n as numberStringFormatter } from './locale.js';

const datePickerDayCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{display:flex;min-inline-size:0px;cursor:pointer;justify-content:center;color:var(--calcite-ui-text-3);inline-size:14.2857142857%}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}.day-v-wrapper{flex:1 1 auto}.day-wrapper{display:flex;flex-direction:column;align-items:center}.day{display:flex;align-items:center;justify-content:center;border-radius:9999px;font-size:var(--calcite-font-size--2);line-height:1rem;line-height:1;color:var(--calcite-ui-text-3);opacity:var(--calcite-ui-opacity-disabled);outline-color:transparent;transition:all var(--calcite-animation-timing) ease-in-out 0s, outline 0s, outline-offset 0s;background:none;box-shadow:0 0 0 2px transparent}.text{margin-block:1px 0px;margin-inline-start:0px}:host([scale=s]) .day-v-wrapper{padding-block:0.125rem}:host([scale=s]) .day-wrapper{padding:0px}:host([scale=s]) .day{block-size:27px;inline-size:27px;font-size:var(--calcite-font-size--2)}:host([scale=m]) .day-v-wrapper{padding-block:0.25rem}:host([scale=m]) .day-wrapper{padding-inline:0.25rem}:host([scale=m]) .day{block-size:33px;inline-size:33px;font-size:var(--calcite-font-size--1)}:host([scale=l]) .day-v-wrapper{padding-block:0.25rem}:host([scale=l]) .day-wrapper{padding-inline:0.25rem}:host([scale=l]) .day{block-size:43px;inline-size:43px;font-size:var(--calcite-font-size-0)}:host([current-month]) .day{opacity:1}:host(:hover:not([disabled])) .day,:host([active]:not([range])) .day{background-color:var(--calcite-ui-foreground-2);color:var(--calcite-ui-text-1)}:host(:focus),:host([active]){outline:2px solid transparent;outline-offset:2px}:host(:focus:not([disabled])) .day{outline:2px solid var(--calcite-ui-brand);outline-offset:2px;box-shadow:0 0 0 2px var(--calcite-ui-foreground-1)}:host([selected]) .day{font-weight:var(--calcite-font-weight-medium);background-color:var(--calcite-ui-brand) !important;color:var(--calcite-ui-foreground-1) !important}:host([range][selected]) .day-wrapper{background-color:var(--calcite-ui-foreground-current)}:host([start-of-range]) .day-wrapper{border-start-start-radius:40%;border-end-start-radius:40%}:host([end-of-range]) .day-wrapper{border-start-end-radius:40%;border-end-end-radius:40%}:host([start-of-range]) :not(.calcite--rtl) .day-wrapper{box-shadow:inset 4px 0 var(--calcite-ui-foreground-1)}:host([start-of-range]) .calcite--rtl .day-wrapper{box-shadow:inset -4px 0 var(--calcite-ui-foreground-1)}:host([start-of-range]) .day{opacity:1}:host([end-of-range]) :not(.calcite--rtl) .day-wrapper{box-shadow:inset -4px 0 var(--calcite-ui-foreground-1)}:host([end-of-range]) .calcite--rtl .day-wrapper{box-shadow:inset 4px 0 var(--calcite-ui-foreground-1)}:host([end-of-range]) .day{opacity:1}:host([start-of-range]:not(:focus)) :not(.calcite--rtl) .day{box-shadow:0 0 0 2px var(--calcite-ui-foreground-1)}:host([start-of-range]:not(:focus)) .calcite--rtl .day{box-shadow:0 0 0 -2px var(--calcite-ui-foreground-1)}:host([end-of-range]:not(:focus)) :not(.calcite--rtl) .day{box-shadow:0 0 0 -2px var(--calcite-ui-foreground-1)}:host([end-of-range]:not(:focus)) .calcite--rtl .day{box-shadow:0 0 0 2px var(--calcite-ui-foreground-1)}:host([start-of-range][scale=l]) :not(.calcite--rtl) .day-wrapper{box-shadow:inset 8px 0 var(--calcite-ui-foreground-1)}:host([start-of-range][scale=l]) .calcite--rtl .day-wrapper{box-shadow:inset -8px 0 var(--calcite-ui-foreground-1)}:host([end-of-range][scale=l]) :not(.calcite--rtl) .day-wrapper{box-shadow:inset -8px 0 var(--calcite-ui-foreground-1)}:host([end-of-range][scale=l]) .calcite--rtl .day-wrapper{box-shadow:inset 8px 0 var(--calcite-ui-foreground-1)}:host([highlighted]) .day-wrapper{background-color:var(--calcite-ui-foreground-current)}:host([highlighted]) .day-wrapper .day{color:var(--calcite-ui-text-1)}:host([highlighted]:not([active]:focus)) .day{border-radius:0px;color:var(--calcite-ui-text-1)}:host([range-hover]:not([selected])) .day-wrapper{background-color:var(--calcite-ui-foreground-2)}:host([range-hover]:not([selected])) .day{border-radius:0px}:host([start-of-range][range-hover]) :not(.calcite--rtl) .day-wrapper{background-image:linear-gradient(to left, var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-2), var(--calcite-ui-foreground-2));border-radius:0px;--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)}:host([start-of-range][range-hover]) .calcite--rtl .day-wrapper{background-image:linear-gradient(to right, var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-2), var(--calcite-ui-foreground-2));border-radius:0px;--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)}:host([end-of-range][range-hover]) :not(.calcite--rtl) .day-wrapper{background-image:linear-gradient(to right, var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-2), var(--calcite-ui-foreground-2));border-radius:0px;--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)}:host([end-of-range][range-hover]) .calcite--rtl .day-wrapper{background-image:linear-gradient(to left, var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-2), var(--calcite-ui-foreground-2));border-radius:0px;--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)}:host(:hover[end-of-range][range-hover]) :not(.calcite--rtl) .day-wrapper,:host(:hover[start-of-range][range-hover]) .calcite--rtl .day-wrapper{background-image:linear-gradient(to right, var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-1), var(--calcite-ui-foreground-1));border-radius:0px;--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)}:host(:hover[start-of-range][range-hover]) :not(.calcite--rtl) .day-wrapper,:host(:hover[end-of-range][range-hover]) .calcite--rtl .day-wrapper{background-image:linear-gradient(to left, var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-1), var(--calcite-ui-foreground-1));border-radius:0px;--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)}:host(:hover[range-hover]:not([selected]).focused--start) :not(.calcite--rtl) .day-wrapper{background-image:linear-gradient(to right, var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-2), var(--calcite-ui-foreground-2))}:host(:hover[range-hover]:not([selected]).focused--start) .calcite--rtl .day-wrapper{background-image:linear-gradient(to right, var(--calcite-ui-foreground-2), var(--calcite-ui-foreground-2), var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-current))}:host(:hover[range-hover]:not([selected]).focused--start) .day{border-radius:9999px;opacity:1;box-shadow:0 0 0 2px var(--calcite-ui-foreground-1)}:host(:hover[range-hover]:not([selected]).focused--end) :not(.calcite--rtl) .day-wrapper{background-image:linear-gradient(to right, var(--calcite-ui-foreground-2), var(--calcite-ui-foreground-2), var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-current))}:host(:hover[range-hover]:not([selected]).focused--end) .calcite--rtl .day-wrapper{background-image:linear-gradient(to right, var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-current), var(--calcite-ui-foreground-2), var(--calcite-ui-foreground-2))}:host(:hover[range-hover]:not([selected]).focused--end) .day{border-radius:9999px;opacity:1;box-shadow:0 0 0 2px var(--calcite-ui-foreground-1)}:host(:hover[range-hover]:not([selected]).focused--start.hover--outside-range) :not(.calcite--rtl) .day-wrapper,:host(:hover[range-hover]:not([selected]).focused--end.hover--outside-range) .calcite--rtl .day-wrapper{background-image:linear-gradient(to right, var(--calcite-ui-foreground-1), var(--calcite-ui-foreground-1), var(--calcite-ui-foreground-2), var(--calcite-ui-foreground-2))}:host(:hover[range-hover]:not([selected]).focused--start.hover--outside-range) :not(.calcite--rtl) .day,:host(:hover[range-hover]:not([selected]).focused--end.hover--outside-range) .calcite--rtl .day{border-radius:9999px;opacity:1;box-shadow:0 0 0 2px var(--calcite-ui-foreground-1)}:host(:hover[range-hover]:not([selected]).focused--end.hover--outside-range) :not(.calcite--rtl) .day-wrapper,:host(:hover[range-hover]:not([selected]).focused--start.hover--outside-range) .calcite--rtl .day-wrapper{background-image:linear-gradient(to right, var(--calcite-ui-foreground-2), var(--calcite-ui-foreground-2), var(--calcite-ui-foreground-1), var(--calcite-ui-foreground-1))}:host(:hover[range-hover]:not([selected]).focused--end.hover--outside-range) :not(.calcite--rtl) .day,:host(:hover[range-hover]:not([selected]).focused--start.hover--outside-range) .calcite--rtl .day{border-radius:9999px;opacity:1;box-shadow:0 0 0 2px var(--calcite-ui-foreground-1)}:host(:hover[start-of-range].hover--inside-range.focused--end) .day-wrapper,:host(:hover[end-of-range].hover--inside-range.focused--start) .day-wrapper{background-image:none}:host([start-of-range].hover--inside-range.focused--end) .day-wrapper,:host([end-of-range].hover--inside-range.focused--start) .day-wrapper{background-color:var(--calcite-ui-foreground-2)}:host([highlighted]:last-child) :not(.calcite--rtl) .day-wrapper,:host([range-hover]:last-child) :not(.calcite--rtl) .day-wrapper,:host([highlighted]:first-child) .calcite--rtl .day-wrapper,:host([range-hover]:first-child) .calcite--rtl .day-wrapper{box-shadow:inset -4px 0px 0px 0px var(--calcite-ui-foreground-1)}:host([highlighted]:first-child) :not(.calcite--rtl) .day-wrapper,:host([range-hover]:first-child) :not(.calcite--rtl) .day-wrapper,:host([highlighted]:last-child) .calcite--rtl .day-wrapper,:host([range-hover]:last-child) .calcite--rtl .day-wrapper{box-shadow:inset 4px 0px 0px 0px var(--calcite-ui-foreground-1)}:host([scale=s][highlighted]:last-child) :not(.calcite--rtl) .day-wrapper,:host([scale=s][range-hover]:last-child) :not(.calcite--rtl) .day-wrapper,:host([scale=s][highlighted]:first-child) .calcite--rtl .day-wrapper,:host([scale=s][range-hover]:first-child) .calcite--rtl .day-wrapper{box-shadow:inset -1px 0px 0px 0px var(--calcite-ui-foreground-1)}:host([scale=s][highlighted]:first-child) :not(.calcite--rtl) .day-wrapper,:host([scale=s][range-hover]:first-child) :not(.calcite--rtl) .day-wrapper,:host([scale=s][highlighted]:last-child) .calcite--rtl .day-wrapper,:host([scale=s][range-hover]:last-child) .calcite--rtl .day-wrapper{box-shadow:inset 1px 0px 0px 0px var(--calcite-ui-foreground-1)}:host([scale=l][highlighted]:first-child) :not(.calcite--rtl) .day-wrapper,:host([scale=l][range-hover]:first-child) :not(.calcite--rtl) .day-wrapper,:host([scale=l][highlighted]:last-child) .calcite--rtl .day-wrapper,:host([scale=l][range-hover]:last-child) .calcite--rtl .day-wrapper{box-shadow:inset 6px 0px 0px 0px var(--calcite-ui-foreground-1)}:host([highlighted]:first-child) .day-wrapper,:host([range-hover]:first-child) .day-wrapper{border-start-start-radius:45%;border-end-start-radius:45%}:host([highlighted]:last-child) .day-wrapper,:host([range-hover]:last-child) .day-wrapper{border-start-end-radius:45%;border-end-end-radius:45%}:host([scale=l][highlighted]:last-child) :not(.calcite--rtl) .day-wrapper,:host([scale=l][range-hover]:last-child) :not(.calcite--rtl) .day-wrapper,:host([scale=l][highlighted]:first-child) .calcite--rtl .day-wrapper,:host([scale=l][range-hover]:first-child) .calcite--rtl .day-wrapper{box-shadow:inset -6px 0px 0px 0px var(--calcite-ui-foreground-1)}@media (forced-colors: active){:host(:hover:not([disabled])) .day,:host([active]:not([range])) .day{border-radius:0px}:host([selected]){outline:2px solid canvasText}:host([selected]) .day{border-radius:0px;background-color:highlight}:host([range][selected]) .day-wrapper,:host([highlighted]) .day-wrapper,:host([range-hover]:not([selected])) .day-wrapper{background-color:highlight}:host([range][selected][start-of-range]) .day-wrapper,:host([range][selected][end-of-range]) .day-wrapper{background-color:canvas}}";

const DatePickerDay = /*@__PURE__*/ proxyCustomElement(class extends HTMLElement {
  constructor() {
    super();
    this.__registerHost();
    this.__attachShadow();
    this.calciteDaySelect = createEvent(this, "calciteDaySelect", 6);
    this.calciteInternalDayHover = createEvent(this, "calciteInternalDayHover", 6);
    /** Date is outside of range and can't be selected */
    this.disabled = false;
    /** Date is in the current month. */
    this.currentMonth = false;
    /** Date is the current selected date of the picker */
    this.selected = false;
    /** Date is currently highlighted as part of the range */
    this.highlighted = false;
    /** Showing date range */
    this.range = false;
    /** Date is the start of date range */
    this.startOfRange = false;
    /** Date is the end of date range */
    this.endOfRange = false;
    /** Date is being hovered and within the set range */
    this.rangeHover = false;
    /** Date is actively in focus for keyboard navigation */
    this.active = false;
    //--------------------------------------------------------------------------
    //
    //  Event Listeners
    //
    //--------------------------------------------------------------------------
    this.onClick = () => {
      !this.disabled && this.calciteDaySelect.emit();
    };
    this.keyDownHandler = (event) => {
      if (isActivationKey(event.key)) {
        !this.disabled && this.calciteDaySelect.emit();
        event.preventDefault();
      }
    };
  }
  mouseoverHandler() {
    this.calciteInternalDayHover.emit();
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentWillLoad() {
    this.parentDatePickerEl = closestElementCrossShadowBoundary(this.el, "calcite-date-picker");
  }
  render() {
    if (this.parentDatePickerEl) {
      const { numberingSystem, lang: locale } = this.parentDatePickerEl;
      numberStringFormatter.numberFormatOptions = {
        useGrouping: false,
        ...(numberingSystem && { numberingSystem }),
        ...(locale && { locale })
      };
    }
    const formattedDay = numberStringFormatter.localize(String(this.day));
    const dir = getElementDir(this.el);
    return (h(Host, { onClick: this.onClick, onKeyDown: this.keyDownHandler, role: "gridcell" }, h("div", { class: { "day-v-wrapper": true, [CSS_UTILITY.rtl]: dir === "rtl" } }, h("div", { class: "day-wrapper" }, h("span", { class: "day" }, h("span", { class: "text" }, formattedDay))))));
  }
  componentDidRender() {
    updateHostInteraction(this, this.isTabbable);
  }
  isTabbable() {
    return this.active;
  }
  get el() { return this; }
  static get style() { return datePickerDayCss; }
}, [1, "calcite-date-picker-day", {
    "day": [2],
    "disabled": [516],
    "currentMonth": [516, "current-month"],
    "selected": [516],
    "highlighted": [516],
    "range": [516],
    "startOfRange": [516, "start-of-range"],
    "endOfRange": [516, "end-of-range"],
    "rangeHover": [516, "range-hover"],
    "active": [516],
    "scale": [513],
    "value": [16]
  }, [[1, "pointerover", "mouseoverHandler"]]]);
function defineCustomElement() {
  if (typeof customElements === "undefined") {
    return;
  }
  const components = ["calcite-date-picker-day"];
  components.forEach(tagName => { switch (tagName) {
    case "calcite-date-picker-day":
      if (!customElements.get(tagName)) {
        customElements.define(tagName, DatePickerDay);
      }
      break;
  } });
}
defineCustomElement();

export { DatePickerDay as D, defineCustomElement as d };
