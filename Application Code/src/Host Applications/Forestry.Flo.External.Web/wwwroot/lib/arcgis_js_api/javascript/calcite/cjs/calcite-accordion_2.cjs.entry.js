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
const resources = require('./resources-3fd6da1b.js');
require('./guid-acbbb0e7.js');
require('./observers-664fbf90.js');

const accordionCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([scale=s]){--calcite-accordion-item-spacing-unit:0.25rem;--calcite-accordion-icon-margin:0.5rem;--calcite-accordion-item-padding:var(--calcite-accordion-item-spacing-unit) 0.5rem;font-size:var(--calcite-font-size--2);line-height:1rem}:host([scale=m]){--calcite-accordion-item-spacing-unit:0.5rem;--calcite-accordion-icon-margin:0.75rem;--calcite-accordion-item-padding:var(--calcite-accordion-item-spacing-unit) 0.75rem;font-size:var(--calcite-font-size--1);line-height:1rem}:host([scale=l]){--calcite-accordion-item-spacing-unit:0.75rem;--calcite-accordion-icon-margin:1rem;--calcite-accordion-item-padding:var(--calcite-accordion-item-spacing-unit) 1rem;font-size:var(--calcite-font-size-0);line-height:1.25rem}:host{position:relative;display:block;max-inline-size:100%;line-height:1.5rem;--calcite-accordion-item-border:var(--calcite-ui-border-2);--calcite-accordion-item-background:var(--calcite-ui-foreground-1)}.accordion--transparent{--calcite-accordion-item-border:transparent;--calcite-accordion-item-background:transparent}.accordion--minimal{--calcite-accordion-item-padding:var(--calcite-accordion-item-spacing-unit) 0}.accordion{border-width:1px;border-block-end-width:0px;border-style:solid;border-color:var(--calcite-ui-border-2)}";

