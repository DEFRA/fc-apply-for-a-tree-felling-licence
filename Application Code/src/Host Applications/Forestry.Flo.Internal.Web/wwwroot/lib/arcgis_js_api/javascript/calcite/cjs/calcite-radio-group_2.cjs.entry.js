/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
'use strict';

Object.defineProperty(exports, '__esModule', { value: true });

const index = require('./index-b27b231c.js');
const dom = require('./dom-2b919cb6.js');
const label = require('./label-5d4931f7.js');
const form = require('./form-45498958.js');
const interactive = require('./interactive-3d681fb9.js');
require('./resources-3fd6da1b.js');
require('./guid-acbbb0e7.js');

const radioGroupCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{display:flex;background-color:var(--calcite-ui-foreground-1);inline-size:-moz-fit-content;inline-size:fit-content;outline:1px solid var(--calcite-ui-border-input);outline-offset:-1px}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}:host([layout=vertical]){flex-direction:column;align-items:flex-start;align-self:flex-start}:host([width=full]){inline-size:100%;min-inline-size:-moz-fit-content;min-inline-size:fit-content}:host([width=full]) ::slotted(calcite-radio-group-item){flex:1 1 auto}:host([width=full][layout=vertical]) ::slotted(calcite-radio-group-item){justify-content:flex-start}::slotted(input[slot=hidden-form-input]){margin:0 !important;opacity:0 !important;outline:none !important;padding:0 !important;position:absolute !important;inset:0 !important;transform:none !important;-webkit-appearance:none !important;z-index:-1 !important}";

