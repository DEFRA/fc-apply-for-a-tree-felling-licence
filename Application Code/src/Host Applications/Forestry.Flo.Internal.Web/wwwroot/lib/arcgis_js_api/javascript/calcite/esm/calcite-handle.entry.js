/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { r as registerInstance, c as createEvent, h, g as getElement } from './index-7b536c47.js';
import { t as toAriaBoolean } from './dom-bbdd8cab.js';
import './resources-c8c07116.js';
import './guid-3c14f5cd.js';

const CSS = {
  handle: "handle",
  handleActivated: "handle--activated"
};
const ICONS = {
  drag: "drag"
};

const handleCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host{display:flex}.handle{display:flex;cursor:move;align-items:center;justify-content:center;align-self:stretch;border-style:none;background-color:transparent;outline-color:transparent;color:var(--calcite-ui-border-input);padding-block:0.75rem;padding-inline:0.25rem;line-height:0}.handle:hover{background-color:var(--calcite-ui-foreground-2);color:var(--calcite-ui-text-1)}.handle:focus{color:var(--calcite-ui-text-1);outline:2px solid var(--calcite-ui-brand);outline-offset:-2px}.handle--activated{background-color:var(--calcite-ui-foreground-3);color:var(--calcite-ui-text-1)}.handle calcite-icon{color:inherit}";

const Handle = class {
  constructor(hostRef) {
    registerInstance(this, hostRef);
    this.calciteHandleNudge = createEvent(this, "calciteHandleNudge", 6);
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * @internal
     */
    this.activated = false;
    /**
     * Value for the button title attribute
     */
    this.textTitle = "handle";
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.handleKeyDown = (event) => {
      switch (event.key) {
        case " ":
          this.activated = !this.activated;
          event.preventDefault();
          break;
        case "ArrowUp":
        case "ArrowDown":
          if (!this.activated) {
            return;
          }
          event.preventDefault();
          const direction = event.key.toLowerCase().replace("arrow", "");
          this.calciteHandleNudge.emit({ handle: this.el, direction });
          break;
      }
    };
    this.handleBlur = () => {
      this.activated = false;
    };
  }
  // --------------------------------------------------------------------------
  //
  //  Methods
  //
  // --------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.handleButton) === null || _a === void 0 ? void 0 : _a.focus();
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    return (
    // Needs to be a span because of https://github.com/SortableJS/Sortable/issues/1486
    h("span", { "aria-pressed": toAriaBoolean(this.activated), class: { [CSS.handle]: true, [CSS.handleActivated]: this.activated }, onBlur: this.handleBlur, onKeyDown: this.handleKeyDown, ref: (el) => {
        this.handleButton = el;
      }, role: "button", tabindex: "0", title: this.textTitle }, h("calcite-icon", { icon: ICONS.drag, scale: "s" })));
  }
  get el() { return getElement(this); }
};
Handle.style = handleCss;

export { Handle as calcite_handle };
