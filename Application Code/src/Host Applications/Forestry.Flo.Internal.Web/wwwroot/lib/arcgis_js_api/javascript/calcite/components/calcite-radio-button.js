/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { proxyCustomElement, HTMLElement, createEvent, h, Host } from '@stencil/core/internal/client/index.js';
import { g as guid } from './guid.js';
import { c as getElementDir, f as focusElement, t as toAriaBoolean } from './dom.js';
import { c as connectLabel, d as disconnectLabel, g as getLabelText } from './label2.js';
import { c as connectForm, d as disconnectForm, H as HiddenFormInputSlot } from './form.js';
import { g as getRoundRobinIndex } from './array.js';
import { u as updateHostInteraction } from './interactive.js';

const CSS = {
  container: "container"
};

const radioButtonCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{display:block;cursor:pointer}:host .container{position:relative;outline:2px solid transparent;outline-offset:2px}:host .radio{cursor:pointer;border-radius:9999px;background-color:var(--calcite-ui-foreground-1);outline-color:transparent;transition:all var(--calcite-animation-timing) ease-in-out 0s, outline 0s, outline-offset 0s;box-shadow:inset 0 0 0 1px var(--calcite-ui-border-input)}:host([hovered]) .radio,:host(:not([checked])[focused]:not([disabled])) .radio{box-shadow:inset 0 0 0 2px var(--calcite-ui-brand)}:host([focused]) .radio{outline:2px solid var(--calcite-ui-brand);outline-offset:2px}:host([disabled]) .radio{cursor:default;opacity:var(--calcite-ui-opacity-disabled)}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}:host([hovered][disabled]) .radio{box-shadow:inset 0 0 0 1px var(--calcite-ui-border-input)}:host([scale=s]){--calcite-radio-size:var(--calcite-font-size--2)}:host([scale=m]){--calcite-radio-size:var(--calcite-font-size--1)}:host([scale=l]){--calcite-radio-size:var(--calcite-font-size-0)}.radio{block-size:var(--calcite-radio-size);max-inline-size:var(--calcite-radio-size);min-inline-size:var(--calcite-radio-size)}:host([scale=s][checked]) .radio,:host([hovered][scale=s][checked][disabled]) .radio{box-shadow:inset 0 0 0 4px var(--calcite-ui-brand)}:host([scale=s][focused][checked]:not([disabled])) .radio{box-shadow:inset 0 0 0 4px var(--calcite-ui-brand), 0 0 0 2px var(--calcite-ui-foreground-1)}:host([scale=m][checked]) .radio,:host([hovered][scale=m][checked][disabled]) .radio{box-shadow:inset 0 0 0 5px var(--calcite-ui-brand)}:host([scale=m][focused][checked]:not([disabled])) .radio{box-shadow:inset 0 0 0 5px var(--calcite-ui-brand), 0 0 0 2px var(--calcite-ui-foreground-1)}:host([scale=l][checked]) .radio,:host([hovered][scale=l][checked][disabled]) .radio{box-shadow:inset 0 0 0 6px var(--calcite-ui-brand)}:host([scale=l][focused][checked]:not([disabled])) .radio{box-shadow:inset 0 0 0 6px var(--calcite-ui-brand), 0 0 0 2px var(--calcite-ui-foreground-1)}@media (forced-colors: active){:host([checked]) .radio::after,:host([checked][disabled]) .radio::after{content:\"\";inline-size:var(--calcite-radio-size);block-size:var(--calcite-radio-size);background-color:windowText;display:block}}::slotted(input[slot=hidden-form-input]){margin:0 !important;opacity:0 !important;outline:none !important;padding:0 !important;position:absolute !important;inset:0 !important;transform:none !important;-webkit-appearance:none !important;z-index:-1 !important}";

const RadioButton = /*@__PURE__*/ proxyCustomElement(class extends HTMLElement {
  constructor() {
    super();
    this.__registerHost();
    this.__attachShadow();
    this.calciteInternalRadioButtonBlur = createEvent(this, "calciteInternalRadioButtonBlur", 6);
    this.calciteRadioButtonChange = createEvent(this, "calciteRadioButtonChange", 6);
    this.calciteInternalRadioButtonCheckedChange = createEvent(this, "calciteInternalRadioButtonCheckedChange", 6);
    this.calciteInternalRadioButtonFocus = createEvent(this, "calciteInternalRadioButtonFocus", 6);
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /** When `true`, the component is checked. */
    this.checked = false;
    /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
    this.disabled = false;
    /**
     * The focused state of the component.
     *
     * @internal
     */
    this.focused = false;
    /** When `true`, the component is not displayed and is not focusable or checkable. */
    this.hidden = false;
    /**
     * The hovered state of the component.
     *
     * @internal
     */
    this.hovered = false;
    /** When `true`, the component must have a value selected from the `calcite-radio-button-group` in order for the form to submit. */
    this.required = false;
    /** Specifies the size of the component inherited from the `calcite-radio-button-group`. */
    this.scale = "m";
    //--------------------------------------------------------------------------
    //
    //  Private Methods
    //
    //--------------------------------------------------------------------------
    this.selectItem = (items, selectedIndex) => {
      items[selectedIndex].click();
    };
    this.queryButtons = () => {
      return Array.from(this.rootNode.querySelectorAll("calcite-radio-button:not([hidden])")).filter((radioButton) => radioButton.name === this.name);
    };
    this.isDefaultSelectable = () => {
      const radioButtons = this.queryButtons();
      return !radioButtons.some((radioButton) => radioButton.checked) && radioButtons[0] === this.el;
    };
    this.check = () => {
      if (this.disabled) {
        return;
      }
      this.uncheckAllRadioButtonsInGroup();
      this.checked = true;
      this.focused = true;
      this.calciteRadioButtonChange.emit();
      this.setFocus();
    };
    this.clickHandler = () => {
      this.check();
    };
    this.setContainerEl = (el) => {
      this.containerEl = el;
    };
    this.handleKeyDown = (event) => {
      const keys = ["ArrowLeft", "ArrowUp", "ArrowRight", "ArrowDown", " "];
      const { key } = event;
      const { el } = this;
      if (keys.indexOf(key) === -1) {
        return;
      }
      if (key === " ") {
        this.check();
        event.preventDefault();
        return;
      }
      let adjustedKey = key;
      if (getElementDir(el) === "rtl") {
        if (key === "ArrowRight") {
          adjustedKey = "ArrowLeft";
        }
        if (key === "ArrowLeft") {
          adjustedKey = "ArrowRight";
        }
      }
      const radioButtons = Array.from(this.rootNode.querySelectorAll("calcite-radio-button:not([hidden]")).filter((radioButton) => radioButton.name === this.name);
      let currentIndex = 0;
      const radioButtonsLength = radioButtons.length;
      radioButtons.some((item, index) => {
        if (item.checked) {
          currentIndex = index;
          return true;
        }
      });
      switch (adjustedKey) {
        case "ArrowLeft":
        case "ArrowUp":
          event.preventDefault();
          this.selectItem(radioButtons, getRoundRobinIndex(Math.max(currentIndex - 1, -1), radioButtonsLength));
          return;
        case "ArrowRight":
        case "ArrowDown":
          event.preventDefault();
          this.selectItem(radioButtons, getRoundRobinIndex(currentIndex + 1, radioButtonsLength));
          return;
        default:
          return;
      }
    };
    this.onContainerBlur = () => {
      this.focused = false;
      this.calciteInternalRadioButtonBlur.emit();
    };
    this.onContainerFocus = () => {
      if (!this.disabled) {
        this.focused = true;
        this.calciteInternalRadioButtonFocus.emit();
      }
    };
  }
  checkedChanged(newChecked) {
    if (newChecked) {
      this.uncheckOtherRadioButtonsInGroup();
    }
    this.calciteInternalRadioButtonCheckedChange.emit();
  }
  nameChanged() {
    this.checkLastRadioButton();
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    if (!this.disabled) {
      focusElement(this.containerEl);
    }
  }
  onLabelClick(event) {
    if (!this.disabled && !this.hidden) {
      this.uncheckOtherRadioButtonsInGroup();
      const label = event.currentTarget;
      const radioButton = label.for
        ? this.rootNode.querySelector(`calcite-radio-button[id="${label.for}"]`)
        : label.querySelector(`calcite-radio-button[name="${this.name}"]`);
      if (radioButton) {
        radioButton.checked = true;
        radioButton.focused = true;
      }
      this.calciteRadioButtonChange.emit();
      this.setFocus();
    }
  }
  checkLastRadioButton() {
    const radioButtons = this.queryButtons();
    const checkedRadioButtons = radioButtons.filter((radioButton) => radioButton.checked);
    if ((checkedRadioButtons === null || checkedRadioButtons === void 0 ? void 0 : checkedRadioButtons.length) > 1) {
      const lastCheckedRadioButton = checkedRadioButtons[checkedRadioButtons.length - 1];
      checkedRadioButtons
        .filter((checkedRadioButton) => checkedRadioButton !== lastCheckedRadioButton)
        .forEach((checkedRadioButton) => {
        checkedRadioButton.checked = false;
        checkedRadioButton.emitCheckedChange();
      });
    }
  }
  /** @internal */
  async emitCheckedChange() {
    this.calciteInternalRadioButtonCheckedChange.emit();
  }
  uncheckAllRadioButtonsInGroup() {
    const radioButtons = this.queryButtons();
    radioButtons.forEach((radioButton) => {
      if (radioButton.checked) {
        radioButton.checked = false;
        radioButton.focused = false;
      }
    });
  }
  uncheckOtherRadioButtonsInGroup() {
    const radioButtons = this.queryButtons();
    const otherRadioButtons = radioButtons.filter((radioButton) => radioButton.guid !== this.guid);
    otherRadioButtons.forEach((otherRadioButton) => {
      if (otherRadioButton.checked) {
        otherRadioButton.checked = false;
        otherRadioButton.focused = false;
      }
    });
  }
  getTabIndex() {
    if (this.disabled) {
      return undefined;
    }
    return this.checked || this.isDefaultSelectable() ? 0 : -1;
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  mouseenter() {
    this.hovered = true;
  }
  mouseleave() {
    this.hovered = false;
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    this.rootNode = this.el.getRootNode();
    this.guid = this.el.id || `calcite-radio-button-${guid()}`;
    if (this.name) {
      this.checkLastRadioButton();
    }
    connectLabel(this);
    connectForm(this);
  }
  componentDidLoad() {
    if (this.focused && !this.disabled) {
      this.setFocus();
    }
  }
  disconnectedCallback() {
    disconnectLabel(this);
    disconnectForm(this);
  }
  componentDidRender() {
    updateHostInteraction(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    const tabIndex = this.getTabIndex();
    return (h(Host, { onClick: this.clickHandler, onKeyDown: this.handleKeyDown }, h("div", { "aria-checked": toAriaBoolean(this.checked), "aria-label": getLabelText(this), class: CSS.container, onBlur: this.onContainerBlur, onFocus: this.onContainerFocus, ref: this.setContainerEl, role: "radio", tabIndex: tabIndex }, h("div", { class: "radio" })), h(HiddenFormInputSlot, { component: this })));
  }
  get el() { return this; }
  static get watchers() { return {
    "checked": ["checkedChanged"],
    "name": ["nameChanged"]
  }; }
  static get style() { return radioButtonCss; }
}, [1, "calcite-radio-button", {
    "checked": [1540],
    "disabled": [516],
    "focused": [1540],
    "guid": [1537],
    "hidden": [516],
    "hovered": [1540],
    "label": [1],
    "name": [513],
    "required": [516],
    "scale": [513],
    "value": [1032],
    "setFocus": [64],
    "emitCheckedChange": [64]
  }, [[1, "pointerenter", "mouseenter"], [1, "pointerleave", "mouseleave"]]]);
function defineCustomElement$1() {
  if (typeof customElements === "undefined") {
    return;
  }
  const components = ["calcite-radio-button"];
  components.forEach(tagName => { switch (tagName) {
    case "calcite-radio-button":
      if (!customElements.get(tagName)) {
        customElements.define(tagName, RadioButton);
      }
      break;
  } });
}
defineCustomElement$1();

const CalciteRadioButton = RadioButton;
const defineCustomElement = defineCustomElement$1;

export { CalciteRadioButton, defineCustomElement };
