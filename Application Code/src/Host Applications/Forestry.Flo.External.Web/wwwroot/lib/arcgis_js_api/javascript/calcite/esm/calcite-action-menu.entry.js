/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { r as registerInstance, c as createEvent, h, F as Fragment, g as getElement } from './index-7b536c47.js';
import { C as CSS, I as ICONS, S as SLOTS } from './resources-f878b23a.js';
import { t as toAriaBoolean, i as isPrimaryPointerButton, f as focusElement } from './dom-bbdd8cab.js';
import { g as getRoundRobinIndex } from './array-bdb71f86.js';
import { g as guid } from './guid-3c14f5cd.js';
import { i as isActivationKey } from './key-41222930.js';
import './resources-c8c07116.js';

const actionMenuCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host{box-sizing:border-box;display:flex;flex-direction:column;background-color:var(--calcite-ui-foreground-1);font-size:var(--calcite-font-size-1);color:var(--calcite-ui-text-2)}.menu ::slotted(calcite-action){margin:0.125rem;display:flex;outline-color:transparent}.menu ::slotted(calcite-action[active]){outline:2px solid var(--calcite-ui-brand);outline-offset:0px}.default-trigger{position:relative;block-size:100%;flex:0 1 auto;align-self:stretch}slot[name=trigger]::slotted(calcite-action),calcite-action::slotted([slot=trigger]){position:relative;block-size:100%;flex:0 1 auto;align-self:stretch}.menu{flex-direction:column;flex-wrap:nowrap;outline:2px solid transparent;outline-offset:2px}";