const Accordion = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteInternalAccordionChange = index.createEvent(this, "calciteInternalAccordionChange", 6);
    //--------------------------------------------------------------------------
    //
    //  Public Properties
    //
    //--------------------------------------------------------------------------
    /** Specifies the appearance of the component. */
    this.appearance = "solid";
    /** Specifies the placement of the icon in the header. */
    this.iconPosition = "end";
    /** Specifies the type of the icon in the header. */
    this.iconType = "chevron";
    /** Specifies the size of the component. */
    this.scale = "m";
    /**
     * Specifies the selection mode - "multiple" (allow any number of open items), "single" (allow one open item),
     * or "single-persist" (allow and require one open item).
     */
    this.selectionMode = "multi";
    //--------------------------------------------------------------------------
    //
    //  Private State/Props
    //
    //--------------------------------------------------------------------------
    /** created list of Accordion items */
    this.items = [];
    /** keep track of whether the items have been sorted so we don't re-sort */
    this.sorted = false;
    this.sortItems = (items) => items.sort((a, b) => a.position - b.position).map((a) => a.item);
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentDidLoad() {
    if (!this.sorted) {
      this.items = this.sortItems(this.items);
      this.sorted = true;
    }
  }
  render() {
    const transparent = this.appearance === "transparent";
    const minimal = this.appearance === "minimal";
    return (index.h("div", { class: {
        "accordion--transparent": transparent,
        "accordion--minimal": minimal,
        accordion: !transparent && !minimal
      } }, index.h("slot", null)));
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  calciteInternalAccordionItemKeyEvent(event) {
    const item = event.detail.item;
    const parent = event.detail.parent;
    if (this.el === parent) {
      const { key } = item;
      const itemToFocus = event.target;
      const isFirstItem = this.itemIndex(itemToFocus) === 0;
      const isLastItem = this.itemIndex(itemToFocus) === this.items.length - 1;
      switch (key) {
        case "ArrowDown":
          if (isLastItem) {
            this.focusFirstItem();
          }
          else {
            this.focusNextItem(itemToFocus);
          }
          break;
        case "ArrowUp":
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
    }
    event.stopPropagation();
  }
  registerCalciteAccordionItem(event) {
    const item = {
      item: event.target,
      parent: event.detail.parent,
      position: event.detail.position
    };
    if (this.el === item.parent) {
      this.items.push(item);
    }
    event.stopPropagation();
  }
  updateActiveItemOnChange(event) {
    this.requestedAccordionItem = event.detail.requestedAccordionItem;
    this.calciteInternalAccordionChange.emit({
      requestedAccordionItem: this.requestedAccordionItem
    });
    event.stopPropagation();
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  focusFirstItem() {
    const firstItem = this.items[0];
    this.focusElement(firstItem);
  }
  focusLastItem() {
    const lastItem = this.items[this.items.length - 1];
    this.focusElement(lastItem);
  }
  focusNextItem(el) {
    const index = this.itemIndex(el);
    const nextItem = this.items[index + 1] || this.items[0];
    this.focusElement(nextItem);
  }
  focusPrevItem(el) {
    const index = this.itemIndex(el);
    const prevItem = this.items[index - 1] || this.items[this.items.length - 1];
    this.focusElement(prevItem);
  }
  itemIndex(el) {
    return this.items.indexOf(el);
  }
  focusElement(item) {
    const target = item;
    target === null || target === void 0 ? void 0 : target.focus();
  }
  get el() { return index.getElement(this); }
};
Accordion.style = accordionCss;

const SLOTS = {
  actionsStart: "actions-start",
  actionsEnd: "actions-end"
};
const CSS = {
  icon: "icon",
  header: "header",
  headerContent: "header-content",
  actionsStart: "actions-start",
  actionsEnd: "actions-end",
  headerText: "header-text",
  heading: "heading",
  description: "description",
  expandIcon: "expand-icon",
  content: "content",
  iconStart: "icon--start",
  iconEnd: "icon--end",
  headerContainer: "header-container"
};

const accordionItemCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}.icon-position--end,.icon-position--start{--calcite-accordion-item-icon-rotation:calc(90deg * -1);--calcite-accordion-item-active-icon-rotation:0deg;--calcite-accordion-item-icon-rotation-rtl:90deg;--calcite-accordion-item-active-icon-rotation-rtl:0deg}.icon-position--start{--calcite-accordion-item-flex-direction:row-reverse;--calcite-accordion-item-icon-spacing-start:0;--calcite-accordion-item-icon-spacing-end:var(--calcite-accordion-icon-margin)}.icon-position--end{--calcite-accordion-item-flex-direction:row;--calcite-accordion-item-icon-spacing-start:var(--calcite-accordion-icon-margin);--calcite-accordion-item-icon-spacing-end:0}.icon-position--end:not(.icon-type--plus-minus){--calcite-accordion-item-icon-rotation:0deg;--calcite-accordion-item-active-icon-rotation:180deg;--calcite-accordion-item-icon-rotation-rtl:0deg;--calcite-accordion-item-active-icon-rotation-rtl:calc(180deg * -1)}:host{position:relative;display:flex;flex-direction:column;color:var(--calcite-ui-text-3);text-decoration-line:none;background-color:var(--calcite-accordion-item-background, var(--calcite-ui-foreground-1))}:host .header-content{outline-color:transparent}:host(:focus) .header-content{outline:2px solid transparent;outline:2px solid var(--calcite-ui-brand);outline-offset:-2px}:host([expanded]){color:var(--calcite-ui-text-1)}:host([expanded]) .content{display:block;color:var(--calcite-ui-text-1)}:host([expanded]) .header{border-block-end-color:transparent}:host .header{display:flex;align-items:stretch}:host .icon{position:relative;margin:0px;display:inline-flex;color:var(--calcite-ui-text-3);transition-duration:150ms;transition-timing-function:cubic-bezier(0.4, 0, 0.2, 1);margin-inline-end:var(--calcite-accordion-item-icon-spacing-start);margin-inline-start:var(--calcite-accordion-item-icon-spacing-end)}.icon--start{display:flex;align-items:center;margin-inline-end:var(--calcite-accordion-icon-margin)}.icon--end{display:flex;align-items:center;margin-inline-end:var(--calcite-accordion-icon-margin);margin-inline-start:var(--calcite-accordion-icon-margin)}.header-container{inline-size:100%}.content{padding:var(--calcite-accordion-item-padding)}:host .content,:host .header{border-block-end:1px solid var(--calcite-accordion-item-border, var(--calcite-ui-border-2))}:host .header *{display:inline-flex;align-items:center;transition-duration:150ms;transition-timing-function:cubic-bezier(0.4, 0, 0.2, 1)}:host .content{display:none;padding-block-start:0px;color:var(--calcite-ui-text-3);text-align:initial}:host .expand-icon{color:var(--calcite-ui-text-3);margin-inline-start:var(--calcite-accordion-item-icon-spacing-start);margin-inline-end:var(--calcite-accordion-item-icon-spacing-end);transform:rotate(var(--calcite-accordion-item-icon-rotation))}.calcite--rtl .expand-icon{transform:rotate(var(--calcite-accordion-item-icon-rotation-rtl))}:host([expanded]) .expand-icon{color:var(--calcite-ui-text-1);transform:rotate(var(--calcite-accordion-item-active-icon-rotation))}:host([expanded]) .calcite--rtl .expand-icon{transform:rotate(var(--calcite-accordion-item-active-icon-rotation-rtl))}:host .header-text{margin-block:0px;flex-grow:1;flex-direction:column;padding-block:0px;text-align:initial;margin-inline-end:auto}:host .heading,:host .description{display:flex;inline-size:100%}:host .heading{font-weight:var(--calcite-font-weight-medium);color:var(--calcite-ui-text-2)}:host .description{margin-block-start:0.25rem;color:var(--calcite-ui-text-3)}:host(:focus) .heading,:host(:hover) .heading{color:var(--calcite-ui-text-1)}:host(:focus) .icon,:host(:hover) .icon{color:var(--calcite-ui-text-1)}:host(:focus) .expand-icon,:host(:hover) .expand-icon{color:var(--calcite-ui-text-1)}:host(:focus) .description,:host(:hover) .description{color:var(--calcite-ui-text-2)}:host(:focus) .heading,:host(:active) .heading,:host([expanded]) .heading{color:var(--calcite-ui-text-1)}:host(:focus) .icon,:host(:active) .icon,:host([expanded]) .icon{color:var(--calcite-ui-text-1)}:host(:focus) .expand-icon,:host(:active) .expand-icon,:host([expanded]) .expand-icon{color:var(--calcite-ui-text-1)}:host(:focus) .description,:host(:active) .description,:host([expanded]) .description{color:var(--calcite-ui-text-2)}.header-content{flex-grow:1;cursor:pointer;padding:var(--calcite-accordion-item-padding);flex-direction:var(--calcite-accordion-item-flex-direction)}.actions-start,.actions-end{display:flex;align-items:center}@media (forced-colors: active){:host([expanded]) .header{border-block-end:none}:host([expanded]) .heading{font-weight:bolder}:host(:hover) .heading,:host(:focus) .heading{text-decoration:underline}}";

const AccordionItem = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteInternalAccordionItemKeyEvent = index.createEvent(this, "calciteInternalAccordionItemKeyEvent", 6);
    this.calciteInternalAccordionItemSelect = index.createEvent(this, "calciteInternalAccordionItemSelect", 6);
    this.calciteInternalAccordionItemClose = index.createEvent(this, "calciteInternalAccordionItemClose", 6);
    this.calciteInternalAccordionItemRegister = index.createEvent(this, "calciteInternalAccordionItemRegister", 6);
    //--------------------------------------------------------------------------
    //
    //  Public Properties
    //
    //--------------------------------------------------------------------------
    /**
     * When `true`, the component is active.
     *
     * @deprecated use `expanded` instead.
     */
    this.active = false;
    /** When `true`, the component is expanded. */
    this.expanded = false;
    /** what icon position does the parent accordion specify */
    this.iconPosition = "end";
    /** handle clicks on item header */
    this.itemHeaderClickHandler = () => this.emitRequestedItem();
  }
  activeHandler(value) {
    this.expanded = value;
  }
  expandedHandler(value) {
    this.active = value;
  }
  iconHandler(value) {
    this.iconStart = value;
  }
  iconStartHandler(value) {
    this.icon = value;
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    this.parent = this.el.parentElement;
    this.selectionMode = dom.getElementProp(this.el, "selection-mode", "multi");
    this.iconType = dom.getElementProp(this.el, "icon-type", "chevron");
    this.iconPosition = dom.getElementProp(this.el, "icon-position", this.iconPosition);
    const isExpanded = this.active || this.expanded;
    if (isExpanded) {
      this.activeHandler(isExpanded);
      this.expandedHandler(isExpanded);
    }
    if (this.iconStart) {
      this.icon = this.iconStart;
    }
    else if (this.icon) {
      this.iconStart = this.icon;
    }
    conditionalSlot.connectConditionalSlotComponent(this);
  }
  componentDidLoad() {
    this.itemPosition = this.getItemPosition();
    this.calciteInternalAccordionItemRegister.emit({
      parent: this.parent,
      position: this.itemPosition
    });
  }
  disconnectedCallback() {
    conditionalSlot.disconnectConditionalSlotComponent(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderActionsStart() {
    const { el } = this;
    return dom.getSlotted(el, SLOTS.actionsStart) ? (index.h("div", { class: CSS.actionsStart }, index.h("slot", { name: SLOTS.actionsStart }))) : null;
  }
  renderActionsEnd() {
    const { el } = this;
    return dom.getSlotted(el, SLOTS.actionsEnd) ? (index.h("div", { class: CSS.actionsEnd }, index.h("slot", { name: SLOTS.actionsEnd }))) : null;
  }
  render() {
    const dir = dom.getElementDir(this.el);
    const iconStartEl = this.iconStart ? (index.h("calcite-icon", { class: CSS.iconStart, icon: this.iconStart, key: "icon-start", scale: "s" })) : null;
    const iconEndEl = this.iconEnd ? (index.h("calcite-icon", { class: CSS.iconEnd, icon: this.iconEnd, key: "icon-end", scale: "s" })) : null;
    const description = this.description || this.itemSubtitle;
    return (index.h(index.Host, null, index.h("div", { class: {
        [`icon-position--${this.iconPosition}`]: true,
        [`icon-type--${this.iconType}`]: true
      } }, index.h("div", { class: { [CSS.header]: true, [resources.CSS_UTILITY.rtl]: dir === "rtl" } }, this.renderActionsStart(), index.h("div", { "aria-expanded": dom.toAriaBoolean(this.active || this.expanded), class: CSS.headerContent, onClick: this.itemHeaderClickHandler, role: "button", tabindex: "0" }, index.h("div", { class: CSS.headerContainer }, iconStartEl, index.h("div", { class: CSS.headerText }, index.h("span", { class: CSS.heading }, this.heading || this.itemTitle), description ? index.h("span", { class: CSS.description }, description) : null), iconEndEl), index.h("calcite-icon", { class: CSS.expandIcon, icon: this.iconType === "chevron"
        ? "chevronDown"
        : this.iconType === "caret"
          ? "caretDown"
          : this.expanded || this.active
            ? "minus"
            : "plus", scale: "s" })), this.renderActionsEnd()), index.h("div", { class: CSS.content }, index.h("slot", null)))));
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  keyDownHandler(event) {
    if (event.target === this.el) {
      switch (event.key) {
        case " ":
        case "Enter":
          this.emitRequestedItem();
          event.preventDefault();
          break;
        case "ArrowUp":
        case "ArrowDown":
        case "Home":
        case "End":
          this.calciteInternalAccordionItemKeyEvent.emit({
            parent: this.parent,
            item: event
          });
          event.preventDefault();
          break;
      }
    }
  }
  updateActiveItemOnChange(event) {
    this.requestedAccordionItem = event.detail
      .requestedAccordionItem;
    if (this.el.parentNode !== this.requestedAccordionItem.parentNode) {
      return;
    }
    this.determineActiveItem();
    event.stopPropagation();
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  determineActiveItem() {
    switch (this.selectionMode) {
      case "multi":
      case "multiple":
        if (this.el === this.requestedAccordionItem) {
          this.expanded = !this.expanded;
        }
        break;
      case "single":
        this.expanded = this.el === this.requestedAccordionItem ? !this.expanded : false;
        break;
      case "single-persist":
        this.expanded = this.el === this.requestedAccordionItem;
        break;
    }
  }
  emitRequestedItem() {
    this.calciteInternalAccordionItemSelect.emit({
      requestedAccordionItem: this.el
    });
  }
  getItemPosition() {
    return Array.prototype.indexOf.call(this.parent.querySelectorAll("calcite-accordion-item"), this.el);
  }
  get el() { return index.getElement(this); }
  static get watchers() { return {
    "active": ["activeHandler"],
    "expanded": ["expandedHandler"],
    "icon": ["iconHandler"],
    "iconStart": ["iconStartHandler"]
  }; }
};
AccordionItem.style = accordionItemCss;

exports.calcite_accordion = Accordion;
exports.calcite_accordion_item = AccordionItem;
