/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
'use strict';

Object.defineProperty(exports, '__esModule', { value: true });

const index = require('./index-b27b231c.js');
const sortable_esm = require('./sortable.esm-0a247e37.js');
const resources = require('./resources-c882f865.js');
const observers = require('./observers-664fbf90.js');
const interactive = require('./interactive-3d681fb9.js');
const guid = require('./guid-acbbb0e7.js');
const dom = require('./dom-2b919cb6.js');
const conditionalSlot = require('./conditionalSlot-f62c357a.js');
require('./array-e3a23914.js');
require('./debounce-3c20d30d.js');
require('./resources-3fd6da1b.js');

const CSS = {
  container: "container",
  handle: "handle"
};
var ICON_TYPES;
(function (ICON_TYPES) {
  ICON_TYPES["grip"] = "grip";
})(ICON_TYPES || (ICON_TYPES = {}));

function getScreenReaderText(item, status, valueList) {
  const { items, intlDragHandleIdle, intlDragHandleActive, intlDragHandleChange, intlDragHandleCommit } = valueList;
  const total = items.length;
  const position = resources.getItemIndex(valueList, item) + 1;
  if (status === "idle") {
    const idleText = intlDragHandleIdle
      ? replacePlaceholders(intlDragHandleIdle, item.label, position, total)
      : `${item.label}, press space and use arrow keys to reorder content. Current position ${position} of ${total}.`;
    return idleText;
  }
  else if (status === "active") {
    const activeText = intlDragHandleActive
      ? replacePlaceholders(intlDragHandleActive, item.label, position, total)
      : `Reordering ${item.label}, current position ${position} of ${total}.`;
    return activeText;
  }
  else if (status === "change") {
    const changeText = intlDragHandleChange
      ? replacePlaceholders(intlDragHandleChange, item.label, position, total)
      : `${item.label}, new position ${position} of ${total}. Press space to confirm.`;
    return changeText;
  }
  else {
    const commitText = intlDragHandleCommit
      ? replacePlaceholders(intlDragHandleCommit, item.label, position, total)
      : `${item.label}, current position ${position} of ${total}.`;
    return commitText;
  }
}
function getHandleAndItemElement(event) {
  const handle = event
    .composedPath()
    .find((item) => { var _a; return ((_a = item.dataset) === null || _a === void 0 ? void 0 : _a.jsHandle) !== undefined; });
  const item = event
    .composedPath()
    .find((item) => { var _a; return ((_a = item.tagName) === null || _a === void 0 ? void 0 : _a.toLowerCase()) === "calcite-value-list-item"; });
  return { handle, item };
}
function replacePlaceholders(text, label, position, total) {
  const replacePosition = text.replace("${position}", position.toString());
  const replaceLabel = replacePosition.replace("${item.label}", label);
  return replaceLabel.replace("${total}", total.toString());
}

const valueListCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{position:relative;box-sizing:border-box;display:flex;flex-shrink:0;flex-grow:0;flex-direction:column;align-items:stretch;background-color:transparent;font-size:var(--calcite-font-size--1);color:var(--calcite-ui-text-2)}:host *{box-sizing:border-box}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}calcite-value-list-item:last-of-type{--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)}:host([filter-enabled]) header{margin-block-end:0.25rem;display:flex;align-items:center;justify-content:flex-end;background-color:var(--calcite-ui-foreground-1);--tw-shadow:0 1px 0 var(--calcite-ui-border-3);--tw-shadow-colored:0 1px 0 var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)}:host([filter-enabled]) header.sticky-pos{position:sticky;inset-block-start:0px;z-index:300}calcite-filter{margin-block-end:1px}.assistive-text{position:absolute;inline-size:1px;block-size:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0, 0, 0, 0);white-space:nowrap;border-width:0}";

