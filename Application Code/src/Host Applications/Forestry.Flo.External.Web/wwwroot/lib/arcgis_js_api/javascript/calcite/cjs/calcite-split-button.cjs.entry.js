/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
'use strict';

Object.defineProperty(exports, '__esModule', { value: true });

const index = require('./index-b27b231c.js');
const interactive = require('./interactive-3d681fb9.js');

const CSS = {
  container: "split-button__container",
  dividerContainer: "split-button__divider-container",
  divider: "split-button__divider",
  widthAuto: "width-auto",
  widthHalf: "width-half",
  widthFull: "width-full"
};

const splitButtonCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{display:inline-block}:host .split-button__container{display:flex;align-items:stretch}:host .split-button__container>calcite-dropdown>calcite-button{block-size:100%;vertical-align:top}:host:host([color=blue]){--calcite-split-button-background:var(--calcite-ui-brand);--calcite-split-button-divider:var(--calcite-ui-foreground-1)}:host:host([color=red]){--calcite-split-button-background:var(--calcite-ui-danger);--calcite-split-button-divider:var(--calcite-ui-foreground-1)}:host:host([color=neutral]){--calcite-split-button-background:var(--calcite-ui-foreground-3);--calcite-split-button-divider:var(--calcite-ui-text-1)}:host:host([color=inverse]){--calcite-split-button-background:var(--calcite-ui-inverse);--calcite-split-button-divider:var(--calcite-ui-foreground-1)}:host([appearance=transparent]):host([color=blue]){--calcite-split-button-divider:var(--calcite-ui-brand)}:host([appearance=transparent]):host([color=red]){--calcite-split-button-divider:var(--calcite-ui-danger)}:host([appearance=transparent]):host([color=neutral]){--calcite-split-button-divider:var(--calcite-ui-text-1)}:host([appearance=transparent]):host([color=inverse]){--calcite-split-button-divider:var(--calcite-ui-foreground-1)}:host([appearance=clear]),:host([appearance=transparent]){--calcite-split-button-background:transparent}:host([appearance=outline]){--calcite-split-button-background:var(--calcite-ui-foreground-1)}:host([appearance=clear]):host([color=blue]),:host([appearance=outline]):host([color=blue]){--calcite-split-button-divider:var(--calcite-ui-brand)}:host([appearance=clear]):host([color=red]),:host([appearance=outline]):host([color=red]){--calcite-split-button-divider:var(--calcite-ui-danger)}:host([appearance=clear]):host([color=neutral]),:host([appearance=outline]):host([color=neutral]){--calcite-split-button-divider:var(--calcite-ui-foreground-3)}:host([appearance=clear]):host([color=inverse]),:host([appearance=outline]):host([color=inverse]){--calcite-split-button-divider:var(--calcite-ui-inverse)}.width-auto{inline-size:auto}.width-half{inline-size:50%}.width-full{inline-size:100%}.split-button__divider-container{display:flex;inline-size:1px;align-items:stretch;transition:all var(--calcite-animation-timing) ease-in-out 0s, outline 0s, outline-offset 0s;background-color:var(--calcite-split-button-background)}.split-button__divider{margin-block:0.25rem;display:inline-block;inline-size:1px;background-color:var(--calcite-split-button-divider)}:host([appearance=outline]) .split-button__divider-container,:host([appearance=clear]) .split-button__divider-container,:host([appearance=minimal]) .split-button__divider-container{border-block:1px solid var(--calcite-split-button-divider)}:host([appearance=outline]):hover .split-button__divider-container,:host([appearance=clear]):hover .split-button__divider-container,:host([appearance=minimal]):hover .split-button__divider-container{background-color:var(--calcite-split-button-divider)}:host([appearance=outline]:hover) .split-button__divider-container,:host([appearance=clear]:hover) .split-button__divider-container,:host([appearance=minimal]):hover .split-button__divider-container{background-color:var(--calcite-split-button-divider)}:host([appearance=outline]:focus-within):host([color=blue]),:host([appearance=clear]:focus-within):host([color=blue]),:host([appearance=minimal]:focus-within):host([color=blue]){--calcite-split-button-divider:var(--calcite-ui-brand-press)}:host([appearance=outline]:focus-within):host([color=red]),:host([appearance=clear]:focus-within):host([color=red]),:host([appearance=minimal]:focus-within):host([color=red]){--calcite-split-button-divider:var(--calcite-ui-danger-press)}:host([appearance=outline]:focus-within) .split-button__divider-container,:host([appearance=clear]:focus-within) .split-button__divider-container,:host([appearance=minimal]:focus-within) .split-button__divider-container{background-color:var(--calcite-split-button-divider)}:host([disabled]) .split-button__divider-container{opacity:var(--calcite-ui-opacity-disabled)}:host([disabled]) calcite-dropdown>calcite-button{pointer-events:none}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}";

