/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { proxyCustomElement, HTMLElement, createEvent, h } from '@stencil/core/internal/client/index.js';
import { f as focusElement } from './dom.js';

const stepperCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host{position:relative;display:flex;inline-size:100%;min-inline-size:-moz-fit-content;min-inline-size:fit-content;flex-direction:row;flex-wrap:wrap;align-items:stretch;justify-content:space-between}:host([layout=vertical]){flex:1 1 auto;flex-direction:column}:host([layout=horizontal]){display:grid;grid-template-areas:\"items\" \"content\"}";

const Stepper = /*@__PURE__*/ proxyCustomElement(class extends HTMLElement {
  constructor() {
    super();
    this.__registerHost();
    this.__attachShadow();
    this.calciteStepperItemChange = createEvent(this, "calciteStepperItemChange", 6);
    this.calciteInternalStepperItemChange = createEvent(this, "calciteInternalStepperItemChange", 6);
    //--------------------------------------------------------------------------
    //
    //  Public Properties
    //
    //--------------------------------------------------------------------------
    /** When `true`, displays a status icon in the `calcite-stepper-item` heading. */
    this.icon = false;
    /** Defines the layout of the component. */
    this.layout = "horizontal";
    /** When `true`, displays the step number in the `calcite-stepper-item` heading. */
    this.numbered = false;
    /** Specifies the size of the component. */
    this.scale = "m";
    //--------------------------------------------------------------------------
    //
    //  Private State/Props
    //
    //--------------------------------------------------------------------------
    this.itemMap = new Map();
    /** list of sorted Stepper items */
    this.items = [];
    /** list of enabled Stepper items */
    this.enabledItems = [];
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentDidLoad() {
    // if no stepper items are set as active, default to the first one
    if (typeof this.currentPosition !== "number") {
      this.calciteInternalStepperItemChange.emit({
        position: 0
      });
    }
  }
  render() {
    return (h("slot", { onSlotchange: (event) => {
        const items = event.currentTarget
          .assignedElements()
          .filter((el) => (el === null || el === void 0 ? void 0 : el.tagName) === "CALCITE-STEPPER-ITEM");
        const spacing = Array(items.length).fill("1fr").join(" ");
        this.el.style.gridTemplateAreas = spacing;
        this.el.style.gridTemplateColumns = spacing;
      } }));
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  calciteInternalStepperItemKeyEvent(event) {
    const item = event.detail.item;
    const itemToFocus = event.target;
    const isFirstItem = this.itemIndex(itemToFocus) === 0;
    const isLastItem = this.itemIndex(itemToFocus) === this.enabledItems.length - 1;
    switch (item.key) {
      case "ArrowDown":
      case "ArrowRight":
        if (isLastItem) {
          this.focusFirstItem();
        }
        else {
          this.focusNextItem(itemToFocus);
        }
        break;
      case "ArrowUp":
      case "ArrowLeft":
        if (isFirstItem) {
          this.focusLastItem();
        }
        else {
          this.focusPrevItem(itemToFocus);
        }
        break;
      case "Home":
        this.focusFirstItem();
        break;
      case "End":
        this.focusLastItem();
        break;
    }
    event.stopPropagation();
  }
  registerItem(event) {
    const item = event.target;
    const { content, position } = event.detail;
    this.itemMap.set(item, { position, content });
    this.items = this.sortItems();
    this.enabledItems = this.filterItems();
    event.stopPropagation();
  }
  updateItem(event) {
    const { position } = event.detail;
    if (typeof position === "number") {
      this.currentPosition = position;
    }
    this.calciteInternalStepperItemChange.emit({
      position
    });
  }
  handleUserRequestedStepperItemSelect(event) {
    const { position } = event.detail;
    this.calciteStepperItemChange.emit({
      position
    });
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /** Set the next `calcite-stepper-item` as active. */
  async nextStep() {
    const enabledStepIndex = this.getEnabledStepIndex(this.currentPosition + 1, "next");
    if (typeof enabledStepIndex !== "number") {
      return;
    }
    this.updateStep(enabledStepIndex);
  }
  /** Set the previous `calcite-stepper-item` as active. */
  async prevStep() {
    const enabledStepIndex = this.getEnabledStepIndex(this.currentPosition - 1, "previous");
    if (typeof enabledStepIndex !== "number") {
      return;
    }
    this.updateStep(enabledStepIndex);
  }
  /**
   * Set a specified `calcite-stepper-item` as active.
   *
   * @param step
   */
  async goToStep(step) {
    const position = step - 1;
    if (this.currentPosition !== position) {
      this.updateStep(position);
    }
  }
  /** Set the first `calcite-stepper-item` as active. */
  async startStep() {
    const enabledStepIndex = this.getEnabledStepIndex(0, "next");
    if (typeof enabledStepIndex !== "number") {
      return;
    }
    this.updateStep(enabledStepIndex);
  }
  /** Set the last `calcite-stepper-item` as active. */
  async endStep() {
    const enabledStepIndex = this.getEnabledStepIndex(this.items.length - 1, "previous");
    if (typeof enabledStepIndex !== "number") {
      return;
    }
    this.updateStep(enabledStepIndex);
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  getEnabledStepIndex(startIndex, direction = "next") {
    var _a;
    const { items, currentPosition } = this;
    let newIndex = startIndex;
    while ((_a = items[newIndex]) === null || _a === void 0 ? void 0 : _a.disabled) {
      newIndex = newIndex + (direction === "previous" ? -1 : 1);
    }
    return newIndex !== currentPosition && newIndex < items.length && newIndex >= 0
      ? newIndex
      : null;
  }
  updateStep(position) {
    this.currentPosition = position;
    this.calciteInternalStepperItemChange.emit({
      position
    });
  }
  focusFirstItem() {
    const firstItem = this.enabledItems[0];
    focusElement(firstItem);
  }
  focusLastItem() {
    const lastItem = this.enabledItems[this.enabledItems.length - 1];
    focusElement(lastItem);
  }
  focusNextItem(el) {
    const index = this.itemIndex(el);
    const nextItem = this.enabledItems[index + 1] || this.enabledItems[0];
    focusElement(nextItem);
  }
  focusPrevItem(el) {
    const index = this.itemIndex(el);
    const prevItem = this.enabledItems[index - 1] || this.enabledItems[this.enabledItems.length - 1];
    focusElement(prevItem);
  }
  itemIndex(el) {
    return this.enabledItems.indexOf(el);
  }
  sortItems() {
    const { itemMap } = this;
    return Array.from(itemMap.keys()).sort((a, b) => itemMap.get(a).position - itemMap.get(b).position);
  }
  filterItems() {
    return this.items.filter((item) => !item.disabled);
  }
  get el() { return this; }
  static get style() { return stepperCss; }
}, [1, "calcite-stepper", {
    "icon": [516],
    "layout": [513],
    "numbered": [516],
    "numberingSystem": [513, "numbering-system"],
    "scale": [513],
    "nextStep": [64],
    "prevStep": [64],
    "goToStep": [64],
    "startStep": [64],
    "endStep": [64]
  }, [[0, "calciteInternalStepperItemKeyEvent", "calciteInternalStepperItemKeyEvent"], [0, "calciteInternalStepperItemRegister", "registerItem"], [0, "calciteInternalStepperItemSelect", "updateItem"], [0, "calciteInternalUserRequestedStepperItemSelect", "handleUserRequestedStepperItemSelect"]]]);
function defineCustomElement$1() {
  if (typeof customElements === "undefined") {
    return;
  }
  const components = ["calcite-stepper"];
  components.forEach(tagName => { switch (tagName) {
    case "calcite-stepper":
      if (!customElements.get(tagName)) {
        customElements.define(tagName, Stepper);
      }
      break;
  } });
}
defineCustomElement$1();

const CalciteStepper = Stepper;
const defineCustomElement = defineCustomElement$1;

export { CalciteStepper, defineCustomElement };
