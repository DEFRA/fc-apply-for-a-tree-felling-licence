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
const observers = require('./observers-664fbf90.js');
require('./resources-3fd6da1b.js');
require('./guid-acbbb0e7.js');

const CSS$1 = {
  header: "header",
  heading: "heading",
  close: "close",
  container: "container",
  tipContainer: "tip-container",
  tipContainerAdvancing: "tip-container--advancing",
  tipContainerRetreating: "tip-container--retreating",
  pagination: "pagination",
  pagePosition: "page-position",
  pageNext: "page-next",
  pagePrevious: "page-previous"
};
const ICONS$1 = {
  chevronLeft: "chevron-left",
  chevronRight: "chevron-right",
  close: "x"
};
const TEXT$1 = {
  defaultGroupTitle: "Tips",
  defaultPaginationLabel: "Tip",
  close: "Close",
  previous: "Previous",
  next: "Next"
};
const HEADING_LEVEL$1 = 2;

const CSS = {
  container: "container",
  header: "header",
  heading: "heading",
  close: "close",
  imageFrame: "image-frame",
  content: "content",
  info: "info"
};
const ICONS = {
  close: "x"
};
const SLOTS = {
  thumbnail: "thumbnail"
};
const TEXT = {
  close: "Close"
};
const HEADING_LEVEL = (HEADING_LEVEL$1 + 1);

const tipCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host{position:relative;margin:1rem;box-sizing:border-box;display:flex;flex-direction:row;border-width:1px;border-style:solid;border-color:var(--calcite-ui-border-2);background-color:var(--calcite-ui-foreground-1);font-size:var(--calcite-font-size--1);line-height:1rem;color:var(--calcite-ui-text-2)}:host *{box-sizing:border-box}.container{inline-size:100%;padding:1rem}:host([dismissed]),:host([dismissed]) .container{display:none}:host([selected]) .container{margin:0px;border-style:none;padding:0px}.header{margin:0px;display:flex;align-content:space-between;align-items:center;fill:var(--calcite-ui-text-2);color:var(--calcite-ui-text-2)}.heading{margin:0px;padding:0px;font-weight:var(--calcite-font-weight-medium)}.header .heading{flex:1 1 auto;padding:0.5rem}.header{margin-block-end:0.5rem}.header .heading{padding:0px;font-size:var(--calcite-font-size-0);line-height:1.25rem;color:var(--calcite-ui-text-1)}.container[hidden]{display:none}.content{display:flex}.info{padding-block:0px;padding-inline:1rem;inline-size:70%}.info:only-child{inline-size:100%;padding-inline:0px}::slotted(p){margin-block-start:0px}::slotted(a){outline-color:transparent;color:var(--calcite-ui-brand)}::slotted(a:focus){outline:2px solid var(--calcite-ui-brand);outline-offset:2px}.image-frame{inline-size:25%}.image-frame img{max-inline-size:100%}::slotted(img){max-inline-size:100%}";

const Tip = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteTipDismiss = index.createEvent(this, "calciteTipDismiss", 6);
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, the component does not display.
     */
    this.dismissed = false;
    /**
     * When `true`, the close button is not present on the component.
     */
    this.nonDismissible = false;
    /**
     * When `true`, the component is selected if it has a parent `calcite-tip-manager`.
     *
     * Only one tip can be selected within the `calcite-tip-manager` parent.
     */
    this.selected = false;
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.hideTip = () => {
      this.dismissed = true;
      this.calciteTipDismiss.emit();
    };
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
  renderHeader() {
    var _a;
    const { heading, headingLevel, el } = this;
    const parentLevel = (_a = el.closest("calcite-tip-manager")) === null || _a === void 0 ? void 0 : _a.headingLevel;
    const relativeLevel = parentLevel ? Heading.constrainHeadingLevel(parentLevel + 1) : null;
    const level = headingLevel || relativeLevel || HEADING_LEVEL;
    return heading ? (index.h("header", { class: CSS.header }, index.h(Heading.Heading, { class: CSS.heading, level: level }, heading))) : null;
  }
  renderDismissButton() {
    const { nonDismissible, hideTip, intlClose } = this;
    const text = intlClose || TEXT.close;
    return !nonDismissible ? (index.h("calcite-action", { class: CSS.close, icon: ICONS.close, onClick: hideTip, scale: "l", text: text })) : null;
  }
  renderImageFrame() {
    const { el } = this;
    return dom.getSlotted(el, SLOTS.thumbnail) ? (index.h("div", { class: CSS.imageFrame, key: "thumbnail" }, index.h("slot", { name: SLOTS.thumbnail }))) : null;
  }
  renderInfoNode() {
    return (index.h("div", { class: CSS.info }, index.h("slot", null)));
  }
  renderContent() {
    return (index.h("div", { class: CSS.content }, this.renderImageFrame(), this.renderInfoNode()));
  }
  render() {
    return (index.h(index.Fragment, null, index.h("article", { class: CSS.container }, this.renderHeader(), this.renderContent()), this.renderDismissButton()));
  }
  get el() { return index.getElement(this); }
};
Tip.style = tipCss;

const tipGroupCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host{box-sizing:border-box;display:block;background-color:var(--calcite-ui-foreground-1);font-size:var(--calcite-font-size--1);line-height:1rem;color:var(--calcite-ui-text-2)}::slotted(calcite-tip){margin:0px;border-style:none}:host-context(calcite-tip-manager){margin-block:0.75rem}:host-context(calcite-tip-manager) ::slotted(calcite-tip){padding:1rem}";

const TipGroup = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
  }
  render() {
    return index.h("slot", null);
  }
};
TipGroup.style = tipGroupCss;

const tipManagerCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host{box-sizing:border-box;display:block;background-color:var(--calcite-ui-foreground-1);font-size:var(--calcite-font-size--1);line-height:1rem;color:var(--calcite-ui-text-2);--calcite-tip-manager-height:19vh}:host *{box-sizing:border-box}:host([closed]){display:none}.header{margin:0px;display:flex;align-content:space-between;align-items:center;fill:var(--calcite-ui-text-2);color:var(--calcite-ui-text-2)}.heading{margin:0px;padding:0px;font-weight:var(--calcite-font-weight-medium)}.header .heading{flex:1 1 auto;padding:0.5rem}.header{border-width:0px;border-block-end-width:1px;border-style:solid;border-color:var(--calcite-ui-border-3);padding-block:0px;padding-inline-end:0px;padding-inline-start:1rem}.header .heading{padding:0px;font-size:var(--calcite-font-size-1);line-height:1.5rem;font-weight:var(--calcite-font-weight-bold);color:var(--calcite-ui-text-1)}.container{position:relative;overflow:hidden;outline-color:transparent;min-block-size:150px}.container:focus{outline:2px solid var(--calcite-ui-brand);outline-offset:2px}.tip-container{margin-block-start:1px;display:flex;align-items:flex-start;justify-content:center;overflow:auto;outline-color:transparent;animation-name:none;animation-duration:var(--calcite-animation-timing);block-size:var(--calcite-tip-manager-height)}.tip-container:focus{outline:2px solid var(--calcite-ui-brand);outline-offset:2px}::slotted(calcite-tip){border-style:none;padding:0.75rem;max-inline-size:var(--calcite-tip-max-width)}.tip-container--advancing{animation-name:tip-advance}.tip-container--retreating{animation-name:tip-retreat}.pagination{display:flex;align-items:center;justify-content:center;padding-inline:0px;padding-block:0.75rem 0.5rem}.page-position{margin-block:0px;margin-inline:0.5rem;font-size:var(--calcite-font-size--2);line-height:1rem}@keyframes tip-advance{0%{opacity:0;transform:translate3d(50px, 0, 0) scale(0.99)}100%{opacity:1;transform:translate3d(0, 0, 0) scale(1)}}@keyframes tip-retreat{0%{opacity:0;transform:translate3d(-50px, 0, 0) scale(0.99)}100%{opacity:1;transform:translate3d(0, 0, 0) scale(1)}}";

