/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { proxyCustomElement, HTMLElement, createEvent, h, Host } from '@stencil/core/internal/client/index.js';
import { I as ICON_TYPES } from './resources.js';
import { g as guid } from './guid.js';
import { S as SLOTS$1, C as CSS, d as defineCustomElement$2 } from './pick-list-item.js';
import { b as getSlotted } from './dom.js';
import { c as connectConditionalSlotComponent, d as disconnectConditionalSlotComponent } from './conditionalSlot.js';
import { u as updateHostInteraction } from './interactive.js';
import { d as defineCustomElement$5 } from './action.js';
import { d as defineCustomElement$4 } from './icon.js';
import { d as defineCustomElement$3 } from './loader.js';

const ICONS = {
  drag: "drag"
};
const SLOTS = {
  actionsEnd: "actions-end",
  actionsStart: "actions-start"
};

const valueListItemCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{margin-block-end:1px;box-sizing:border-box;display:flex;background-color:var(--calcite-ui-foreground-1);font-size:var(--calcite-font-size--1);color:var(--calcite-ui-text-2);--tw-shadow:0 1px 0 var(--calcite-ui-border-3);--tw-shadow-colored:0 1px 0 var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow);transition:background-color var(--calcite-animation-timing), box-shadow var(--calcite-animation-timing)}:host *{box-sizing:border-box}calcite-pick-list-item{position:relative;margin:0px;flex-grow:1;--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)}:host([active]),:host([selected]){--tw-shadow:0 0 0 1px var(--calcite-ui-brand);--tw-shadow-colored:0 0 0 1px var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)}.handle{display:flex;cursor:move;align-items:center;justify-content:center;border-style:none;background-color:transparent;padding-block:0px;padding-inline:0.25rem;color:var(--calcite-ui-border-input);outline-color:transparent}.handle:hover{background-color:var(--calcite-ui-foreground-2);color:var(--calcite-ui-text-1)}.handle:focus{outline:2px solid var(--calcite-ui-brand);outline-offset:-2px}.handle--activated{background-color:var(--calcite-ui-foreground-3);color:var(--calcite-ui-text-1)}.handle calcite-icon{color:inherit}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}";

const ValueListItem = /*@__PURE__*/ proxyCustomElement(class extends HTMLElement {
  constructor() {
    super();
    this.__registerHost();
    this.__attachShadow();
    this.calciteListItemRemove = createEvent(this, "calciteListItemRemove", 7);
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    /**
     * @internal
     */
    this.disableDeselect = false;
    /**
     * When `true`, prevents the content of the component from user interaction.
     */
    this.nonInteractive = false;
    /**
     * @internal
     */
    this.handleActivated = false;
    /**
     * Determines the icon SVG symbol that will be shown. Options are circle, square, grip or null.
     *
     * @see [ICON_TYPES](https://github.com/Esri/calcite-components/blob/master/src/components/pick-list/resources.ts#L5)
     */
    this.icon = null;
    /**
     * When `true`, adds an action to remove the component.
     */
    this.removable = false;
    /**
     * When `true`, the component is selected.
     */
    this.selected = false;
    this.pickListItem = null;
    this.guid = `calcite-value-list-item-${guid()}`;
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.getPickListRef = (el) => (this.pickListItem = el);
    this.handleKeyDown = (event) => {
      if (event.key === " ") {
        this.handleActivated = !this.handleActivated;
      }
    };
    this.handleBlur = () => {
      this.handleActivated = false;
    };
    this.handleSelectChange = (event) => {
      this.selected = event.detail.selected;
    };
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    connectConditionalSlotComponent(this);
  }
  disconnectedCallback() {
    disconnectConditionalSlotComponent(this);
  }
  componentDidRender() {
    updateHostInteraction(this, this.el.closest("calcite-value-list") ? "managed" : false);
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /**
   * Toggle the selection state. By default this won't trigger an event.
   * The first argument allows the value to be coerced, rather than swapping values.
   *
   * @param coerce
   */
  async toggleSelected(coerce) {
    this.pickListItem.toggleSelected(coerce);
  }
  /** Set focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.pickListItem) === null || _a === void 0 ? void 0 : _a.setFocus();
  }
  calciteListItemChangeHandler(event) {
    // adjust item payload from wrapped item before bubbling
    event.detail.item = this.el;
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderActionsEnd() {
    const { el } = this;
    const hasActionsEnd = getSlotted(el, SLOTS.actionsEnd);
    return hasActionsEnd ? (h("slot", { name: SLOTS.actionsEnd, slot: SLOTS$1.actionsEnd })) : null;
  }
  renderActionsStart() {
    const { el } = this;
    const hasActionsStart = getSlotted(el, SLOTS.actionsStart);
    return hasActionsStart ? (h("slot", { name: SLOTS.actionsStart, slot: SLOTS$1.actionsStart })) : null;
  }
  renderHandle() {
    const { icon } = this;
    if (icon === ICON_TYPES.grip) {
      return (h("span", { class: {
          [CSS.handle]: true,
          [CSS.handleActivated]: this.handleActivated
        }, "data-js-handle": true, onBlur: this.handleBlur, onKeyDown: this.handleKeyDown, role: "button", tabindex: "0" }, h("calcite-icon", { icon: ICONS.drag, scale: "s" })));
    }
  }
  render() {
    return (h(Host, { id: this.el.id || this.guid }, this.renderHandle(), h("calcite-pick-list-item", { description: this.description, disableDeselect: this.disableDeselect, disabled: this.disabled, label: this.label, metadata: this.metadata, nonInteractive: this.nonInteractive, onCalciteListItemChange: this.handleSelectChange, ref: this.getPickListRef, removable: this.removable, selected: this.selected, value: this.value }, this.renderActionsStart(), this.renderActionsEnd())));
  }
  get el() { return this; }
  static get style() { return valueListItemCss; }
}, [1, "calcite-value-list-item", {
    "description": [513],
    "disabled": [516],
    "disableDeselect": [4, "disable-deselect"],
    "nonInteractive": [516, "non-interactive"],
    "handleActivated": [1028, "handle-activated"],
    "icon": [513],
    "label": [513],
    "metadata": [16],
    "removable": [516],
    "selected": [1540],
    "value": [8],
    "toggleSelected": [64],
    "setFocus": [64]
  }, [[0, "calciteListItemChange", "calciteListItemChangeHandler"]]]);
function defineCustomElement$1() {
  if (typeof customElements === "undefined") {
    return;
  }
  const components = ["calcite-value-list-item", "calcite-action", "calcite-icon", "calcite-loader", "calcite-pick-list-item"];
  components.forEach(tagName => { switch (tagName) {
    case "calcite-value-list-item":
      if (!customElements.get(tagName)) {
        customElements.define(tagName, ValueListItem);
      }
      break;
    case "calcite-action":
      if (!customElements.get(tagName)) {
        defineCustomElement$5();
      }
      break;
    case "calcite-icon":
      if (!customElements.get(tagName)) {
        defineCustomElement$4();
      }
      break;
    case "calcite-loader":
      if (!customElements.get(tagName)) {
        defineCustomElement$3();
      }
      break;
    case "calcite-pick-list-item":
      if (!customElements.get(tagName)) {
        defineCustomElement$2();
      }
      break;
  } });
}
defineCustomElement$1();

const CalciteValueListItem = ValueListItem;
const defineCustomElement = defineCustomElement$1;

export { CalciteValueListItem, defineCustomElement };
