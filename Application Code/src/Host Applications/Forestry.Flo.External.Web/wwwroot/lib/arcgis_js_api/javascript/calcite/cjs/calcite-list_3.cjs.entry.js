/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
'use strict';

Object.defineProperty(exports, '__esModule', { value: true });

const index = require('./index-b27b231c.js');
const interactive = require('./interactive-3d681fb9.js');
const dom = require('./dom-2b919cb6.js');
const conditionalSlot = require('./conditionalSlot-f62c357a.js');
const Heading = require('./Heading-60920dbe.js');
require('./resources-3fd6da1b.js');
require('./guid-acbbb0e7.js');
require('./observers-664fbf90.js');

const CSS$2 = {
  container: "container"
};

const listCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{display:block}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}.container{box-sizing:border-box;display:flex;inline-size:100%;flex-direction:column;background-color:transparent}.container *{box-sizing:border-box}::slotted(calcite-list-item){margin-block-end:1px;--tw-shadow:0 1px 0 var(--calcite-ui-border-3);--tw-shadow-colored:0 1px 0 var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)}::slotted(calcite-list-item:last-child){--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)}";

const List = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
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
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    const firstListItem = this.el.querySelector(`calcite-list-item:not([non-interactive])`);
    firstListItem === null || firstListItem === void 0 ? void 0 : firstListItem.setFocus();
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    return (index.h(index.Host, { role: "list" }, index.h("div", { class: CSS$2.container }, index.h("slot", null))));
  }
  get el() { return index.getElement(this); }
};
List.style = listCss;

const CSS$1 = {
  container: "container",
  contentContainer: "content-container",
  nestedContainer: "nested-container",
  contentContainerButton: "content-container--button",
  content: "content",
  actionsStart: "actions-start",
  contentStart: "content-start",
  label: "label",
  description: "description",
  contentEnd: "content-end",
  actionsEnd: "actions-end",
  hasCenterContent: "has-center-content"
};
const SLOTS = {
  actionsStart: "actions-start",
  contentStart: "content-start",
  contentEnd: "content-end",
  actionsEnd: "actions-end"
};

const listItemCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{display:flex;flex-direction:column}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}.container{box-sizing:border-box;display:flex;flex:1 1 0%;background-color:var(--calcite-ui-foreground-1);font-family:var(--calcite-sans-family)}.container *{box-sizing:border-box}.nested-container{display:flex;flex-direction:column;background-color:var(--calcite-ui-foreground-1)}.content-container{display:flex;flex:1 1 auto;align-items:stretch;padding:0px;font-family:var(--calcite-sans-family);font-weight:var(--calcite-font-weight-normal);color:var(--calcite-ui-text-2);outline-color:transparent}.content-container--button{cursor:pointer;border-style:none;background-color:var(--calcite-ui-foreground-1);outline-color:transparent;text-align:initial}.content-container--button:hover{background-color:var(--calcite-ui-foreground-2)}.content-container--button:hover .label,.content-container--button:hover .description{color:var(--calcite-ui-text-1)}.content-container--button:focus{outline:2px solid var(--calcite-ui-brand);outline-offset:-2px}.content-container--button .content-start,.content-container--button .content-end{pointer-events:none}.content{display:flex;flex:1 1 auto;flex-direction:column;justify-content:center;padding-inline:0.75rem;padding-block:0.5rem;font-size:var(--calcite-font-size--2);line-height:1.375}.label,.description{font-family:var(--calcite-sans-family);font-size:var(--calcite-font-size--2);font-weight:var(--calcite-font-weight-normal);word-wrap:break-word;word-break:break-word}.label:only-child,.description:only-child{margin:0px;padding-block:0.25rem}.label{color:var(--calcite-ui-text-1)}.description{margin-block-start:0.125rem;color:var(--calcite-ui-text-3)}.content-start{justify-content:flex-start}.content-end{justify-content:flex-end}.content-start,.content-end{flex:1 1 auto}.has-center-content .content-start,.has-center-content .content-end{flex:0 1 auto}.actions-start,.actions-end,.content-start,.content-end{display:flex;align-items:center}.content-start ::slotted(calcite-icon),.content-end ::slotted(calcite-icon){margin-inline:0.75rem;align-self:center}.actions-start ::slotted(calcite-action),.actions-end ::slotted(calcite-action){align-self:stretch;color:inherit}::slotted(calcite-list-item-group),::slotted(calcite-list-item){padding-inline-start:0.5rem}";