const TipManager = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteTipManagerToggle = index.createEvent(this, "calciteTipManagerToggle", 6);
    this.calciteTipManagerClose = index.createEvent(this, "calciteTipManagerClose", 6);
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, does not display or position the component.
     */
    this.closed = false;
    this.mutationObserver = observers.createObserver("mutation", () => this.setUpTips());
    this.hideTipManager = () => {
      this.closed = true;
      this.calciteTipManagerToggle.emit();
      this.calciteTipManagerClose.emit();
    };
    this.previousClicked = () => {
      this.previousTip();
    };
    this.nextClicked = () => {
      this.nextTip();
    };
    this.tipManagerKeyDownHandler = (event) => {
      if (event.target !== this.container) {
        return;
      }
      switch (event.key) {
        case "ArrowRight":
          event.preventDefault();
          this.nextTip();
          break;
        case "ArrowLeft":
          event.preventDefault();
          this.previousTip();
          break;
        case "Home":
          event.preventDefault();
          this.selectedIndex = 0;
          break;
        case "End":
          event.preventDefault();
          this.selectedIndex = this.total - 1;
          break;
      }
    };
    this.storeContainerRef = (el) => {
      this.container = el;
    };
  }
  closedChangeHandler() {
    this.direction = null;
    this.calciteTipManagerToggle.emit();
  }
  selectedChangeHandler() {
    this.showSelectedTip();
    this.updateGroupTitle();
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    var _a;
    this.setUpTips();
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el, { childList: true, subtree: true });
  }
  disconnectedCallback() {
    var _a;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /** Selects the next `calcite-tip` to display. */
  async nextTip() {
    this.direction = "advancing";
    const nextIndex = this.selectedIndex + 1;
    this.selectedIndex = (nextIndex + this.total) % this.total;
  }
  /** Selects the previous `calcite-tip` to display. */
  async previousTip() {
    this.direction = "retreating";
    const previousIndex = this.selectedIndex - 1;
    this.selectedIndex = (previousIndex + this.total) % this.total;
  }
  // --------------------------------------------------------------------------
  //
  //  Private Methods
  //
  // --------------------------------------------------------------------------
  setUpTips() {
    const tips = Array.from(this.el.querySelectorAll("calcite-tip"));
    this.total = tips.length;
    if (this.total === 0) {
      return;
    }
    const selectedTip = this.el.querySelector("calcite-tip[selected]");
    this.tips = tips;
    this.selectedIndex = selectedTip ? tips.indexOf(selectedTip) : 0;
    tips.forEach((tip) => {
      tip.nonDismissible = true;
    });
    this.showSelectedTip();
    this.updateGroupTitle();
  }
  showSelectedTip() {
    this.tips.forEach((tip, index) => {
      const isSelected = this.selectedIndex === index;
      tip.selected = isSelected;
      tip.hidden = !isSelected;
    });
  }
  updateGroupTitle() {
    const selectedTip = this.tips[this.selectedIndex];
    const tipParent = selectedTip.closest("calcite-tip-group");
    this.groupTitle = (tipParent === null || tipParent === void 0 ? void 0 : tipParent.groupTitle) || this.intlDefaultTitle || TEXT$1.defaultGroupTitle;
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderPagination() {
    const dir = dom.getElementDir(this.el);
    const { selectedIndex, tips, total, intlNext, intlPrevious, intlPaginationLabel } = this;
    const nextLabel = intlNext || TEXT$1.next;
    const previousLabel = intlPrevious || TEXT$1.previous;
    const paginationLabel = intlPaginationLabel || TEXT$1.defaultPaginationLabel;
    return tips.length > 1 ? (index.h("footer", { class: CSS$1.pagination }, index.h("calcite-action", { class: CSS$1.pagePrevious, icon: dir === "ltr" ? ICONS$1.chevronLeft : ICONS$1.chevronRight, onClick: this.previousClicked, scale: "m", text: previousLabel }), index.h("span", { class: CSS$1.pagePosition }, `${paginationLabel} ${selectedIndex + 1}/${total}`), index.h("calcite-action", { class: CSS$1.pageNext, icon: dir === "ltr" ? ICONS$1.chevronRight : ICONS$1.chevronLeft, onClick: this.nextClicked, scale: "m", text: nextLabel }))) : null;
  }
  render() {
    const { closed, direction, headingLevel, groupTitle, selectedIndex, intlClose, total } = this;
    const closeLabel = intlClose || TEXT$1.close;
    if (total === 0) {
      return null;
    }
    return (index.h("section", { "aria-hidden": dom.toAriaBoolean(closed), class: CSS$1.container, hidden: closed, onKeyDown: this.tipManagerKeyDownHandler, ref: this.storeContainerRef, tabIndex: 0 }, index.h("header", { class: CSS$1.header }, index.h(Heading.Heading, { class: CSS$1.heading, level: headingLevel || HEADING_LEVEL$1 }, groupTitle), index.h("calcite-action", { class: CSS$1.close, onClick: this.hideTipManager, scale: "m", text: closeLabel }, index.h("calcite-icon", { icon: ICONS$1.close, scale: "m" }))), index.h("div", { class: {
        [CSS$1.tipContainer]: true,
        [CSS$1.tipContainerAdvancing]: !closed && direction === "advancing",
        [CSS$1.tipContainerRetreating]: !closed && direction === "retreating"
      }, key: selectedIndex, tabIndex: 0 }, index.h("slot", null)), this.renderPagination()));
  }
  get el() { return index.getElement(this); }
  static get watchers() { return {
    "closed": ["closedChangeHandler"],
    "selectedIndex": ["selectedChangeHandler"]
  }; }
};
TipManager.style = tipManagerCss;

exports.calcite_tip = Tip;
exports.calcite_tip_group = TipGroup;
exports.calcite_tip_manager = TipManager;
