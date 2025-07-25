/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { r as registerInstance, c as createEvent, h, g as getElement, F as Fragment } from './index-7b536c47.js';
import { m as mutationObserverCallback, d as deselectRemovedItems, a as deselectSiblingItems, s as selectSiblings, h as handleFilter, b as getItemData, k as keyDownHandler, i as initialize, e as initializeObserver, f as cleanUpObserver, r as removeItem, l as calciteListItemChangeHandler, n as calciteInternalListItemValueChangeHandler, j as calciteListFocusOutHandler, o as setUpItems, p as setFocus, I as ICON_TYPES, L as List, q as SLOTS, t as CSS, H as HEADING_LEVEL, T as TEXT, u as ICONS, C as CSS$1, S as SLOTS$1 } from './resources-d7bce9b2.js';
import { c as createObserver } from './observers-513bffd3.js';
import { u as updateHostInteraction } from './interactive-fc7d4437.js';
import { g as getSlotted, t as toAriaBoolean } from './dom-bbdd8cab.js';
import { H as Heading, c as constrainHeadingLevel } from './Heading-9ad4465e.js';
import { c as connectConditionalSlotComponent, d as disconnectConditionalSlotComponent } from './conditionalSlot-c749c7e1.js';
import './array-bdb71f86.js';
import './debounce-247d9570.js';
import './resources-c8c07116.js';
import './guid-3c14f5cd.js';

const pickListCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{position:relative;box-sizing:border-box;display:flex;flex-shrink:0;flex-grow:1;flex-direction:column;align-items:stretch;background-color:transparent;font-size:var(--calcite-font-size--1);line-height:1rem;color:var(--calcite-ui-text-2)}:host *{box-sizing:border-box}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}:host([filter-enabled]) header{margin-block-end:0.25rem;display:flex;align-items:stretch;justify-content:flex-end;background-color:var(--calcite-ui-foreground-1);--tw-shadow:0 1px 0 var(--calcite-ui-border-3);--tw-shadow-colored:0 1px 0 var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)}:host([filter-enabled]) header.sticky-pos{position:sticky;inset-block-start:0px;z-index:1}calcite-filter{margin-block-end:0px}:host([loading][disabled]){min-block-size:2rem}";