const ListItem = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, prevents the content of the component from user interaction.
     */
    this.nonInteractive = false;
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
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
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.focusEl) === null || _a === void 0 ? void 0 : _a.focus();
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderActionsStart() {
    const { el } = this;
    return dom.getSlotted(el, SLOTS.actionsStart) ? (index.h("div", { class: CSS$1.actionsStart }, index.h("slot", { name: SLOTS.actionsStart }))) : null;
  }
  renderActionsEnd() {
    const { el } = this;
    return dom.getSlotted(el, SLOTS.actionsEnd) ? (index.h("div", { class: CSS$1.actionsEnd }, index.h("slot", { name: SLOTS.actionsEnd }))) : null;
  }
  renderContentStart() {
    const { el } = this;
    return dom.getSlotted(el, SLOTS.contentStart) ? (index.h("div", { class: CSS$1.contentStart }, index.h("slot", { name: SLOTS.contentStart }))) : null;
  }
  renderContentEnd() {
    const { el } = this;
    return dom.getSlotted(el, SLOTS.contentEnd) ? (index.h("div", { class: CSS$1.contentEnd }, index.h("slot", { name: SLOTS.contentEnd }))) : null;
  }
  renderContent() {
    const { label, description } = this;
    return !!label || !!description ? (index.h("div", { class: CSS$1.content }, label ? index.h("div", { class: CSS$1.label }, label) : null, description ? index.h("div", { class: CSS$1.description }, description) : null)) : null;
  }
  renderContentContainer() {
    const { description, disabled, label, nonInteractive } = this;
    const hasCenterContent = !!label || !!description;
    const content = [this.renderContentStart(), this.renderContent(), this.renderContentEnd()];
    return !nonInteractive ? (index.h("button", { class: {
        [CSS$1.contentContainer]: true,
        [CSS$1.contentContainerButton]: true,
        [CSS$1.hasCenterContent]: hasCenterContent
      }, disabled: disabled, ref: (focusEl) => (this.focusEl = focusEl) }, content)) : (index.h("div", { class: { [CSS$1.contentContainer]: true, [CSS$1.hasCenterContent]: hasCenterContent }, ref: () => (this.focusEl = null) }, content));
  }
  render() {
    return (index.h(index.Host, { role: "listitem" }, index.h("div", { class: CSS$1.container }, this.renderActionsStart(), this.renderContentContainer(), this.renderActionsEnd()), index.h("div", { class: CSS$1.nestedContainer }, index.h("slot", null))));
  }
  get el() { return index.getElement(this); }
};
ListItem.style = listItemCss;

const CSS = {
  heading: "heading",
  container: "container"
};
const HEADING_LEVEL = 3;

const listItemGroupCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host{display:flex;flex-direction:column}.heading{margin:0px;display:flex;flex:1 1 0%;background-color:var(--calcite-ui-foreground-2);padding:0.75rem;font-family:var(--calcite-sans-family);font-size:var(--calcite-font-size--1);font-weight:var(--calcite-font-weight-bold);color:var(--calcite-ui-text-2)}.container{display:flex;inline-size:100%;flex-direction:column;background-color:var(--calcite-ui-foreground-1)}::slotted(calcite-list-item-group){padding-inline-start:0.5rem}";

const ListItemGroup = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
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
    const relativeLevel = parentLevel ? Heading.constrainHeadingLevel(parentLevel + 1) : null;
    const level = headingLevel || relativeLevel || HEADING_LEVEL;
    return (index.h(index.Host, null, heading ? (index.h(Heading.Heading, { class: CSS.heading, level: level }, heading)) : null, index.h("div", { class: CSS.container, role: "group" }, index.h("slot", null))));
  }
  get el() { return index.getElement(this); }
};
ListItemGroup.style = listItemGroupCss;

exports.calcite_list = List;
exports.calcite_list_item = ListItem;
exports.calcite_list_item_group = ListItemGroup;