const RadioGroup = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteRadioGroupChange = index.createEvent(this, "calciteRadioGroupChange", 6);
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /** Specifies the appearance style of the component. */
    this.appearance = "solid";
    /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
    this.disabled = false;
    /**
     * When `true`, the component must have a value in order for the form to submit.
     *
     * @internal
     */
    this.required = false;
    /** Defines the layout of the component. */
    this.layout = "horizontal";
    /** Specifies the size of the component. */
    this.scale = "m";
    /** The component's `selectedItem` value. */
    this.value = null;
    /** Specifies the width of the component. */
    this.width = "auto";
    //--------------------------------------------------------------------------
    //
    //  Event Listeners
    //
    //--------------------------------------------------------------------------
    this.handleClick = (event) => {
      if (event.target.localName === "calcite-radio-group-item") {
        this.selectItem(event.target, true);
      }
    };
  }
  valueHandler(value) {
    const items = this.getItems();
    items.forEach((item) => (item.checked = item.value === value));
  }
  handleSelectedItemChange(newItem, oldItem) {
    this.value = newItem === null || newItem === void 0 ? void 0 : newItem.value;
    if (newItem === oldItem) {
      return;
    }
    const items = this.getItems();
    const match = Array.from(items)
      .filter((item) => item === newItem)
      .pop();
    if (match) {
      this.selectItem(match);
    }
    else if (items[0]) {
      items[0].tabIndex = 0;
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentWillLoad() {
    const items = this.getItems();
    const lastChecked = Array.from(items)
      .filter((item) => item.checked)
      .pop();
    if (lastChecked) {
      this.selectItem(lastChecked);
    }
    else if (items[0]) {
      items[0].tabIndex = 0;
    }
  }
  componentDidLoad() {
    form.afterConnectDefaultValueSet(this, this.value);
  }
  connectedCallback() {
    label.connectLabel(this);
    form.connectForm(this);
  }
  disconnectedCallback() {
    label.disconnectLabel(this);
    form.disconnectForm(this);
  }
  componentDidRender() {
    interactive.updateHostInteraction(this);
  }
  render() {
    return (index.h(index.Host, { onClick: this.handleClick, role: "radiogroup" }, index.h("slot", null), index.h(form.HiddenFormInputSlot, { component: this })));
  }
  handleSelected(event) {
    event.preventDefault();
    this.selectItem(event.target);
    event.stopPropagation();
  }
  handleKeyDown(event) {
    const keys = ["ArrowLeft", "ArrowUp", "ArrowRight", "ArrowDown", " "];
    const { key } = event;
    const { el, selectedItem } = this;
    if (keys.indexOf(key) === -1) {
      return;
    }
    let adjustedKey = key;
    if (dom.getElementDir(el) === "rtl") {
      if (key === "ArrowRight") {
        adjustedKey = "ArrowLeft";
      }
      if (key === "ArrowLeft") {
        adjustedKey = "ArrowRight";
      }
    }
    const items = this.getItems();
    let selectedIndex = -1;
    items.forEach((item, index) => {
      if (item === selectedItem) {
        selectedIndex = index;
      }
    });
    switch (adjustedKey) {
      case "ArrowLeft":
      case "ArrowUp":
        event.preventDefault();
        const previous = selectedIndex < 1 ? items.item(items.length - 1) : items.item(selectedIndex - 1);
        this.selectItem(previous, true);
        return;
      case "ArrowRight":
      case "ArrowDown":
        event.preventDefault();
        const next = selectedIndex === -1 ? items.item(1) : items.item(selectedIndex + 1) || items.item(0);
        this.selectItem(next, true);
        return;
      case " ":
        event.preventDefault();
        this.selectItem(event.target, true);
        return;
      default:
        return;
    }
  }
  // --------------------------------------------------------------------------
  //
  //  Methods
  //
  // --------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    var _a;
    (_a = (this.selectedItem || this.getItems()[0])) === null || _a === void 0 ? void 0 : _a.focus();
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  onLabelClick() {
    this.setFocus();
  }
  getItems() {
    return this.el.querySelectorAll("calcite-radio-group-item");
  }
  selectItem(selected, emit = false) {
    if (selected === this.selectedItem) {
      return;
    }
    const items = this.getItems();
    let match = null;
    items.forEach((item) => {
      const matches = item.value === selected.value;
      if ((matches && !item.checked) || (!matches && item.checked)) {
        item.checked = matches;
      }
      item.tabIndex = matches ? 0 : -1;
      if (matches) {
        match = item;
        if (emit) {
          this.calciteRadioGroupChange.emit(match.value);
        }
      }
    });
    this.selectedItem = match;
    if (match) {
      match.focus();
    }
  }
  get el() { return index.getElement(this); }
  static get watchers() { return {
    "value": ["valueHandler"],
    "selectedItem": ["handleSelectedItemChange"]
  }; }
};
RadioGroup.style = radioGroupCss;

const SLOTS = {
  input: "input"
};
const CSS = {
  radioGroupItemIcon: "radio-group-item-icon"
};

const radioGroupItemCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host{display:flex;cursor:pointer;align-self:stretch;font-weight:var(--calcite-font-weight-normal);transition:background-color var(--calcite-internal-animation-timing-fast) ease-in-out, border-color var(--calcite-animation-timing) ease-in-out}:host label{pointer-events:none;margin:0.125rem;box-sizing:border-box;display:flex;flex:1 1 0%;align-items:center;color:var(--calcite-ui-text-3);transition:background-color var(--calcite-internal-animation-timing-fast) ease-in-out, border-color var(--calcite-internal-animation-timing-fast) ease-in-out, color var(--calcite-internal-animation-timing-fast) ease-in-out}.label--horizontal{justify-content:center}:host{outline-color:transparent}:host(:focus){outline:2px solid var(--calcite-ui-brand);outline-offset:-1px}.label--scale-s{padding-inline:0.5rem;font-size:var(--calcite-font-size--2);line-height:1rem;padding-block:0.125rem}.label--scale-m{padding-inline:0.75rem;font-size:var(--calcite-font-size--1);line-height:1rem;padding-block:0.375rem}.label--scale-l{padding-inline:1rem;padding-block:0.625rem;font-size:var(--calcite-font-size-0);line-height:1.25rem}:host(:hover) label{background-color:var(--calcite-ui-foreground-2);color:var(--calcite-ui-text-1)}:host(:active) label{background-color:var(--calcite-ui-foreground-3)}:host([checked]) label{cursor:default;border-color:var(--calcite-ui-brand);background-color:var(--calcite-ui-brand);color:var(--calcite-ui-background)}:host([checked]) .label--outline{border-color:var(--calcite-ui-brand);background-color:var(--calcite-ui-foreground-1);box-shadow:inset 0 0 0 1px var(--calcite-ui-brand);color:var(--calcite-ui-brand)}::slotted(input){display:none}@media (forced-colors: active){:host([checked]) label{background-color:highlight}:host([checked]) .label--outline{outline:2px solid transparent;outline-offset:2px}:host([checked]) label:not([class~=label--outline]) .radio-group-item-icon{color:highlightText}}.radio-group-item-icon{position:relative;margin:0px;display:inline-flex;line-height:inherit}:host([icon-position=start]) .label--scale-s .radio-group-item-icon{margin-inline-end:0.5rem}:host([icon-position=end]) .label--scale-s .radio-group-item-icon{margin-inline-end:unset;margin-inline-start:0.5rem}:host([icon-position=start]) .label--scale-m .radio-group-item-icon{margin-inline-end:0.75rem}:host([icon-position=end]) .label--scale-m .radio-group-item-icon{margin-inline-end:unset;margin-inline-start:0.75rem}:host([icon-position=start]) .label--scale-l .radio-group-item-icon{margin-inline-end:1rem}:host([icon-position=end]) .label--scale-l .radio-group-item-icon{margin-inline-end:unset;margin-inline-start:1rem}:host([icon-start]) .label--scale-s .radio-group-item-icon{margin-inline-end:0.5rem}:host([icon-end]) .label--scale-s .radio-group-item-icon{margin-inline-start:0.5rem}:host([icon-start]) .label--scale-m .radio-group-item-icon{margin-inline-end:0.75rem}:host([icon-end]) .label--scale-m .radio-group-item-icon{margin-inline-start:0.75rem}:host([icon-start]) .label--scale-l .radio-group-item-icon{margin-inline-end:1rem}:host([icon-end]) .label--scale-l .radio-group-item-icon{margin-inline-start:1rem}";

const RadioGroupItem = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteInternalRadioGroupItemChange = index.createEvent(this, "calciteInternalRadioGroupItemChange", 6);
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /** When `true`, the component is checked. */
    this.checked = false;
    /** When `true`, the icon will be flipped when the element direction is right-to-left (`"rtl"`). */
    this.iconFlipRtl = false;
    /**
     * Specifies the placement of the icon.
     *
     * @deprecated Use either `iconStart` or `iconEnd` but do not combine them with `icon` and `iconPosition`.
     */
    this.iconPosition = "start";
  }
  handleCheckedChange() {
    this.calciteInternalRadioGroupItemChange.emit();
  }
  render() {
    const { checked, value } = this;
    const scale = dom.getElementProp(this.el, "scale", "m");
    const appearance = dom.getElementProp(this.el, "appearance", "solid");
    const layout = dom.getElementProp(this.el, "layout", "horizontal");
    const iconStartEl = this.iconStart ? (index.h("calcite-icon", { class: CSS.radioGroupItemIcon, flipRtl: this.iconFlipRtl, icon: this.iconStart, key: "icon-start", scale: "s" })) : null;
    const iconEndEl = this.iconEnd ? (index.h("calcite-icon", { class: CSS.radioGroupItemIcon, flipRtl: this.iconFlipRtl, icon: this.iconEnd, key: "icon-end", scale: "s" })) : null;
    const iconEl = (index.h("calcite-icon", { class: CSS.radioGroupItemIcon, flipRtl: this.iconFlipRtl, icon: this.icon, key: "icon", scale: "s" }));
    const iconAtStart = this.icon && this.iconPosition === "start" && !this.iconStart ? iconEl : null;
    const iconAtEnd = this.icon && this.iconPosition === "end" && !this.iconEnd ? iconEl : null;
    return (index.h(index.Host, { "aria-checked": dom.toAriaBoolean(checked), "aria-label": value, role: "radio" }, index.h("label", { class: {
        "label--scale-s": scale === "s",
        "label--scale-m": scale === "m",
        "label--scale-l": scale === "l",
        "label--horizontal": layout === "horizontal",
        "label--outline": appearance === "outline"
      } }, iconAtStart, this.iconStart ? iconStartEl : null, index.h("slot", null, value), index.h("slot", { name: SLOTS.input }), iconAtEnd, this.iconEnd ? iconEndEl : null)));
  }
  get el() { return index.getElement(this); }
  static get watchers() { return {
    "checked": ["handleCheckedChange"]
  }; }
};
RadioGroupItem.style = radioGroupItemCss;

exports.calcite_radio_group = RadioGroup;
exports.calcite_radio_group_item = RadioGroupItem;