const SplitButton = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteSplitButtonPrimaryClick = index.createEvent(this, "calciteSplitButtonPrimaryClick", 6);
    this.calciteSplitButtonSecondaryClick = index.createEvent(this, "calciteSplitButtonSecondaryClick", 6);
    /** Specifies the appearance style of the component. */
    this.appearance = "solid";
    /** Specifies the color of the component. */
    this.color = "blue";
    /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
    this.disabled = false;
    /**
     * When `true`, the component is active.
     *
     * @internal
     */
    this.active = false;
    /** Specifies the icon used for the dropdown menu. */
    this.dropdownIconType = "chevron";
    /**
      When `true`, a busy indicator is displayed on the primary button.
     */
    this.loading = false;
    /**
     * Determines the type of positioning to use for the overlaid content.
     *
     * Using `"absolute"` will work for most cases. The component will be positioned inside of overflowing parent containers and will affect the container's layout.
     *
     * `"fixed"` should be used to escape an overflowing parent container, or when the reference element's `position` CSS property is `"fixed"`.
     *
     */
    this.overlayPositioning = "absolute";
    /** Specifies the size of the component. */
    this.scale = "m";
    /** Specifies the width of the component. */
    this.width = "auto";
    this.calciteSplitButtonPrimaryClickHandler = (event) => this.calciteSplitButtonPrimaryClick.emit(event);
    this.calciteSplitButtonSecondaryClickHandler = (event) => this.calciteSplitButtonSecondaryClick.emit(event);
  }
  handleDisabledChange(value) {
    if (!value) {
      this.active = false;
    }
  }
  activeHandler() {
    if (this.disabled) {
      this.active = false;
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentDidRender() {
    interactive.updateHostInteraction(this);
  }
  render() {
    const widthClasses = {
      [CSS.container]: true,
      [CSS.widthAuto]: this.width === "auto",
      [CSS.widthHalf]: this.width === "half",
      [CSS.widthFull]: this.width === "full"
    };
    const buttonWidth = this.width === "auto" ? "auto" : "full";
    return (index.h("div", { class: widthClasses }, index.h("calcite-button", { appearance: this.appearance, color: this.color, disabled: this.disabled, "icon-end": this.primaryIconEnd ? this.primaryIconEnd : null, "icon-start": this.primaryIconStart ? this.primaryIconStart : null, iconFlipRtl: this.primaryIconFlipRtl ? this.primaryIconFlipRtl : null, label: this.primaryLabel, loading: this.loading, onClick: this.calciteSplitButtonPrimaryClickHandler, scale: this.scale, splitChild: "primary", type: "button", width: buttonWidth }, this.primaryText), index.h("div", { class: CSS.dividerContainer }, index.h("div", { class: CSS.divider })), index.h("calcite-dropdown", { active: this.active, disabled: this.disabled, onClick: this.calciteSplitButtonSecondaryClickHandler, overlayPositioning: this.overlayPositioning, placement: "bottom-end", scale: this.scale, width: this.scale }, index.h("calcite-button", { appearance: this.appearance, color: this.color, disabled: this.disabled, "icon-start": this.dropdownIcon, label: this.dropdownLabel, scale: this.scale, slot: "dropdown-trigger", splitChild: "secondary", type: "button" }), index.h("slot", null))));
  }
  get dropdownIcon() {
    return this.dropdownIconType === "chevron"
      ? "chevronDown"
      : this.dropdownIconType === "caret"
        ? "caretDown"
        : this.dropdownIconType === "ellipsis"
          ? "ellipsis"
          : "handle-vertical";
  }
  get el() { return index.getElement(this); }
  static get watchers() { return {
    "disabled": ["handleDisabledChange"],
    "active": ["activeHandler"]
  }; }
};
SplitButton.style = splitButtonCss;

exports.calcite_split_button = SplitButton;
