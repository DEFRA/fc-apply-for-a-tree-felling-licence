/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { proxyCustomElement, HTMLElement, h, Host } from '@stencil/core/internal/client/index.js';
import { H as Heading, c as constrainHeadingLevel } from './Heading.js';

const CSS = {
  heading: "heading",
  container: "container"
};
const HEADING_LEVEL = 3;

const listItemGroupCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host{display:flex;flex-direction:column}.heading{margin:0px;display:flex;flex:1 1 0%;background-color:var(--calcite-ui-foreground-2);padding:0.75rem;font-family:var(--calcite-sans-family);font-size:var(--calcite-font-size--1);font-weight:var(--calcite-font-weight-bold);color:var(--calcite-ui-text-2)}.container{display:flex;inline-size:100%;flex-direction:column;background-color:var(--calcite-ui-foreground-1)}::slotted(calcite-list-item-group){padding-inline-start:0.5rem}";

const ListItemGroup = /*@__PURE__*/ proxyCustomElement(class extends HTMLElement {
  constructor() {
    super();
    this.__registerHost();
    this.__attachShadow();
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    var _a;
    const { el, heading, headingLevel } = this;
    const parentLevel = (_a = el.closest("calcite-list, calcite-list-item-group")) === null || _a === void 0 ? void 0 : _a.headingLevel;
    const relativeLevel = parentLevel ? constrainHeadingLevel(parentLevel + 1) : null;
    const level = headingLevel || relativeLevel || HEADING_LEVEL;
    return (h(Host, null, heading ? (h(Heading, { class: CSS.heading, level: level }, heading)) : null, h("div", { class: CSS.container, role: "group" }, h("slot", null))));
  }
  get el() { return this; }
  static get style() { return listItemGroupCss; }
}, [1, "calcite-list-item-group", {
    "heading": [513],
    "headingLevel": [514, "heading-level"]
  }]);
function defineCustomElement$1() {
  if (typeof customElements === "undefined") {
    return;
  }
  const components = ["calcite-list-item-group"];
  components.forEach(tagName => { switch (tagName) {
    case "calcite-list-item-group":
      if (!customElements.get(tagName)) {
        customElements.define(tagName, ListItemGroup);
      }
      break;
  } });
}
defineCustomElement$1();

const CalciteListItemGroup = ListItemGroup;
const defineCustomElement = defineCustomElement$1;

export { CalciteListItemGroup, defineCustomElement };
