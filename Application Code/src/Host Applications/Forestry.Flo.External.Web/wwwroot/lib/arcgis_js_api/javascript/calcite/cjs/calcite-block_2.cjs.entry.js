/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
'use strict';

Object.defineProperty(exports, '__esModule', { value: true });

const index = require('./index-b27b231c.js');
const dom = require('./dom-2b919cb6.js');
const Heading = require('./Heading-60920dbe.js');
const conditionalSlot = require('./conditionalSlot-f62c357a.js');
const interactive = require('./interactive-3d681fb9.js');
const guid = require('./guid-acbbb0e7.js');
const key = require('./key-6a28a7af.js');
require('./resources-3fd6da1b.js');
require('./observers-664fbf90.js');

const CSS$1 = {
  container: "container",
  content: "content",
  contentSpaced: "content--spaced",
  headerContainer: "header-container",
  icon: "icon",
  statusIcon: "status-icon",
  toggle: "toggle",
  toggleIcon: "toggle-icon",
  title: "title",
  heading: "heading",
  header: "header",
  button: "button",
  summary: "summary",
  description: "description",
  controlContainer: "control-container",
  valid: "valid",
  invalid: "invalid",
  loading: "loading"
};
const TEXT$1 = {
  collapse: "Collapse",
  expand: "Expand",
  loading: "Loading",
  options: "Options"
};
const SLOTS = {
  icon: "icon",
  control: "control",
  headerMenuActions: "header-menu-actions"
};
const ICONS$1 = {
  opened: "chevron-up",
  closed: "chevron-down",
  valid: "check-circle",
  invalid: "exclamation-mark-triangle",
  refresh: "refresh"
};
const HEADING_LEVEL = 4;

const blockCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:host{box-sizing:border-box;background-color:var(--calcite-ui-foreground-1);color:var(--calcite-ui-text-2);font-size:var(--calcite-font-size--1)}:host *{box-sizing:border-box}:host{--calcite-icon-size:1rem;--calcite-spacing-eighth:0.125rem;--calcite-spacing-quarter:0.25rem;--calcite-spacing-half:0.5rem;--calcite-spacing-three-quarters:0.75rem;--calcite-spacing:1rem;--calcite-spacing-plus-quarter:1.25rem;--calcite-spacing-plus-half:1.5rem;--calcite-spacing-double:2rem;--calcite-menu-min-width:10rem;--calcite-header-min-height:3rem;--calcite-footer-min-height:3rem}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{display:flex;flex-shrink:0;flex-grow:0;flex-direction:column;border-width:0px;border-block-end-width:1px;border-style:solid;border-color:var(--calcite-ui-border-3);padding:0px;transition-property:margin;transition-timing-function:cubic-bezier(0.4, 0, 0.2, 1);transition-duration:150ms;transition-timing-function:cubic-bezier(0.215, 0.440, 0.420, 0.880);flex-basis:auto}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}.header{margin:0px;display:flex;align-content:space-between;align-items:center;fill:var(--calcite-ui-text-2);color:var(--calcite-ui-text-2)}.heading{margin:0px;padding:0px;font-weight:var(--calcite-font-weight-medium)}.header .heading{flex:1 1 auto;padding:0.5rem}.header{justify-content:flex-start;padding:0px}.header,.toggle{grid-area:header}.header-container{display:grid;align-items:stretch;grid-template:auto/auto 1fr auto auto;grid-template-areas:\"handle header control menu\";grid-column:header-start/menu-end;grid-row:1/2}.toggle{margin:0px;display:flex;cursor:pointer;flex-wrap:nowrap;align-items:center;justify-content:space-between;border-style:none;padding:0px;font-family:inherit;outline-color:transparent;text-align:initial;background-color:transparent}.toggle:hover{background-color:var(--calcite-ui-foreground-2)}.toggle:focus{outline:2px solid var(--calcite-ui-brand);outline-offset:-2px}calcite-loader[inline]{grid-area:control;align-self:center}calcite-handle{grid-area:handle}.title{margin:0px;padding:0.75rem}.header .title .heading{padding:0px;font-size:var(--calcite-font-size--1);font-weight:var(--calcite-font-weight-medium);line-height:1.25;color:var(--calcite-ui-text-2);transition-property:color;transition-duration:150ms;transition-timing-function:cubic-bezier(0.4, 0, 0.2, 1);word-wrap:break-word;word-break:break-word}.description{margin-block-start:0.125rem;padding:0px;font-size:var(--calcite-font-size--2);color:var(--calcite-ui-text-3);word-wrap:break-word;word-break:break-word}.icon{margin-inline-start:0.75rem;margin-inline-end:0px;margin-block:0.75rem}.status-icon.valid{color:var(--calcite-ui-success)}.status-icon.invalid{color:var(--calcite-ui-danger)}.status-icon.loading{animation:spin var(--calcite-internal-animation-timing-medium) linear infinite}@keyframes spin{0%{transform:rotate(0deg)}50%{transform:rotate(180deg)}100%{transform:rotate(360deg)}}.toggle-icon{margin-block:0.75rem;align-self:center;justify-self:end;color:var(--calcite-ui-text-3);transition-property:color;transition-duration:150ms;transition-timing-function:cubic-bezier(0.4, 0, 0.2, 1);margin-inline-end:1rem;margin-inline-start:auto}.toggle:hover .toggle-icon{color:var(--calcite-ui-text-1)}.container{position:relative;display:flex;block-size:100%;flex-direction:column}.content{position:relative;flex:1 1 0%}@keyframes in{0%{opacity:0}100%{opacity:1}}.content{animation:in var(--calcite-internal-animation-timing-slow) ease-in-out}.content--spaced{padding-block:var(--calcite-block-padding, 0.5rem);padding-inline:var(--calcite-block-padding, 0.625rem)}.control-container{margin:0px;display:flex;grid-area:control}calcite-action-menu{grid-area:menu}:host([open]){margin-block:0.5rem}:host([open]) .header .title .heading{color:var(--calcite-ui-text-1)}";

const Block = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteBlockToggle = index.createEvent(this, "calciteBlockToggle", 6);
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, the component is collapsible.
     */
    this.collapsible = false;
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    /**
     * When `true`, displays a drag handle in the header.
     */
    this.dragHandle = false;
    /**
     * Accessible name for the component's collapse button.
     *
     * @default "Collapse"
     */
    this.intlCollapse = TEXT$1.collapse;
    /**
     * Accessible name for the component's expand button.
     *
     * @default "Expand"
     */
    this.intlExpand = TEXT$1.expand;
    /**
     * Accessible name when the component is loading.
     *
     * @default "Loading"
     */
    this.intlLoading = TEXT$1.loading;
    /**
     * Accessible name for the component's options button.
     *
     * @default "Options"
     */
    this.intlOptions = TEXT$1.options;
    /**
     * When `true`, a busy indicator is displayed.
     */
    this.loading = false;
    /**
     * When `true`, expands the component and its contents.
     */
    this.open = false;
    /**
     * When `true`, removes padding for the slotted content.
     *
     * @deprecated Use `--calcite-block-padding` CSS variable instead.
     */
    this.disablePadding = false;
    this.guid = guid.guid();
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.onHeaderClick = () => {
      this.open = !this.open;
      this.calciteBlockToggle.emit();
    };
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentDidRender() {
    interactive.updateHostInteraction(this);
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
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderScrim() {
    const { loading } = this;
    const defaultSlot = index.h("slot", null);
    return [loading ? index.h("calcite-scrim", { loading: loading }) : null, defaultSlot];
  }
  renderIcon() {
    const { el, status } = this;
    const showingLoadingStatus = this.loading && !this.open;
    const statusIcon = showingLoadingStatus ? ICONS$1.refresh : ICONS$1[status];
    const hasIcon = dom.getSlotted(el, SLOTS.icon) || statusIcon;
    const iconEl = !statusIcon ? (index.h("slot", { key: "icon-slot", name: SLOTS.icon })) : (index.h("calcite-icon", { class: {
        [CSS$1.statusIcon]: true,
        [CSS$1.valid]: status == "valid",
        [CSS$1.invalid]: status == "invalid",
        [CSS$1.loading]: showingLoadingStatus
      }, icon: statusIcon, scale: "m" }));
    return hasIcon ? index.h("div", { class: CSS$1.icon }, iconEl) : null;
  }
  renderTitle() {
    const { heading, headingLevel, summary, description } = this;
    return heading || summary || description ? (index.h("div", { class: CSS$1.title }, index.h(Heading.Heading, { class: CSS$1.heading, level: headingLevel || HEADING_LEVEL }, heading), summary || description ? (index.h("div", { class: CSS$1.description }, summary || description)) : null)) : null;
  }
  render() {
    const { collapsible, el, intlCollapse, intlExpand, loading, open, intlLoading } = this;
    const toggleLabel = open ? intlCollapse || TEXT$1.collapse : intlExpand || TEXT$1.expand;
    const headerContent = (index.h("header", { class: CSS$1.header }, this.renderIcon(), this.renderTitle()));
    const hasControl = !!dom.getSlotted(el, SLOTS.control);
    const hasMenuActions = !!dom.getSlotted(el, SLOTS.headerMenuActions);
    const collapseIcon = open ? ICONS$1.opened : ICONS$1.closed;
    const { guid } = this;
    const regionId = `${guid}-region`;
    const buttonId = `${guid}-button`;
    const headerNode = (index.h("div", { class: CSS$1.headerContainer }, this.dragHandle ? index.h("calcite-handle", null) : null, collapsible ? (index.h("button", { "aria-controls": regionId, "aria-expanded": collapsible ? dom.toAriaBoolean(open) : null, "aria-label": toggleLabel, class: CSS$1.toggle, id: buttonId, onClick: this.onHeaderClick, title: toggleLabel }, headerContent, !hasControl && !hasMenuActions ? (index.h("calcite-icon", { "aria-hidden": "true", class: CSS$1.toggleIcon, icon: collapseIcon, scale: "s" })) : null)) : (headerContent), loading ? (index.h("calcite-loader", { inline: true, "is-active": true, label: intlLoading })) : hasControl ? (index.h("div", { class: CSS$1.controlContainer }, index.h("slot", { name: SLOTS.control }))) : null, hasMenuActions ? (index.h("calcite-action-menu", { label: this.intlOptions || TEXT$1.options }, index.h("slot", { name: SLOTS.headerMenuActions }))) : null));
    return (index.h(index.Host, null, index.h("article", { "aria-busy": dom.toAriaBoolean(loading), class: {
        [CSS$1.container]: true
      } }, headerNode, index.h("section", { "aria-expanded": dom.toAriaBoolean(open), "aria-labelledby": buttonId, class: {
        [CSS$1.content]: true,
        [CSS$1.contentSpaced]: !this.disablePadding
      }, hidden: !open, id: regionId }, this.renderScrim()))));
  }
  get el() { return index.getElement(this); }
};
Block.style = blockCss;

const CSS = {
  content: "content",
  invalid: "invalid",
  toggle: "toggle",
  toggleSwitch: "toggle--switch",
  toggleSwitchContent: "toggle--switch__content",
  toggleSwitchText: "toggle--switch__text",
  sectionHeader: "section-header",
  sectionHeaderText: "section-header__text",
  statusIcon: "status-icon",
  valid: "valid"
};
const TEXT = {
  collapse: "Collapse",
  expand: "Expand"
};
const ICONS = {
  menuOpen: "chevron-down",
  menuClosedLeft: "chevron-left",
  menuClosedRight: "chevron-right",
  valid: "check-circle",
  invalid: "exclamation-mark-triangle"
};

const blockSectionCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host{box-sizing:border-box;display:block;background-color:var(--calcite-ui-foreground-1);font-size:var(--calcite-font-size--1);color:var(--calcite-ui-text-2)}:host([open]){border-width:0px;border-block-end-width:1px;border-style:solid;border-block-end-color:var(--calcite-ui-border-3)}:host(:last-child){border-block-end-width:0px}.toggle{inline-size:100%;border-width:0px;background-color:transparent;font-family:var(--calcite-sans-family);font-weight:var(--calcite-font-weight-medium);color:var(--calcite-ui-text-2)}.toggle--switch,.section-header{margin-inline:0px;margin-block:0.25rem;display:flex;cursor:pointer;-webkit-user-select:none;user-select:none;align-items:center;padding-inline:0px;padding-block:0.5rem;font-size:var(--calcite-font-size--1);outline-color:transparent}.toggle--switch:focus,.section-header:focus{outline:2px solid var(--calcite-ui-brand);outline-offset:2px}.toggle--switch:hover,.section-header:hover{color:var(--calcite-ui-text-1)}.section-header .status-icon{align-self:flex-end}.section-header__text{margin-inline:0.75rem;margin-block:0px;flex:1 1 auto;text-align:initial;word-wrap:anywhere}.toggle--switch calcite-switch{pointer-events:none;margin-inline-start:0.25rem}.toggle--switch .status-icon{margin-inline-start:0.5rem}.toggle--switch__content{display:flex;flex:1 1 auto;align-items:center}.status-icon.valid{color:var(--calcite-ui-success)}.status-icon.invalid{color:var(--calcite-ui-danger)}";

const BlockSection = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteBlockSectionToggle = index.createEvent(this, "calciteBlockSectionToggle", 6);
    /**
     * When `true`, expands the component and its contents.
     */
    this.open = false;
    /**
     * Specifies the component's toggle display -
     *
     * `"button"` (selectable header), or
     *
     * `"switch"` (toggle switch).
     */
    this.toggleDisplay = "button";
    this.guid = guid.guid();
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.handleHeaderKeyDown = (event) => {
      if (key.isActivationKey(event.key)) {
        this.toggleSection();
        event.preventDefault();
        event.stopPropagation();
      }
    };
    this.toggleSection = () => {
      this.open = !this.open;
      this.calciteBlockSectionToggle.emit();
    };
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderStatusIcon() {
    var _a;
    const { status } = this;
    const statusIcon = (_a = ICONS[status]) !== null && _a !== void 0 ? _a : false;
    const statusIconClasses = {
      [CSS.statusIcon]: true,
      [CSS.valid]: status == "valid",
      [CSS.invalid]: status == "invalid"
    };
    return !!statusIcon ? (index.h("calcite-icon", { class: statusIconClasses, icon: statusIcon, scale: "s" })) : null;
  }
  render() {
    const { el, intlCollapse, intlExpand, open, text, toggleDisplay } = this;
    const dir = dom.getElementDir(el);
    const arrowIcon = open
      ? ICONS.menuOpen
      : dir === "rtl"
        ? ICONS.menuClosedLeft
        : ICONS.menuClosedRight;
    const toggleLabel = open ? intlCollapse || TEXT.collapse : intlExpand || TEXT.expand;
    const { guid } = this;
    const regionId = `${guid}-region`;
    const buttonId = `${guid}-button`;
    const headerNode = toggleDisplay === "switch" ? (index.h("div", { "aria-controls": regionId, "aria-label": toggleLabel, class: {
        [CSS.toggle]: true,
        [CSS.toggleSwitch]: true
      }, id: buttonId, onClick: this.toggleSection, onKeyDown: this.handleHeaderKeyDown, tabIndex: 0, title: toggleLabel }, index.h("div", { class: CSS.toggleSwitchContent }, index.h("span", { class: CSS.toggleSwitchText }, text)), index.h("calcite-switch", { checked: open, label: toggleLabel, scale: "s", tabIndex: -1 }), this.renderStatusIcon())) : (index.h("button", { "aria-controls": regionId, "aria-label": toggleLabel, class: {
        [CSS.sectionHeader]: true,
        [CSS.toggle]: true
      }, id: buttonId, name: toggleLabel, onClick: this.toggleSection }, index.h("calcite-icon", { icon: arrowIcon, scale: "s" }), index.h("span", { class: CSS.sectionHeaderText }, text), this.renderStatusIcon()));
    return (index.h(index.Host, null, headerNode, index.h("section", { "aria-expanded": dom.toAriaBoolean(open), "aria-labelledby": buttonId, class: CSS.content, hidden: !open, id: regionId }, index.h("slot", null))));
  }
  get el() { return index.getElement(this); }
};
BlockSection.style = blockSectionCss;

exports.calcite_block = Block;
exports.calcite_block_section = BlockSection;
