/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
'use strict';

Object.defineProperty(exports, '__esModule', { value: true });

const index = require('./index-b27b231c.js');
const label = require('./label-5d4931f7.js');
require('./dom-2b919cb6.js');
require('./resources-3fd6da1b.js');
require('./guid-acbbb0e7.js');

const CSS = {
  container: "container"
};

const labelCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{display:inline}:host([alignment=start]){text-align:start}:host([alignment=end]){text-align:end}:host([alignment=center]){text-align:center}:host([scale=s]) .container{gap:0.25rem;font-size:var(--calcite-font-size--2);line-height:1rem}:host(:not([disable-spacing])[scale=s]) .container{margin-block-end:var(--calcite-label-margin-bottom, 0.5rem)}:host([scale=m]) .container{gap:0.5rem;font-size:var(--calcite-font-size--1);line-height:1rem}:host(:not([disable-spacing])[scale=m]) .container{margin-block-end:var(--calcite-label-margin-bottom, 0.75rem)}:host([scale=l]) .container{gap:0.5rem;font-size:var(--calcite-font-size-0);line-height:1.25rem}:host(:not([disable-spacing])[scale=l]) .container{margin-block-end:var(--calcite-label-margin-bottom, 1rem)}:host .container{margin-inline:0px;margin-block-start:0px;inline-size:100%;line-height:1.375;color:var(--calcite-ui-text-1)}:host([layout=default]) .container{display:flex;flex-direction:column}:host([layout=inline]) .container,:host([layout=inline-space-between]) .container{display:flex;flex-direction:row;align-items:center;gap:0.5rem}:host([layout=inline][scale=l]) .container{gap:0.75rem}:host([layout=inline-space-between]) .container{justify-content:space-between}:host([disabled])>.container{pointer-events:none;opacity:var(--calcite-ui-opacity-disabled)}:host([disabled]) ::slotted(*){pointer-events:none}:host([disabled]) ::slotted(*[disabled]),:host([disabled]) ::slotted(*[disabled] *){--tw-bg-opacity:1}:host([disabled]) ::slotted(calcite-input-message:not([active])){--tw-bg-opacity:0}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}:host([disable-spacing]) .container{margin:0px;gap:0px}";

const Label = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteInternalLabelClick = index.createEvent(this, "calciteInternalLabelClick", 2);
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /** Specifies the text alignment of the component. */
    this.alignment = "start";
    /**
     * Specifies the status of the component and any child input, or input messages.
     *
     * @deprecated Set directly on the component the label is bound to instead.
     */
    this.status = "idle";
    /** Specifies the size of the component. */
    this.scale = "m";
    /** Defines the layout of the label in relation to the component. Use `"inline"` positions to wrap the label and component on the same line. */
    this.layout = "default";
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     *
     * @deprecated Use the `disabled` property on the component the label is bound to instead.
     */
    this.disabled = false;
    /**
     * When `true`, disables the component's spacing.
     *
     * @deprecated Set the `--calcite-label-margin-bottom` css variable to `0` instead.
     */
    this.disableSpacing = false;
    //--------------------------------------------------------------------------
    //
    //  Private Methods
    //
    //--------------------------------------------------------------------------
    this.labelClickHandler = (event) => {
      this.calciteInternalLabelClick.emit({
        sourceEvent: event
      });
    };
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    document.dispatchEvent(new CustomEvent(label.labelConnectedEvent));
  }
  disconnectedCallback() {
    document.dispatchEvent(new CustomEvent(label.labelDisconnectedEvent));
  }
  render() {
    return (index.h(index.Host, { onClick: this.labelClickHandler }, index.h("div", { class: CSS.container }, index.h("slot", null))));
  }
  get el() { return index.getElement(this); }
};
Label.style = labelCss;

exports.calcite_label = Label;