const ValueList = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteListChange = index.createEvent(this, "calciteListChange", 6);
    this.calciteListOrderChange = index.createEvent(this, "calciteListOrderChange", 6);
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
     * When `true`, `calcite-value-list-item`s are sortable via a draggable button.
     */
    this.dragEnabled = false;
    /**
     * When `true`, an input appears at the top of the component that can be used by end users to filter list items.
     */
    this.filterEnabled = false;
    /**
     * When `true`, a busy indicator is displayed.
     */
    this.loading = false;
    /**
     * Similar to standard radio buttons and checkboxes.
     * When `true`, a user can select multiple `calcite-value-list-item`s at a time.
     * When `false`, only a single `calcite-value-list-item` can be selected at a time,
     * and a new selection will deselect previous selections.
     */
    this.multiple = false;
    /**
     * When `true` and single-selection is enabled, the selection changes when navigating `calcite-value-list-item`s via keyboard.
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
    this.mutationObserver = observers.createObserver("mutation", resources.mutationObserverCallback.bind(this));
    this.setFilterEl = (el) => {
      this.filterEl = el;
    };
    this.deselectRemovedItems = resources.deselectRemovedItems.bind(this);
    this.deselectSiblingItems = resources.deselectSiblingItems.bind(this);
    this.selectSiblings = resources.selectSiblings.bind(this);
    this.handleFilter = resources.handleFilter.bind(this);
    this.getItemData = resources.getItemData.bind(this);
    this.keyDownHandler = (event) => {
      if (event.defaultPrevented) {
        return;
      }
      const { handle, item } = getHandleAndItemElement(event);
      if (handle && !item.handleActivated && event.key === " ") {
        this.updateScreenReaderText(getScreenReaderText(item, "commit", this));
      }
      if (!handle || !item.handleActivated) {
        resources.keyDownHandler.call(this, event);
        return;
      }
      const { items } = this;
      if (event.key === " ") {
        this.updateScreenReaderText(getScreenReaderText(item, "active", this));
      }
      if ((event.key !== "ArrowUp" && event.key !== "ArrowDown") || items.length <= 1) {
        return;
      }
      event.preventDefault();
      const { el } = this;
      const nextIndex = resources.moveItemIndex(this, item, event.key === "ArrowUp" ? "up" : "down");
      if (nextIndex === items.length - 1) {
        el.appendChild(item);
      }
      else {
        const itemAtNextIndex = el.children[nextIndex];
        const insertionReferenceItem = itemAtNextIndex === item.nextElementSibling
          ? itemAtNextIndex.nextElementSibling
          : itemAtNextIndex;
        el.insertBefore(item, insertionReferenceItem);
      }
      this.items = this.getItems();
      this.calciteListOrderChange.emit(this.items.map(({ value }) => value));
      requestAnimationFrame(() => handle === null || handle === void 0 ? void 0 : handle.focus());
      item.handleActivated = true;
      this.updateHandleAriaLabel(handle, getScreenReaderText(item, "change", this));
    };
    this.storeAssistiveEl = (el) => {
      this.assistiveTextEl = el;
    };
    this.handleFocusIn = (event) => {
      const { handle, item } = getHandleAndItemElement(event);
      if (!(item === null || item === void 0 ? void 0 : item.handleActivated) && item && handle) {
        this.updateHandleAriaLabel(handle, getScreenReaderText(item, "idle", this));
      }
    };
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    resources.initialize.call(this);
    resources.initializeObserver.call(this);
  }
  componentDidLoad() {
    this.setUpDragAndDrop();
  }
  componentDidRender() {
    interactive.updateHostInteraction(this);
  }
  disconnectedCallback() {
    resources.cleanUpObserver.call(this);
    this.cleanUpDragAndDrop();
  }
  calciteListFocusOutHandler(event) {
    resources.calciteListFocusOutHandler.call(this, event);
  }
  calciteListItemRemoveHandler(event) {
    resources.removeItem.call(this, event);
  }
  calciteListItemChangeHandler(event) {
    resources.calciteListItemChangeHandler.call(this, event);
  }
  calciteInternalListItemPropsChangeHandler(event) {
    event.stopPropagation();
    this.setUpFilter();
  }
  calciteInternalListItemValueChangeHandler(event) {
    resources.calciteInternalListItemValueChangeHandler.call(this, event);
    event.stopPropagation();
  }
  // --------------------------------------------------------------------------
  //
  //  Private Methods
  //
  // --------------------------------------------------------------------------
  getItems() {
    return Array.from(this.el.querySelectorAll("calcite-value-list-item"));
  }
  setUpItems() {
    resources.setUpItems.call(this, "calcite-value-list-item");
  }
  setUpFilter() {
    if (this.filterEnabled) {
      this.dataForFilter = this.getItemData();
    }
  }
  setUpDragAndDrop() {
    this.cleanUpDragAndDrop();
    if (!this.dragEnabled) {
      return;
    }
    this.sortable = sortable_esm.Sortable.create(this.el, {
      dataIdAttr: "id",
      handle: `.${CSS.handle}`,
      draggable: "calcite-value-list-item",
      group: this.group,
      onSort: () => {
        this.items = Array.from(this.el.querySelectorAll("calcite-value-list-item"));
        const values = this.items.map((item) => item.value);
        this.calciteListOrderChange.emit(values);
      }
    });
  }
  cleanUpDragAndDrop() {
    var _a;
    (_a = this.sortable) === null || _a === void 0 ? void 0 : _a.destroy();
    this.sortable = null;
  }
  handleBlur() {
    if (this.dragEnabled) {
      this.updateScreenReaderText("");
    }
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /** Returns the currently selected items */
  async getSelectedItems() {
    return this.selectedValues;
  }
  /**
   * Sets focus on the component.
   *
   * @param focusId
   */
  async setFocus(focusId) {
    return resources.setFocus.call(this, focusId);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  getIconType() {
    let type = null;
    if (this.dragEnabled) {
      type = ICON_TYPES.grip;
    }
    return type;
  }
  updateScreenReaderText(text) {
    this.assistiveTextEl.textContent = text;
  }
  updateHandleAriaLabel(handleElement, text) {
    handleElement.ariaLabel = text;
  }
  render() {
    return (index.h(resources.List, { onBlur: this.handleBlur, onFocusin: this.handleFocusIn, onKeyDown: this.keyDownHandler, props: this }));
  }
  get el() { return index.getElement(this); }
};
ValueList.style = valueListCss;