const SUPPORTED_MENU_NAV_KEYS = ["ArrowUp", "ArrowDown", "End", "Home"];
const ActionMenu = class {
  constructor(hostRef) {
    registerInstance(this, hostRef);
    this.calciteActionMenuOpenChange = createEvent(this, "calciteActionMenuOpenChange", 6);
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, the component is expanded.
     */
    this.expanded = false;
    /**
     * When `true`, the component is open.
     */
    this.open = false;
    /**
     * Determines the type of positioning to use for the overlaid content.
     *
     * Using `"absolute"` will work for most cases. The component will be positioned inside of overflowing parent containers and will affect the container's layout.
     * `"fixed"` should be used to escape an overflowing parent container, or when the reference element's `position` CSS property is `"fixed"`.
     *
     */
    this.overlayPositioning = "absolute";
    /**
     * Determines where the component will be positioned relative to the `referenceElement`.
     *
     * @see [LogicalPlacement](https://github.com/Esri/calcite-components/blob/master/src/utils/floating-ui.ts#L25)
     */
    this.placement = "auto";
    this.actionElements = [];
    this.guid = `calcite-action-menu-${guid()}`;
    this.menuId = `${this.guid}-menu`;
    this.menuButtonId = `${this.guid}-menu-button`;
    this.activeMenuItemIndex = -1;
    // --------------------------------------------------------------------------
    //
    //  Component Methods
    //
    // --------------------------------------------------------------------------
    this.connectMenuButtonEl = () => {
      const { menuButtonId, menuId, open, label } = this;
      const menuButtonEl = this.slottedMenuButtonEl || this.defaultMenuButtonEl;
      if (this.menuButtonEl === menuButtonEl) {
        return;
      }
      this.disconnectMenuButtonEl();
      this.menuButtonEl = menuButtonEl;
      this.setTooltipReferenceElement();
      if (!menuButtonEl) {
        return;
      }
      menuButtonEl.active = open;
      menuButtonEl.setAttribute("aria-controls", menuId);
      menuButtonEl.setAttribute("aria-expanded", toAriaBoolean(open));
      menuButtonEl.setAttribute("aria-haspopup", "true");
      if (!menuButtonEl.id) {
        menuButtonEl.id = menuButtonId;
      }
      if (!menuButtonEl.label) {
        menuButtonEl.label = label;
      }
      if (!menuButtonEl.text) {
        menuButtonEl.text = label;
      }
      menuButtonEl.addEventListener("pointerdown", this.menuButtonClick);
      menuButtonEl.addEventListener("keydown", this.menuButtonKeyDown);
    };
    this.disconnectMenuButtonEl = () => {
      const { menuButtonEl } = this;
      if (!menuButtonEl) {
        return;
      }
      menuButtonEl.removeEventListener("pointerdown", this.menuButtonClick);
      menuButtonEl.removeEventListener("keydown", this.menuButtonKeyDown);
    };
    this.setMenuButtonEl = (event) => {
      const actions = event.target
        .assignedElements({
        flatten: true
      })
        .filter((el) => el === null || el === void 0 ? void 0 : el.matches("calcite-action"));
      this.slottedMenuButtonEl = actions[0];
      this.connectMenuButtonEl();
    };
    this.setDefaultMenuButtonEl = (el) => {
      this.defaultMenuButtonEl = el;
      this.connectMenuButtonEl();
    };
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.handleCalciteActionClick = () => {
      this.open = false;
      this.setFocus();
    };
    this.menuButtonClick = (event) => {
      if (!isPrimaryPointerButton(event)) {
        return;
      }
      this.toggleOpen();
    };
    this.updateTooltip = (event) => {
      const tooltips = event.target
        .assignedElements({
        flatten: true
      })
        .filter((el) => el === null || el === void 0 ? void 0 : el.matches("calcite-tooltip"));
      this.tooltipEl = tooltips[0];
      this.setTooltipReferenceElement();
    };
    this.setTooltipReferenceElement = () => {
      const { tooltipEl, expanded, menuButtonEl, open } = this;
      if (tooltipEl) {
        tooltipEl.referenceElement = !expanded && !open ? menuButtonEl : null;
      }
    };
    this.updateAction = (action, index) => {
      const { guid, activeMenuItemIndex } = this;
      const id = `${guid}-action-${index}`;
      action.tabIndex = -1;
      action.setAttribute("role", "menuitem");
      if (!action.id) {
        action.id = id;
      }
      action.active = index === activeMenuItemIndex;
    };
    this.updateActions = (actions) => {
      actions === null || actions === void 0 ? void 0 : actions.forEach(this.updateAction);
    };
    this.handleDefaultSlotChange = (event) => {
      const actions = event.target
        .assignedElements({
        flatten: true
      })
        .filter((el) => el === null || el === void 0 ? void 0 : el.matches("calcite-action"));
      this.actionElements = actions;
    };
    this.menuButtonKeyDown = (event) => {
      const { key } = event;
      const { actionElements, activeMenuItemIndex, open } = this;
      if (!actionElements.length) {
        return;
      }
      if (isActivationKey(key)) {
        event.preventDefault();
        if (!open) {
          this.toggleOpen();
          return;
        }
        const action = actionElements[activeMenuItemIndex];
        action ? action.click() : this.toggleOpen(false);
      }
      if (key === "Tab") {
        this.open = false;
        return;
      }
      if (key === "Escape") {
        this.toggleOpen(false);
        event.preventDefault();
        return;
      }
      this.handleActionNavigation(event, key, actionElements);
    };
    this.handleActionNavigation = (event, key, actions) => {
      if (!this.isValidKey(key, SUPPORTED_MENU_NAV_KEYS)) {
        return;
      }
      event.preventDefault();
      if (!this.open) {
        this.toggleOpen();
        if (key === "Home" || key === "ArrowDown") {
          this.activeMenuItemIndex = 0;
        }
        if (key === "End" || key === "ArrowUp") {
          this.activeMenuItemIndex = actions.length - 1;
        }
        return;
      }
      if (key === "Home") {
        this.activeMenuItemIndex = 0;
      }
      if (key === "End") {
        this.activeMenuItemIndex = actions.length - 1;
      }
      const currentIndex = this.activeMenuItemIndex;
      if (key === "ArrowUp") {
        this.activeMenuItemIndex = getRoundRobinIndex(Math.max(currentIndex - 1, -1), actions.length);
      }
      if (key === "ArrowDown") {
        this.activeMenuItemIndex = getRoundRobinIndex(currentIndex + 1, actions.length);
      }
    };
    this.toggleOpenEnd = () => {
      this.setFocus();
      this.el.removeEventListener("calcitePopoverOpen", this.toggleOpenEnd);
    };
    this.toggleOpen = (value = !this.open) => {
      this.el.addEventListener("calcitePopoverOpen", this.toggleOpenEnd);
      this.open = value;
    };
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  disconnectedCallback() {
    this.disconnectMenuButtonEl();
  }
  expandedHandler() {
    this.open = false;
    this.setTooltipReferenceElement();
  }
  openHandler(open) {
    this.activeMenuItemIndex = this.open ? 0 : -1;
    if (this.menuButtonEl) {
      this.menuButtonEl.active = open;
    }
    this.calciteActionMenuOpenChange.emit(open);
    this.setTooltipReferenceElement();
  }
  closeCalciteActionMenuOnClick(event) {
    if (!isPrimaryPointerButton(event)) {
      return;
    }
    const composedPath = event.composedPath();
    if (composedPath.includes(this.el)) {
      return;
    }
    this.open = false;
  }
  activeMenuItemIndexHandler() {
    this.updateActions(this.actionElements);
  }
  // --------------------------------------------------------------------------
  //
  //  Methods
  //
  // --------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    focusElement(this.menuButtonEl);
  }
  renderMenuButton() {
    const { label, scale, expanded } = this;
    const menuButtonSlot = (h("slot", { name: SLOTS.trigger, onSlotchange: this.setMenuButtonEl }, h("calcite-action", { class: CSS.defaultTrigger, icon: ICONS.menu, ref: this.setDefaultMenuButtonEl, scale: scale, text: label, textEnabled: expanded })));
    return menuButtonSlot;
  }
  renderMenuItems() {
    const { actionElements, activeMenuItemIndex, open, menuId, menuButtonEl, label, placement, overlayPositioning, flipPlacements } = this;
    const activeAction = actionElements[activeMenuItemIndex];
    const activeDescendantId = (activeAction === null || activeAction === void 0 ? void 0 : activeAction.id) || null;
    return (h("calcite-popover", { disablePointer: true, flipPlacements: flipPlacements, label: label, offsetDistance: 0, open: open, overlayPositioning: overlayPositioning, placement: placement, referenceElement: menuButtonEl }, h("div", { "aria-activedescendant": activeDescendantId, "aria-labelledby": menuButtonEl === null || menuButtonEl === void 0 ? void 0 : menuButtonEl.id, class: CSS.menu, id: menuId, onClick: this.handleCalciteActionClick, role: "menu", tabIndex: -1 }, h("slot", { onSlotchange: this.handleDefaultSlotChange }))));
  }
  render() {
    return (h(Fragment, null, this.renderMenuButton(), this.renderMenuItems(), h("slot", { name: SLOTS.tooltip, onSlotchange: this.updateTooltip })));
  }
  isValidKey(key, supportedKeys) {
    return !!supportedKeys.find((k) => k === key);
  }
  get el() { return getElement(this); }
  static get watchers() { return {
    "expanded": ["expandedHandler"],
    "open": ["openHandler"],
    "activeMenuItemIndex": ["activeMenuItemIndexHandler"]
  }; }
};
ActionMenu.style = actionMenuCss;

export { ActionMenu as calcite_action_menu };