const PickList = class {
  constructor(hostRef) {
    registerInstance(this, hostRef);
    this.calciteListChange = createEvent(this, "calciteListChange", 6);
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    /**
     * When `true`, an input appears at the top of the list that can be used by end users to filter items in the list.
     */
    this.filterEnabled = false;
    /**
     * When `true`, a busy indicator is displayed.
     */
    this.loading = false;
    /**
     * Similar to standard radio buttons and checkboxes.
     * When `true`, a user can select multiple `calcite-pick-list-item`s at a time.
     * When `false`, only a single `calcite-pick-list-item` can be selected at a time,
     * and a new selection will deselect previous selections.
     */
    this.multiple = false;
    /**
     * When `true` and single selection is enabled, the selection changes when navigating `calcite-pick-list-item`s via keyboard.
     */
    this.selectionFollowsFocus = false;
    // --------------------------------------------------------------------------
    //
    //  Private Properties
    //
    // --------------------------------------------------------------------------
    this.selectedValues = new Map();
    this.dataForFilter = [];
    this.lastSelectedItem = null;
    this.mutationObserver = createObserver("mutation", mutationObserverCallback.bind(this));
    this.setFilterEl = (el) => {
      this.filterEl = el;
    };
    this.deselectRemovedItems = deselectRemovedItems.bind(this);
    this.deselectSiblingItems = deselectSiblingItems.bind(this);
    this.selectSiblings = selectSiblings.bind(this);
    this.handleFilter = handleFilter.bind(this);
    this.getItemData = getItemData.bind(this);
    this.keyDownHandler = keyDownHandler.bind(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    initialize.call(this);
    initializeObserver.call(this);
  }
  disconnectedCallback() {
    cleanUpObserver.call(this);
  }
  componentDidRender() {
    updateHostInteraction(this);
  }
  calciteListItemRemoveHandler(event) {
    removeItem.call(this, event);
  }
  calciteListItemChangeHandler(event) {
    calciteListItemChangeHandler.call(this, event);
  }
  calciteInternalListItemPropsChangeHandler(event) {
    event.stopPropagation();
    this.setUpFilter();
  }
  calciteInternalListItemValueChangeHandler(event) {
    calciteInternalListItemValueChangeHandler.call(this, event);
    event.stopPropagation();
  }
  calciteListFocusOutHandler(event) {
    calciteListFocusOutHandler.call(this, event);
  }
  // --------------------------------------------------------------------------
  //
  //  Private Methods
  //
  // --------------------------------------------------------------------------
  setUpItems() {
    setUpItems.call(this, "calcite-pick-list-item");
  }
  setUpFilter() {
    if (this.filterEnabled) {
      this.dataForFilter = this.getItemData();
    }
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /** Returns the component's selected `calcite-pick-list-item`s. */
  async getSelectedItems() {
    return this.selectedValues;
  }
  /**
   * Sets focus on the component.
   *
   * @param focusId
   */
  async setFocus(focusId) {
    return setFocus.call(this, focusId);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  getIconType() {
    return this.multiple ? ICON_TYPES.square : ICON_TYPES.circle;
  }
  render() {
    return h(List, { onKeyDown: this.keyDownHandler, props: this });
  }
  get el() { return getElement(this); }
};
PickList.style = pickListCss;

const pickListGroupCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host{margin-block-end:0.25rem;box-sizing:border-box;display:block;background-color:transparent;font-size:var(--calcite-font-size--1);color:var(--calcite-ui-text-2)}:host *{box-sizing:border-box}:host(:last-child){margin-block-end:0px}.header{margin:0px;display:flex;align-content:space-between;align-items:center;fill:var(--calcite-ui-text-2);color:var(--calcite-ui-text-2)}.heading{margin:0px;padding:0px;font-weight:var(--calcite-font-weight-medium)}.header .heading{flex:1 1 auto;padding:0.5rem}.heading{margin-block:0.5rem;margin-inline:1rem;font-size:var(--calcite-font-size-0);line-height:1.25rem;font-size:var(--calcite-font-size--1);line-height:1.375;color:var(--calcite-ui-text-3)}.container--indented{margin-inline-start:1.5rem}";

const PickListGroup = class {
  constructor(hostRef) {
    registerInstance(this, hostRef);
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    connectConditionalSlotComponent(this);
  }
  disconnectedCallback() {
    disconnectConditionalSlotComponent(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    var _a;
    const { el, groupTitle, headingLevel } = this;
    const hasParentItem = getSlotted(el, SLOTS.parentItem) !== null;
    const sectionClasses = {
      [CSS.container]: true,
      [CSS.indented]: hasParentItem
    };
    const title = groupTitle;
    const parentLevel = (_a = el.closest("calcite-pick-list")) === null || _a === void 0 ? void 0 : _a.headingLevel;
    const relativeLevel = parentLevel ? constrainHeadingLevel(parentLevel + 1) : null;
    const level = headingLevel || relativeLevel || HEADING_LEVEL;
    return (h(Fragment, null, title ? (h(Heading, { class: CSS.heading, level: level }, title)) : null, h("slot", { name: SLOTS.parentItem }), h("section", { class: sectionClasses }, h("slot", null))));
  }
  get el() { return getElement(this); }
};
PickListGroup.style = pickListGroupCss;

const pickListItemCss = "@charset \"UTF-8\";@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{margin:0px;margin-block-end:1px;box-sizing:border-box;display:flex;align-items:stretch;background-color:var(--calcite-ui-foreground-1);font-size:var(--calcite-font-size--1);line-height:1rem;color:var(--calcite-ui-text-1);--tw-shadow:0 1px 0 var(--calcite-ui-border-3);--tw-shadow-colored:0 1px 0 var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow);transition:background-color var(--calcite-animation-timing);animation:calcite-fade-in var(--calcite-animation-timing)}:host *{box-sizing:border-box}.label{display:flex;flex:1 1 auto;cursor:pointer;align-items:center;background-color:transparent;outline-color:transparent}.label:focus{outline:2px solid var(--calcite-ui-brand);outline-offset:-2px}.label:hover{background-color:var(--calcite-ui-foreground-2)}:host([non-interactive]:hover){background-color:var(--calcite-ui-foreground-1)}:host([non-interactive]) .label,:host([non-interactive]) .icon{pointer-events:none}.icon{margin-block:0px;display:flex;cursor:pointer;align-items:center;padding:0.25rem;color:var(--calcite-ui-brand);flex:0 0 auto;line-height:0}.icon:hover{background-color:var(--calcite-ui-foreground-2)}.icon-dot{display:flex;inline-size:1.5rem;align-items:center;padding:0.5rem}.icon-dot:before{opacity:0;content:\"•\"}.icon calcite-icon{opacity:0}:host([selected]) .icon-dot:before,:host([selected]) .icon calcite-icon{transition:opacity var(--calcite-animation-timing);opacity:1}.text-container{pointer-events:none;display:flex;flex-direction:column;flex-wrap:nowrap;overflow:hidden;padding-block:0.5rem;padding-inline:0.75rem;font-size:var(--calcite-font-size--2);line-height:1.375;word-wrap:break-word;word-break:break-word}.title{font-weight:var(--calcite-font-weight-normal);color:var(--calcite-ui-text-1)}.description{margin-block-start:0.125rem;font-weight:var(--calcite-font-weight-normal);color:var(--calcite-ui-text-3)}.actions{margin:0px;display:flex;flex:0 1 auto;align-items:stretch;justify-content:flex-end}.actions--start~.label{padding-inline-start:0.25rem}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}";

const PickListItem = class {
  constructor(hostRef) {
    registerInstance(this, hostRef);
    this.calciteListItemChange = createEvent(this, "calciteListItemChange", 6);
    this.calciteListItemRemove = createEvent(this, "calciteListItemRemove", 7);
    this.calciteInternalListItemPropsChange = createEvent(this, "calciteInternalListItemPropsChange", 6);
    this.calciteInternalListItemValueChange = createEvent(this, "calciteInternalListItemValueChange", 6);
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    /**
     * When `false`, the component cannot be deselected by user interaction.
     */
    this.disableDeselect = false;
    /**
     * @internal
     */
    this.nonInteractive = false;
    /**
     * Determines the icon SVG symbol that will be shown. Options are `"circle"`, `"square"`, `"grip"` or `null`.
     *
     * @see [ICON_TYPES](https://github.com/Esri/calcite-components/blob/master/src/components/pick-list/resources.ts#L5)
     */
    this.icon = null;
    /**
     * When `true`, displays a remove action that removes the item from the list.
     */
    this.removable = false;
    /**
     * When `true`, selects an item. Toggles when an item is checked/unchecked.
     */
    this.selected = false;
    /**
     * When `removable` is `true`, the accessible name for the component's remove button.
     *
     * @default "Remove"
     */
    this.intlRemove = TEXT.remove;
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.pickListClickHandler = (event) => {
      if (this.disabled || (this.disableDeselect && this.selected) || this.nonInteractive) {
        return;
      }
      this.shiftPressed = event.shiftKey;
      this.selected = !this.selected;
    };
    this.pickListKeyDownHandler = (event) => {
      if (event.key === " ") {
        event.preventDefault();
        if ((this.disableDeselect && this.selected) || this.nonInteractive) {
          return;
        }
        this.selected = !this.selected;
      }
    };
    this.removeClickHandler = () => {
      this.calciteListItemRemove.emit();
    };
  }
  descriptionWatchHandler() {
    this.calciteInternalListItemPropsChange.emit();
  }
  labelWatchHandler() {
    this.calciteInternalListItemPropsChange.emit();
  }
  metadataWatchHandler() {
    this.calciteInternalListItemPropsChange.emit();
  }
  selectedWatchHandler() {
    this.calciteListItemChange.emit({
      item: this.el,
      value: this.value,
      selected: this.selected,
      shiftPressed: this.shiftPressed
    });
    this.shiftPressed = false;
  }
  valueWatchHandler(newValue, oldValue) {
    this.calciteInternalListItemValueChange.emit({ oldValue, newValue });
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    connectConditionalSlotComponent(this);
  }
  disconnectedCallback() {
    disconnectConditionalSlotComponent(this);
  }
  componentDidRender() {
    updateHostInteraction(this, this.el.closest("calcite-pick-list") ? "managed" : false);
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /**
   * Toggles the selection state. By default this won't trigger an event.
   * The first argument allows the value to be coerced, rather than swapping values.
   *
   * @param coerce
   */
  async toggleSelected(coerce) {
    this.selected = typeof coerce === "boolean" ? coerce : !this.selected;
  }
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
  renderIcon() {
    const { icon } = this;
    if (!icon) {
      return null;
    }
    return (h("span", { class: {
        [CSS$1.icon]: true,
        [CSS$1.iconDot]: icon === ICON_TYPES.circle
      }, onClick: this.pickListClickHandler }, icon === ICON_TYPES.square ? h("calcite-icon", { icon: ICONS.checked, scale: "s" }) : null));
  }
  renderRemoveAction() {
    return this.removable ? (h("calcite-action", { class: CSS$1.remove, icon: ICONS.remove, onCalciteActionClick: this.removeClickHandler, slot: SLOTS$1.actionsEnd, text: this.intlRemove })) : null;
  }
  renderActionsStart() {
    const { el } = this;
    const hasActionsStart = getSlotted(el, SLOTS$1.actionsStart);
    return hasActionsStart ? (h("div", { class: { [CSS$1.actions]: true, [CSS$1.actionsStart]: true } }, h("slot", { name: SLOTS$1.actionsStart }))) : null;
  }
  renderActionsEnd() {
    const { el, removable } = this;
    const hasActionsEnd = getSlotted(el, SLOTS$1.actionsEnd);
    return hasActionsEnd || removable ? (h("div", { class: { [CSS$1.actions]: true, [CSS$1.actionsEnd]: true } }, h("slot", { name: SLOTS$1.actionsEnd }), this.renderRemoveAction())) : null;
  }
  render() {
    const { description, label } = this;
    return (h(Fragment, null, this.renderIcon(), this.renderActionsStart(), h("label", { "aria-label": label, class: CSS$1.label, onClick: this.pickListClickHandler, onKeyDown: this.pickListKeyDownHandler, ref: (focusEl) => (this.focusEl = focusEl), tabIndex: 0 }, h("div", { "aria-checked": toAriaBoolean(this.selected), class: CSS$1.textContainer, role: "menuitemcheckbox" }, h("span", { class: CSS$1.title }, label), description ? h("span", { class: CSS$1.description }, description) : null)), this.renderActionsEnd()));
  }
  get el() { return getElement(this); }
  static get watchers() { return {
    "description": ["descriptionWatchHandler"],
    "label": ["labelWatchHandler"],
    "metadata": ["metadataWatchHandler"],
    "selected": ["selectedWatchHandler"],
    "value": ["valueWatchHandler"]
  }; }
};
PickListItem.style = pickListItemCss;

export { PickList as calcite_pick_list, PickListGroup as calcite_pick_list_group, PickListItem as calcite_pick_list_item };