const ICONS = {
  drag: "drag"
};
const SLOTS = {
  actionsEnd: "actions-end",
  actionsStart: "actions-start"
};

const valueListItemCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{margin-block-end:1px;box-sizing:border-box;display:flex;background-color:var(--calcite-ui-foreground-1);font-size:var(--calcite-font-size--1);color:var(--calcite-ui-text-2);--tw-shadow:0 1px 0 var(--calcite-ui-border-3);--tw-shadow-colored:0 1px 0 var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow);transition:background-color var(--calcite-animation-timing), box-shadow var(--calcite-animation-timing)}:host *{box-sizing:border-box}calcite-pick-list-item{position:relative;margin:0px;flex-grow:1;--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)}:host([active]),:host([selected]){--tw-shadow:0 0 0 1px var(--calcite-ui-brand);--tw-shadow-colored:0 0 0 1px var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow)}.handle{display:flex;cursor:move;align-items:center;justify-content:center;border-style:none;background-color:transparent;padding-block:0px;padding-inline:0.25rem;color:var(--calcite-ui-border-input);outline-color:transparent}.handle:hover{background-color:var(--calcite-ui-foreground-2);color:var(--calcite-ui-text-1)}.handle:focus{outline:2px solid var(--calcite-ui-brand);outline-offset:-2px}.handle--activated{background-color:var(--calcite-ui-foreground-3);color:var(--calcite-ui-text-1)}.handle calcite-icon{color:inherit}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}";

const ValueListItem = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteListItemRemove = index.createEvent(this, "calciteListItemRemove", 7);
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    /**
     * @internal
     */
    this.disableDeselect = false;
    /**
     * When `true`, prevents the content of the component from user interaction.
     */
    this.nonInteractive = false;
    /**
     * @internal
     */
    this.handleActivated = false;
    /**
     * Determines the icon SVG symbol that will be shown. Options are circle, square, grip or null.
     *
     * @see [ICON_TYPES](https://github.com/Esri/calcite-components/blob/master/src/components/pick-list/resources.ts#L5)
     */
    this.icon = null;
    /**
     * When `true`, adds an action to remove the component.
     */
    this.removable = false;
    /**
     * When `true`, the component is selected.
     */
    this.selected = false;
    this.pickListItem = null;
    this.guid = `calcite-value-list-item-${guid.guid()}`;
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.getPickListRef = (el) => (this.pickListItem = el);
    this.handleKeyDown = (event) => {
      if (event.key === " ") {
        this.handleActivated = !this.handleActivated;
      }
    };
    this.handleBlur = () => {
      this.handleActivated = false;
    };
    this.handleSelectChange = (event) => {
      this.selected = event.detail.selected;
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
  componentDidRender() {
    interactive.updateHostInteraction(this, this.el.closest("calcite-value-list") ? "managed" : false);
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /**
   * Toggle the selection state. By default this won't trigger an event.
   * The first argument allows the value to be coerced, rather than swapping values.
   *
   * @param coerce
   */
  async toggleSelected(coerce) {
    this.pickListItem.toggleSelected(coerce);
  }
  /** Set focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.pickListItem) === null || _a === void 0 ? void 0 : _a.setFocus();
  }
  calciteListItemChangeHandler(event) {
    // adjust item payload from wrapped item before bubbling
    event.detail.item = this.el;
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderActionsEnd() {
    const { el } = this;
    const hasActionsEnd = dom.getSlotted(el, SLOTS.actionsEnd);
    return hasActionsEnd ? (index.h("slot", { name: SLOTS.actionsEnd, slot: resources.SLOTS.actionsEnd })) : null;
  }
  renderActionsStart() {
    const { el } = this;
    const hasActionsStart = dom.getSlotted(el, SLOTS.actionsStart);
    return hasActionsStart ? (index.h("slot", { name: SLOTS.actionsStart, slot: resources.SLOTS.actionsStart })) : null;
  }
  renderHandle() {
    const { icon } = this;
    if (icon === resources.ICON_TYPES.grip) {
      return (index.h("span", { class: {
          [resources.CSS.handle]: true,
          [resources.CSS.handleActivated]: this.handleActivated
        }, "data-js-handle": true, onBlur: this.handleBlur, onKeyDown: this.handleKeyDown, role: "button", tabindex: "0" }, index.h("calcite-icon", { icon: ICONS.drag, scale: "s" })));
    }
  }
  render() {
    return (index.h(index.Host, { id: this.el.id || this.guid }, this.renderHandle(), index.h("calcite-pick-list-item", { description: this.description, disableDeselect: this.disableDeselect, disabled: this.disabled, label: this.label, metadata: this.metadata, nonInteractive: this.nonInteractive, onCalciteListItemChange: this.handleSelectChange, ref: this.getPickListRef, removable: this.removable, selected: this.selected, value: this.value }, this.renderActionsStart(), this.renderActionsEnd())));
  }
  get el() { return index.getElement(this); }
};
ValueListItem.style = valueListItemCss;

exports.calcite_value_list = ValueList;
exports.calcite_value_list_item = ValueListItem;
