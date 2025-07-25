/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
'use strict';

Object.defineProperty(exports, '__esModule', { value: true });

const index = require('./index-b27b231c.js');
const dom = require('./dom-2b919cb6.js');
const conditionalSlot = require('./conditionalSlot-f62c357a.js');
const interactive = require('./interactive-3d681fb9.js');
require('./resources-3fd6da1b.js');
require('./guid-acbbb0e7.js');
require('./observers-664fbf90.js');

const SLOTS = {
  contentStart: "content-start",
  contentEnd: "content-end"
};

const tileCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{box-sizing:border-box;display:inline-block;-webkit-user-select:none;user-select:none;background-color:var(--calcite-ui-foreground-1);color:var(--calcite-ui-text-3);transition-duration:150ms;transition-timing-function:cubic-bezier(0.4, 0, 0.2, 1)}:host .container{pointer-events:none;display:grid;grid-template-columns:repeat(1, minmax(0, 1fr));gap:0.5rem}:host .content{display:flex;flex:1 1 auto;flex-direction:column;justify-content:center;gap:0.5rem;inline-size:10%}:host .content-container{display:flex;inline-size:100%;flex:1 1 auto;align-items:stretch;padding:0px;color:var(--calcite-ui-text-2);outline-color:transparent}:host .content-slot-container{display:flex;align-items:center;background-color:var(--calcite-ui-foreground-1)}:host .content-slot-container:first-child{padding-inline:0 0.75rem}:host .content-slot-container:last-child{padding-inline:0.75rem 0}:host .heading{pointer-events:none;overflow-wrap:break-word;font-size:var(--calcite-font-size--1);line-height:1.375;font-weight:var(--calcite-font-weight-medium);color:var(--calcite-ui-text-2);transition-duration:150ms;transition-timing-function:cubic-bezier(0.4, 0, 0.2, 1)}:host .large-visual{align-items:center;text-align:center;min-block-size:12rem}:host .large-visual .icon{display:flex;justify-content:center;align-self:flex-end}:host .large-visual .content-container{align-self:center}:host .description{pointer-events:none;overflow-wrap:break-word;font-size:var(--calcite-font-size--2);line-height:1.375;color:var(--calcite-ui-text-3);transition-duration:150ms;transition-timing-function:cubic-bezier(0.4, 0, 0.2, 1)}:host(:not([embed])){padding:0.75rem;box-shadow:0 0 0 1px var(--calcite-ui-border-2)}:host(:not([embed])[href]:hover){cursor:pointer;box-shadow:0 0 0 2px var(--calcite-ui-brand)}:host(:not([embed])[href]:active){box-shadow:0 0 0 3px var(--calcite-ui-brand)}:host([icon][heading]:not([description]):not([embed])){padding:0px}:host([icon][heading]:not([description])) .icon{display:flex;justify-content:center}:host([icon][heading]:not([description])) .large-visual{text-align:center}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}:host(:hover) .heading,:host([active]) .heading{color:var(--calcite-ui-text-1)}:host(:hover) .description,:host([active]) .description{color:var(--calcite-ui-text-2)}";

const Tile = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /**
     * When `true`, the component is active.
     */
    this.active = false;
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    /**
     * The component's embed mode.
     *
     * When `true`, renders without a border and padding for use by other components.
     */
    this.embed = false;
    /**
     * The focused state of the component.
     *
     * @internal
     */
    this.focused = false;
    /** When `true`, the component is not displayed and is not focusable.  */
    this.hidden = false;
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    conditionalSlot.connectConditionalSlotComponent(this);
  }
  disconnectedCallback() {
    conditionalSlot.disconnectConditionalSlotComponent(this);
  }
  componentDidRender() {
    interactive.updateHostInteraction(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderTile() {
    const { icon, el, heading, description } = this;
    const isLargeVisual = heading && icon && !description;
    const iconStyle = isLargeVisual
      ? {
        height: "64px",
        width: "64px"
      }
      : undefined;
    return (index.h("div", { class: { container: true, "large-visual": isLargeVisual } }, icon && (index.h("div", { class: "icon" }, index.h("calcite-icon", { icon: icon, scale: "l", style: iconStyle }))), index.h("div", { class: "content-container" }, dom.getSlotted(el, SLOTS.contentStart) ? (index.h("div", { class: "content-slot-container" }, index.h("slot", { name: SLOTS.contentStart }))) : null, index.h("div", { class: "content" }, heading && index.h("div", { class: "heading" }, heading), description && index.h("div", { class: "description" }, description)), dom.getSlotted(el, SLOTS.contentEnd) ? (index.h("div", { class: "content-slot-container" }, index.h("slot", { name: SLOTS.contentEnd }))) : null)));
  }
  render() {
    return (index.h(index.Fragment, null, this.href ? (index.h("calcite-link", { disabled: this.disabled, href: this.href }, this.renderTile())) : (this.renderTile())));
  }
  get el() { return index.getElement(this); }
};
Tile.style = tileCss;

exports.calcite_tile = Tile;
