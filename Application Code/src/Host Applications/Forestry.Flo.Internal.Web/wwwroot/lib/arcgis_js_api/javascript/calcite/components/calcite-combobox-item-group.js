/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { proxyCustomElement, HTMLElement, h } from '@stencil/core/internal/client/index.js';
import { d as getAncestors, e as getDepth } from './utils2.js';
import { g as guid } from './guid.js';
import { g as getElementProp } from './dom.js';

const CSS = {
  list: "list",
  label: "label",
  title: "title"
};

const comboboxItemGroupCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}.scale--s{font-size:var(--calcite-font-size--2);line-height:1rem;--calcite-combobox-item-spacing-unit-l:0.5rem;--calcite-combobox-item-spacing-unit-s:0.25rem;--calcite-combobox-item-spacing-indent-1:0.5rem;--calcite-combobox-item-spacing-indent-2:1rem}.scale--m{font-size:var(--calcite-font-size--1);line-height:1rem;--calcite-combobox-item-spacing-unit-l:0.75rem;--calcite-combobox-item-spacing-unit-s:0.5rem;--calcite-combobox-item-spacing-indent-1:0.75rem;--calcite-combobox-item-spacing-indent-2:1.5rem}.scale--l{font-size:var(--calcite-font-size-0);line-height:1.25rem;--calcite-combobox-item-spacing-unit-l:1rem;--calcite-combobox-item-spacing-unit-s:0.75rem;--calcite-combobox-item-spacing-indent-1:1rem;--calcite-combobox-item-spacing-indent-2:2rem}:host,.list{margin:0px;display:flex;flex-direction:column;padding:0px}:host(:focus),.list:focus{outline:2px solid transparent;outline-offset:2px}.label{box-sizing:border-box;display:flex;inline-size:100%;min-inline-size:0px;max-inline-size:100%;color:var(--calcite-ui-text-3)}.label--indent-1{padding-inline-start:var(--calcite-combobox-item-spacing-indent-1)}.label--indent-2{padding-inline-start:var(--calcite-combobox-item-spacing-indent-2)}.title{border:0 solid;display:block;flex:1 1 0%;border-block-end-width:1px;font-weight:var(--calcite-font-weight-bold);color:var(--calcite-ui-text-2);word-wrap:break-word;word-break:break-word;border-block-end-color:var(--calcite-ui-border-3);padding-block:var(--calcite-combobox-item-spacing-unit-l);padding-inline:0;margin-inline:var(--calcite-combobox-item-spacing-unit-s)}";

const ComboboxItemGroup = /*@__PURE__*/ proxyCustomElement(class extends HTMLElement {
  constructor() {
    super();
    this.__registerHost();
    this.__attachShadow();
    this.guid = guid();
    this.scale = "m";
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    this.ancestors = getAncestors(this.el);
    this.scale = getElementProp(this.el, "scale", this.scale);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    const { el, scale } = this;
    const indent = `${CSS.label}--indent-${getDepth(el)}`;
    return (h("ul", { "aria-labelledby": this.guid, class: { [CSS.list]: true, [`scale--${scale}`]: true }, role: "group" }, h("li", { class: { [CSS.label]: true, [indent]: true }, id: this.guid, role: "presentation" }, h("span", { class: CSS.title }, this.label)), h("slot", null)));
  }
  get el() { return this; }
  static get style() { return comboboxItemGroupCss; }
}, [1, "calcite-combobox-item-group", {
    "ancestors": [1040],
    "label": [1]
  }]);
function defineCustomElement$1() {
  if (typeof customElements === "undefined") {
    return;
  }
  const components = ["calcite-combobox-item-group"];
  components.forEach(tagName => { switch (tagName) {
    case "calcite-combobox-item-group":
      if (!customElements.get(tagName)) {
        customElements.define(tagName, ComboboxItemGroup);
      }
      break;
  } });
}
defineCustomElement$1();

const CalciteComboboxItemGroup = ComboboxItemGroup;
const defineCustomElement = defineCustomElement$1;

export { CalciteComboboxItemGroup, defineCustomElement };
