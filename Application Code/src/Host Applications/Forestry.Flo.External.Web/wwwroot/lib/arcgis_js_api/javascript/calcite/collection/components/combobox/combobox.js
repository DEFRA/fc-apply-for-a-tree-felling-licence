/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { filter } from "../../utils/filter";
import { debounce } from "lodash-es";
import { FloatingCSS, connectFloatingUI, disconnectFloatingUI, defaultMenuPlacement, filterComputedPlacements, reposition, updateAfterClose } from "../../utils/floating-ui";
import { guid } from "../../utils/guid";
import { ComboboxChildSelector, ComboboxItem, ComboboxItemGroup, TEXT } from "./resources";
import { getItemAncestors, getItemChildren, hasActiveChildren } from "./utils";
import { connectLabel, disconnectLabel, getLabelText } from "../../utils/label";
import { afterConnectDefaultValueSet, connectForm, disconnectForm, HiddenFormInputSlot, submitForm } from "../../utils/form";
import { createObserver } from "../../utils/observers";
import { updateHostInteraction } from "../../utils/interactive";
import { isPrimaryPointerButton, toAriaBoolean } from "../../utils/dom";
import { connectOpenCloseComponent, disconnectOpenCloseComponent } from "../../utils/openCloseComponent";
const isGroup = (el) => el.tagName === ComboboxItemGroup;
const itemUidPrefix = "combobox-item-";
const chipUidPrefix = "combobox-chip-";
const labelUidPrefix = "combobox-label-";
const listboxUidPrefix = "combobox-listbox-";
const inputUidPrefix = "combobox-input-";
/**
 * @slot - A slot for adding `calcite-combobox-item`s.
 */
export class Combobox {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Public Properties
    //
    //--------------------------------------------------------------------------
    /**
     * When `true`, displays and positions the component.
     *
     * @deprecated use `open` instead.
     */
    this.active = false;
    /**When `true`, displays and positions the component. */
    this.open = false;
    /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
    this.disabled = false;
    /** Specifies the maximum number of `calcite-combobox-item`s (including nested children) to display before displaying a scrollbar. */
    this.maxItems = 0;
    /**
     * Determines the type of positioning to use for the overlaid content.
     *
     * Using `"absolute"` will work for most cases. The component will be positioned inside of overflowing parent containers and will affect the container's layout.
     *
     * `"fixed"` should be used to escape an overflowing parent container, or when the reference element's `position` CSS property is `"fixed"`.
     *
     */
    this.overlayPositioning = "absolute";
    /**
     * When `true`, the component must have a value in order for the form to submit.
     *
     * @internal
     */
    this.required = false;
    /**
     * specify the selection mode
     * - multiple: allow any number of selected items (default)
     * - single: only one selection)
     * - ancestors: like multiple, but show ancestors of selected items as selected, only deepest children shown in chips
     */
    this.selectionMode = "multi";
    /** Specifies the size of the component. */
    this.scale = "m";
    /** The component's value(s) from the selected `calcite-combobox-item`(s). */
    this.value = null;
    /**
     * Accessible name for the component's remove tag when a `calcite-combobox-item` is selected.
     *
     * @default "Remove tag"
     */
    this.intlRemoveTag = TEXT.removeTag;
    //--------------------------------------------------------------------------
    //
    //  Private State/Props
    //
    //--------------------------------------------------------------------------
    this.placement = defaultMenuPlacement;
    this.internalValueChangeFlag = false;
    this.items = [];
    this.groupItems = [];
    this.selectedItems = [];
    this.visibleItems = [];
    this.activeItemIndex = -1;
    this.activeChipIndex = -1;
    this.activeDescendant = "";
    this.text = "";
    this.textInput = null;
    this.mutationObserver = createObserver("mutation", () => this.updateItems());
    this.resizeObserver = createObserver("resize", () => this.setMaxScrollerHeight());
    this.guid = guid();
    this.inputHeight = 0;
    this.ignoreSelectedEventsFlag = false;
    this.openTransitionProp = "opacity";
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.setFilteredPlacements = () => {
      const { el, flipPlacements } = this;
      this.filteredFlipPlacements = flipPlacements
        ? filterComputedPlacements(flipPlacements, el)
        : null;
    };
    this.getValue = () => {
      const items = this.selectedItems.map((item) => { var _a; return (_a = item === null || item === void 0 ? void 0 : item.value) === null || _a === void 0 ? void 0 : _a.toString(); });
      return (items === null || items === void 0 ? void 0 : items.length) ? (items.length > 1 ? items : items[0]) : "";
    };
    this.onLabelClick = () => {
      this.setFocus();
    };
    this.keydownHandler = (event) => {
      const { key } = event;
      switch (key) {
        case "Tab":
          this.activeChipIndex = -1;
          this.activeItemIndex = -1;
          if (this.allowCustomValues && this.text) {
            this.addCustomChip(this.text, true);
            event.preventDefault();
          }
          else if (this.open) {
            this.open = false;
            event.preventDefault();
          }
          break;
        case "ArrowLeft":
          this.previousChip();
          event.preventDefault();
          break;
        case "ArrowRight":
          this.nextChip();
          event.preventDefault();
          break;
        case "ArrowUp":
          event.preventDefault();
          this.shiftActiveItemIndex(-1);
          if (!this.comboboxInViewport()) {
            this.el.scrollIntoView();
          }
          break;
        case "ArrowDown":
          event.preventDefault();
          if (!this.open) {
            this.open = true;
          }
          this.shiftActiveItemIndex(1);
          if (!this.comboboxInViewport()) {
            this.el.scrollIntoView();
          }
          break;
        case " ":
          if (!this.textInput.value) {
            event.preventDefault();
            this.open = true;
            this.shiftActiveItemIndex(1);
          }
          break;
        case "Home":
          if (!this.open) {
            return;
          }
          event.preventDefault();
          this.updateActiveItemIndex(0);
          this.scrollToActiveItem();
          if (!this.comboboxInViewport()) {
            this.el.scrollIntoView();
          }
          break;
        case "End":
          if (!this.open) {
            return;
          }
          event.preventDefault();
          this.updateActiveItemIndex(this.visibleItems.length - 1);
          this.scrollToActiveItem();
          if (!this.comboboxInViewport()) {
            this.el.scrollIntoView();
          }
          break;
        case "Escape":
          this.open = false;
          event.preventDefault();
          break;
        case "Enter":
          if (this.activeItemIndex > -1) {
            this.toggleSelection(this.visibleItems[this.activeItemIndex]);
            event.preventDefault();
          }
          else if (this.activeChipIndex > -1) {
            this.removeActiveChip();
            event.preventDefault();
          }
          else if (this.allowCustomValues && this.text) {
            this.addCustomChip(this.text, true);
            event.preventDefault();
          }
          else if (!event.defaultPrevented) {
            if (submitForm(this)) {
              event.preventDefault();
            }
          }
          break;
        case "Delete":
        case "Backspace":
          if (this.activeChipIndex > -1) {
            event.preventDefault();
            this.removeActiveChip();
          }
          else if (!this.text && this.isMulti()) {
            event.preventDefault();
            this.removeLastChip();
          }
          break;
      }
    };
    this.toggleCloseEnd = () => {
      this.open = false;
      this.el.removeEventListener("calciteComboboxClose", this.toggleCloseEnd);
    };
    this.toggleOpenEnd = () => {
      this.open = false;
      this.el.removeEventListener("calciteComboboxOpen", this.toggleOpenEnd);
    };
    this.setMaxScrollerHeight = async () => {
      const { listContainerEl, open, referenceEl } = this;
      if (!listContainerEl || !open) {
        return;
      }
      await this.reposition(true);
      const maxScrollerHeight = this.getMaxScrollerHeight();
      listContainerEl.style.maxHeight = maxScrollerHeight > 0 ? `${maxScrollerHeight}px` : "";
      listContainerEl.style.minWidth = `${referenceEl.clientWidth}px`;
      await this.reposition(true);
    };
    this.calciteChipDismissHandler = (event, comboboxItem) => {
      this.open = false;
      const selection = this.items.find((item) => item === comboboxItem);
      if (selection) {
        this.toggleSelection(selection, false);
      }
      this.calciteComboboxChipDismiss.emit(event.detail);
    };
    this.clickHandler = (event) => {
      if (event.composedPath().some((node) => node.tagName === "CALCITE-CHIP")) {
        return;
      }
      this.open = !this.open;
      this.updateActiveItemIndex(0);
      this.setFocus();
    };
    this.setInactiveIfNotContained = (event) => {
      const composedPath = event.composedPath();
      if (!this.open || composedPath.includes(this.el) || composedPath.includes(this.referenceEl)) {
        return;
      }
      if (this.allowCustomValues && this.text.trim().length) {
        this.addCustomChip(this.text);
      }
      if (this.selectionMode === "single") {
        if (this.textInput) {
          this.textInput.value = "";
        }
        this.text = "";
        this.filterItems("");
        this.updateActiveItemIndex(-1);
      }
      this.open = false;
    };
    this.setFloatingEl = (el) => {
      this.floatingEl = el;
      connectFloatingUI(this, this.referenceEl, this.floatingEl);
    };
    this.setContainerEl = (el) => {
      this.resizeObserver.observe(el);
      this.listContainerEl = el;
      this.transitionEl = el;
      connectOpenCloseComponent(this);
    };
    this.setReferenceEl = (el) => {
      this.referenceEl = el;
      connectFloatingUI(this, this.referenceEl, this.floatingEl);
    };
    this.inputHandler = (event) => {
      const value = event.target.value;
      this.text = value;
      this.filterItems(value);
      if (value) {
        this.activeChipIndex = -1;
      }
    };
    this.filterItems = (() => {
      const find = (item, filteredData) => item &&
        filteredData.some(({ label, value }) => {
          if (isGroup(item)) {
            return value === item.label;
          }
          return (value === item.textLabel ||
            value === item.value ||
            label === item.textLabel ||
            label === item.value);
        });
      return debounce((text) => {
        const filteredData = filter(this.data, text);
        const items = this.getCombinedItems();
        items.forEach((item) => {
          const hidden = !find(item, filteredData);
          item.hidden = hidden;
          const [parent, grandparent] = item.ancestors;
          if (find(parent, filteredData) || find(grandparent, filteredData)) {
            item.hidden = false;
          }
          if (!hidden) {
            item.ancestors.forEach((ancestor) => (ancestor.hidden = false));
          }
        });
        this.visibleItems = this.getVisibleItems();
        this.calciteComboboxFilterChange.emit({ visibleItems: [...this.visibleItems], text: text });
      }, 100);
    })();
    this.internalCalciteLookupChangeEvent = () => {
      this.calciteLookupChange.emit(this.selectedItems);
    };
    this.emitCalciteLookupChange = debounce(this.internalCalciteLookupChangeEvent, 0);
    this.internalComboboxChangeEvent = () => {
      const { selectedItems } = this;
      this.calciteComboboxChange.emit({ selectedItems });
    };
    this.emitComboboxChange = debounce(this.internalComboboxChangeEvent, 0);
    this.updateItems = () => {
      this.items = this.getItems();
      this.groupItems = this.getGroupItems();
      this.data = this.getData();
      this.selectedItems = this.getSelectedItems();
      this.visibleItems = this.getVisibleItems();
      this.needsIcon = this.getNeedsIcon();
      if (!this.allowCustomValues) {
        this.setMaxScrollerHeight();
      }
    };
    this.scrollToActiveItem = () => {
      const activeItem = this.visibleItems[this.activeItemIndex];
      const height = this.calculateSingleItemHeight(activeItem);
      const { offsetHeight, scrollTop } = this.listContainerEl;
      if (offsetHeight + scrollTop < activeItem.offsetTop + height) {
        this.listContainerEl.scrollTop = activeItem.offsetTop - offsetHeight + height;
      }
      else if (activeItem.offsetTop < scrollTop) {
        this.listContainerEl.scrollTop = activeItem.offsetTop;
      }
    };
    this.comboboxFocusHandler = () => {
      var _a;
      (_a = this.textInput) === null || _a === void 0 ? void 0 : _a.focus();
    };
    this.comboboxBlurHandler = (event) => {
      this.setInactiveIfNotContained(event);
    };
  }
  activeHandler(value) {
    if (this.disabled) {
      this.active = false;
      this.open = false;
      return;
    }
    this.open = value;
  }
  openHandler(value) {
    if (!value) {
      updateAfterClose(this.floatingEl);
    }
    if (this.disabled) {
      this.active = false;
      this.open = false;
      return;
    }
    this.active = value;
    this.setMaxScrollerHeight();
  }
  handleDisabledChange(value) {
    if (!value) {
      this.active = false;
      this.open = false;
    }
  }
  maxItemsHandler() {
    this.setMaxScrollerHeight();
  }
  overlayPositioningHandler() {
    this.reposition(true);
  }
  valueHandler(value) {
    if (!this.internalValueChangeFlag) {
      const items = this.getItems();
      if (Array.isArray(value)) {
        items.forEach((item) => (item.selected = value.includes(item.value)));
      }
      else if (value) {
        items.forEach((item) => (item.selected = value === item.value));
      }
      else {
        items.forEach((item) => (item.selected = false));
      }
      this.updateItems();
    }
  }
  flipPlacementsHandler() {
    this.setFilteredPlacements();
    this.reposition(true);
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  documentClickHandler(event) {
    if (!isPrimaryPointerButton(event)) {
      return;
    }
    this.setInactiveIfNotContained(event);
  }
  calciteComboboxItemChangeHandler(event) {
    if (this.ignoreSelectedEventsFlag) {
      return;
    }
    const target = event.target;
    const newIndex = this.visibleItems.indexOf(target);
    this.updateActiveItemIndex(newIndex);
    this.toggleSelection(target, target.selected);
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /**
   * Updates the position of the component.
   *
   * @param delayed
   */
  async reposition(delayed = false) {
    const { floatingEl, referenceEl, placement, overlayPositioning, filteredFlipPlacements } = this;
    return reposition(this, {
      floatingEl,
      referenceEl,
      overlayPositioning,
      placement,
      flipPlacements: filteredFlipPlacements,
      type: "menu"
    }, delayed);
  }
  /** Sets focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.textInput) === null || _a === void 0 ? void 0 : _a.focus();
    this.activeChipIndex = -1;
    this.activeItemIndex = -1;
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    var _a;
    this.internalValueChangeFlag = true;
    this.value = this.getValue();
    this.internalValueChangeFlag = false;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el, { childList: true, subtree: true });
    connectLabel(this);
    connectForm(this);
    connectOpenCloseComponent(this);
    this.setFilteredPlacements();
    this.reposition(true);
    if (this.active) {
      this.activeHandler(this.active);
    }
    if (this.open) {
      this.openHandler(this.open);
    }
  }
  componentWillLoad() {
    this.updateItems();
  }
  componentDidLoad() {
    afterConnectDefaultValueSet(this, this.getValue());
    this.reposition(true);
  }
  componentDidRender() {
    if (this.el.offsetHeight !== this.inputHeight) {
      this.reposition(true);
      this.inputHeight = this.el.offsetHeight;
    }
    updateHostInteraction(this);
  }
  disconnectedCallback() {
    var _a, _b;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
    (_b = this.resizeObserver) === null || _b === void 0 ? void 0 : _b.disconnect();
    disconnectLabel(this);
    disconnectForm(this);
    disconnectFloatingUI(this, this.referenceEl, this.floatingEl);
    disconnectOpenCloseComponent(this);
  }
  selectedItemsHandler() {
    this.internalValueChangeFlag = true;
    this.value = this.getValue();
    this.internalValueChangeFlag = false;
  }
  /** when search text is cleared, reset active to  */
  textHandler() {
    this.updateActiveItemIndex(-1);
  }
  comboboxInViewport() {
    const bounding = this.el.getBoundingClientRect();
    return (bounding.top >= 0 &&
      bounding.left >= 0 &&
      bounding.right <= (window.innerWidth || document.documentElement.clientWidth) &&
      bounding.bottom <= (window.innerHeight || document.documentElement.clientHeight));
  }
  onBeforeOpen() {
    this.calciteComboboxBeforeOpen.emit();
  }
  onOpen() {
    this.calciteComboboxOpen.emit();
  }
  onBeforeClose() {
    this.calciteComboboxBeforeClose.emit();
  }
  onClose() {
    this.calciteComboboxClose.emit();
  }
  getMaxScrollerHeight() {
    const items = this.getCombinedItems().filter((item) => !item.hidden);
    const { maxItems } = this;
    let itemsToProcess = 0;
    let maxScrollerHeight = 0;
    if (items.length > maxItems) {
      items.forEach((item) => {
        if (itemsToProcess < maxItems && maxItems > 0) {
          const height = this.calculateSingleItemHeight(item);
          if (height > 0) {
            maxScrollerHeight += height;
            itemsToProcess++;
          }
        }
      });
    }
    return maxScrollerHeight;
  }
  calculateSingleItemHeight(item) {
    let height = item.offsetHeight;
    // if item has children items, don't count their height twice
    const children = Array.from(item.querySelectorAll(ComboboxChildSelector));
    children
      .map((child) => child === null || child === void 0 ? void 0 : child.offsetHeight)
      .forEach((offsetHeight) => {
      height -= offsetHeight;
    });
    return height;
  }
  getCombinedItems() {
    return [...this.groupItems, ...this.items];
  }
  toggleSelection(item, value = !item.selected) {
    if (!item) {
      return;
    }
    if (this.isMulti()) {
      item.selected = value;
      this.updateAncestors(item);
      this.selectedItems = this.getSelectedItems();
      this.emitCalciteLookupChange();
      this.emitComboboxChange();
      this.resetText();
      this.filterItems("");
    }
    else {
      this.ignoreSelectedEventsFlag = true;
      this.items.forEach((el) => (el.selected = el === item ? value : false));
      this.ignoreSelectedEventsFlag = false;
      this.selectedItems = this.getSelectedItems();
      this.emitComboboxChange();
      if (this.textInput) {
        this.textInput.value = item.textLabel;
      }
      this.open = false;
      this.updateActiveItemIndex(-1);
      this.resetText();
      this.filterItems("");
    }
  }
  updateAncestors(item) {
    if (this.selectionMode !== "ancestors") {
      return;
    }
    const ancestors = getItemAncestors(item);
    const children = getItemChildren(item);
    if (item.selected) {
      ancestors.forEach((el) => {
        el.selected = true;
      });
    }
    else {
      children.forEach((el) => (el.selected = false));
      [...ancestors].forEach((el) => {
        if (!hasActiveChildren(el)) {
          el.selected = false;
        }
      });
    }
  }
  getVisibleItems() {
    return this.items.filter((item) => !item.hidden);
  }
  getSelectedItems() {
    if (!this.isMulti()) {
      const match = this.items.find(({ selected }) => selected);
      return match ? [match] : [];
    }
    return (this.items
      .filter((item) => item.selected && (this.selectionMode !== "ancestors" || !hasActiveChildren(item)))
      /** Preserve order of entered tags */
      .sort((a, b) => {
      const aIdx = this.selectedItems.indexOf(a);
      const bIdx = this.selectedItems.indexOf(b);
      if (aIdx > -1 && bIdx > -1) {
        return aIdx - bIdx;
      }
      return bIdx - aIdx;
    }));
  }
  getData() {
    return this.items.map((item) => ({
      constant: item.constant,
      filterDisabled: item.filterDisabled,
      value: item.value,
      label: item.textLabel
    }));
  }
  getNeedsIcon() {
    return this.selectionMode === "single" && this.items.some((item) => item.icon);
  }
  resetText() {
    if (this.textInput) {
      this.textInput.value = "";
    }
    this.text = "";
  }
  getItems() {
    const items = Array.from(this.el.querySelectorAll(ComboboxItem));
    return items.filter((item) => !item.disabled);
  }
  getGroupItems() {
    return Array.from(this.el.querySelectorAll(ComboboxItemGroup));
  }
  addCustomChip(value, focus) {
    const existingItem = this.items.find((el) => el.textLabel === value);
    if (existingItem) {
      this.toggleSelection(existingItem, true);
    }
    else {
      if (!this.isMulti()) {
        this.toggleSelection(this.selectedItems[this.selectedItems.length - 1], false);
      }
      const item = document.createElement(ComboboxItem);
      item.value = value;
      item.textLabel = value;
      item.selected = true;
      this.el.appendChild(item);
      this.resetText();
      if (focus) {
        this.setFocus();
      }
      this.updateItems();
      this.filterItems("");
      this.emitCalciteLookupChange();
      this.emitComboboxChange();
    }
  }
  removeActiveChip() {
    this.toggleSelection(this.selectedItems[this.activeChipIndex], false);
    this.setFocus();
  }
  removeLastChip() {
    this.toggleSelection(this.selectedItems[this.selectedItems.length - 1], false);
    this.setFocus();
  }
  previousChip() {
    if (this.text) {
      return;
    }
    const length = this.selectedItems.length - 1;
    const active = this.activeChipIndex;
    this.activeChipIndex = active === -1 ? length : Math.max(active - 1, 0);
    this.updateActiveItemIndex(-1);
    this.focusChip();
  }
  nextChip() {
    if (this.text || this.activeChipIndex === -1) {
      return;
    }
    const last = this.selectedItems.length - 1;
    const newIndex = this.activeChipIndex + 1;
    if (newIndex > last) {
      this.activeChipIndex = -1;
      this.setFocus();
    }
    else {
      this.activeChipIndex = newIndex;
      this.focusChip();
    }
    this.updateActiveItemIndex(-1);
  }
  focusChip() {
    var _a;
    const guid = (_a = this.selectedItems[this.activeChipIndex]) === null || _a === void 0 ? void 0 : _a.guid;
    const chip = guid
      ? this.referenceEl.querySelector(`#${chipUidPrefix}${guid}`)
      : null;
    chip === null || chip === void 0 ? void 0 : chip.setFocus();
  }
  shiftActiveItemIndex(delta) {
    const { length } = this.visibleItems;
    const newIndex = (this.activeItemIndex + length + delta) % length;
    this.updateActiveItemIndex(newIndex);
    this.scrollToActiveItem();
  }
  updateActiveItemIndex(index) {
    var _a;
    this.activeItemIndex = index;
    let activeDescendant = null;
    this.visibleItems.forEach((el, i) => {
      if (i === index) {
        el.active = true;
        activeDescendant = `${itemUidPrefix}${el.guid}`;
      }
      else {
        el.active = false;
      }
    });
    this.activeDescendant = activeDescendant;
    if (this.activeItemIndex > -1) {
      this.activeChipIndex = -1;
      (_a = this.textInput) === null || _a === void 0 ? void 0 : _a.focus();
    }
  }
  isMulti() {
    return this.selectionMode !== "single";
  }
  //--------------------------------------------------------------------------
  //
  //  Render Methods
  //
  //--------------------------------------------------------------------------
  renderChips() {
    const { activeChipIndex, scale, selectionMode, intlRemoveTag } = this;
    return this.selectedItems.map((item, i) => {
      const chipClasses = {
        chip: true,
        "chip--active": activeChipIndex === i
      };
      const ancestors = [...getItemAncestors(item)].reverse();
      const pathLabel = [...ancestors, item].map((el) => el.textLabel);
      const label = selectionMode !== "ancestors" ? item.textLabel : pathLabel.join(" / ");
      return (h("calcite-chip", { class: chipClasses, dismissLabel: intlRemoveTag, dismissible: true, icon: item.icon, id: item.guid ? `${chipUidPrefix}${item.guid}` : null, key: item.textLabel, onCalciteChipDismiss: (event) => this.calciteChipDismissHandler(event, item), scale: scale, title: label, value: item.value }, label));
    });
  }
  renderInput() {
    const { guid, active, disabled, placeholder, selectionMode, selectedItems, open } = this;
    const single = selectionMode === "single";
    const selectedItem = selectedItems[0];
    const showLabel = !(open || active) && single && !!selectedItem;
    return (h("span", { class: {
        "input-wrap": true,
        "input-wrap--single": single
      } }, showLabel && (h("span", { class: {
        label: true,
        "label--icon": !!(selectedItem === null || selectedItem === void 0 ? void 0 : selectedItem.icon)
      }, key: "label" }, selectedItem.textLabel)), h("input", { "aria-activedescendant": this.activeDescendant, "aria-autocomplete": "list", "aria-controls": `${listboxUidPrefix}${guid}`, "aria-label": getLabelText(this), class: {
        input: true,
        "input--single": true,
        "input--transparent": this.activeChipIndex > -1,
        "input--hidden": showLabel,
        "input--icon": !!this.placeholderIcon
      }, disabled: disabled, id: `${inputUidPrefix}${guid}`, key: "input", onBlur: this.comboboxBlurHandler, onFocus: this.comboboxFocusHandler, onInput: this.inputHandler, placeholder: placeholder, ref: (el) => (this.textInput = el), type: "text" })));
  }
  renderListBoxOptions() {
    return this.visibleItems.map((item) => (h("li", { "aria-selected": toAriaBoolean(item.selected), id: item.guid ? `${itemUidPrefix}${item.guid}` : null, role: "option", tabindex: "-1" }, item.textLabel)));
  }
  renderFloatingUIContainer() {
    const { active, setFloatingEl, setContainerEl, open } = this;
    const classes = {
      "list-container": true,
      [FloatingCSS.animation]: true,
      [FloatingCSS.animationActive]: open || active
    };
    return (h("div", { "aria-hidden": "true", class: {
        "floating-ui-container": true,
        "floating-ui-container--active": open || active
      }, ref: setFloatingEl }, h("div", { class: classes, ref: setContainerEl }, h("ul", { class: { list: true, "list--hide": !(open || active) } }, h("slot", null)))));
  }
  renderIconStart() {
    const { selectedItems, placeholderIcon, selectionMode } = this;
    const selectedItem = selectedItems[0];
    const selectedIcon = selectedItem === null || selectedItem === void 0 ? void 0 : selectedItem.icon;
    const singleSelectionMode = selectionMode === "single";
    const iconAtStart = !this.open && selectedItem
      ? !!selectedIcon && singleSelectionMode
      : !!this.placeholderIcon && (!selectedItem || singleSelectionMode);
    return (iconAtStart && (h("span", { class: "icon-start" }, h("calcite-icon", { class: "selected-icon", icon: !this.open && selectedItem ? selectedIcon : placeholderIcon, scale: "s" }))));
  }
  renderIconEnd() {
    const { active, open } = this;
    return (h("span", { class: "icon-end" }, h("calcite-icon", { icon: active || open ? "chevron-up" : "chevron-down", scale: "s" })));
  }
  render() {
    const { active, guid, label, open } = this;
    const single = this.selectionMode === "single";
    return (h(Host, null, h("div", { "aria-autocomplete": "list", "aria-controls": `${listboxUidPrefix}${guid}`, "aria-expanded": toAriaBoolean(open || active), "aria-haspopup": "listbox", "aria-labelledby": `${labelUidPrefix}${guid}`, "aria-live": "polite", "aria-owns": `${listboxUidPrefix}${guid}`, class: {
        wrapper: true,
        "wrapper--single": single || !this.selectedItems.length,
        "wrapper--active": open || active
      }, onClick: this.clickHandler, onKeyDown: this.keydownHandler, ref: this.setReferenceEl, role: "combobox" }, h("div", { class: "grid-input" }, this.renderIconStart(), !single && this.renderChips(), h("label", { class: "screen-readers-only", htmlFor: `${inputUidPrefix}${guid}`, id: `${labelUidPrefix}${guid}` }, label), this.renderInput()), this.renderIconEnd()), h("ul", { "aria-labelledby": `${labelUidPrefix}${guid}`, "aria-multiselectable": "true", class: "screen-readers-only", id: `${listboxUidPrefix}${guid}`, role: "listbox", tabIndex: -1 }, this.renderListBoxOptions()), this.renderFloatingUIContainer(), h(HiddenFormInputSlot, { component: this })));
  }
  static get is() { return "calcite-combobox"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["combobox.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["combobox.css"]
    };
  }
  static get properties() {
    return {
      "active": {
        "type": "boolean",
        "mutable": true,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use `open` instead."
            }],
          "text": "When `true`, displays and positions the component."
        },
        "attribute": "active",
        "reflect": true,
        "defaultValue": "false"
      },
      "open": {
        "type": "boolean",
        "mutable": true,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "When `true`, displays and positions the component."
        },
        "attribute": "open",
        "reflect": true,
        "defaultValue": "false"
      },
      "disabled": {
        "type": "boolean",
        "mutable": false,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "When `true`, interaction is prevented and the component is displayed with lower opacity."
        },
        "attribute": "disabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "label": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": true,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Accessible name for the component."
        },
        "attribute": "label",
        "reflect": false
      },
      "placeholder": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies the placeholder text for the input."
        },
        "attribute": "placeholder",
        "reflect": false
      },
      "placeholderIcon": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies the placeholder icon for the input."
        },
        "attribute": "placeholder-icon",
        "reflect": true
      },
      "maxItems": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "number",
          "resolved": "number",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the maximum number of `calcite-combobox-item`s (including nested children) to display before displaying a scrollbar."
        },
        "attribute": "max-items",
        "reflect": true,
        "defaultValue": "0"
      },
      "name": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the name of the component on form submission."
        },
        "attribute": "name",
        "reflect": true
      },
      "allowCustomValues": {
        "type": "boolean",
        "mutable": false,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "When `true`, allows entry of custom values, which are not in the original set of items."
        },
        "attribute": "allow-custom-values",
        "reflect": true
      },
      "overlayPositioning": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "OverlayPositioning",
          "resolved": "\"absolute\" | \"fixed\"",
          "references": {
            "OverlayPositioning": {
              "location": "import",
              "path": "../../utils/floating-ui"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Determines the type of positioning to use for the overlaid content.\n\nUsing `\"absolute\"` will work for most cases. The component will be positioned inside of overflowing parent containers and will affect the container's layout.\n\n`\"fixed\"` should be used to escape an overflowing parent container, or when the reference element's `position` CSS property is `\"fixed\"`."
        },
        "attribute": "overlay-positioning",
        "reflect": true,
        "defaultValue": "\"absolute\""
      },
      "required": {
        "type": "boolean",
        "mutable": false,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": "When `true`, the component must have a value in order for the form to submit."
        },
        "attribute": "required",
        "reflect": true,
        "defaultValue": "false"
      },
      "selectionMode": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "ComboboxSelectionMode",
          "resolved": "\"ancestors\" | \"multi\" | \"multiple\" | \"single\"",
          "references": {
            "ComboboxSelectionMode": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "specify the selection mode\n- multiple: allow any number of selected items (default)\n- single: only one selection)\n- ancestors: like multiple, but show ancestors of selected items as selected, only deepest children shown in chips"
        },
        "attribute": "selection-mode",
        "reflect": true,
        "defaultValue": "\"multi\""
      },
      "scale": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Scale",
          "resolved": "\"l\" | \"m\" | \"s\"",
          "references": {
            "Scale": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the size of the component."
        },
        "attribute": "scale",
        "reflect": true,
        "defaultValue": "\"m\""
      },
      "value": {
        "type": "string",
        "mutable": true,
        "complexType": {
          "original": "string | string[]",
          "resolved": "string | string[]",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "The component's value(s) from the selected `calcite-combobox-item`(s)."
        },
        "attribute": "value",
        "reflect": false,
        "defaultValue": "null"
      },
      "intlRemoveTag": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"Remove tag\""
            }],
          "text": "Accessible name for the component's remove tag when a `calcite-combobox-item` is selected."
        },
        "attribute": "intl-remove-tag",
        "reflect": false,
        "defaultValue": "TEXT.removeTag"
      },
      "flipPlacements": {
        "type": "unknown",
        "mutable": false,
        "complexType": {
          "original": "EffectivePlacement[]",
          "resolved": "Placement[]",
          "references": {
            "EffectivePlacement": {
              "location": "import",
              "path": "../../utils/floating-ui"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Defines the available placements that can be used when a flip occurs."
        }
      }
    };
  }
  static get states() {
    return {
      "items": {},
      "groupItems": {},
      "selectedItems": {},
      "visibleItems": {},
      "needsIcon": {},
      "activeItemIndex": {},
      "activeChipIndex": {},
      "activeDescendant": {},
      "text": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteLookupChange",
        "name": "calciteLookupChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use `calciteComboboxChange` instead."
            }],
          "text": "Fires when the selected items set changes."
        },
        "complexType": {
          "original": "HTMLCalciteComboboxItemElement[]",
          "resolved": "HTMLCalciteComboboxItemElement[]",
          "references": {
            "HTMLCalciteComboboxItemElement": {
              "location": "global"
            }
          }
        }
      }, {
        "method": "calciteComboboxChange",
        "name": "calciteComboboxChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the selected item(s) changes."
        },
        "complexType": {
          "original": "{\n    selectedItems: HTMLCalciteComboboxItemElement[];\n  }",
          "resolved": "{ selectedItems: HTMLCalciteComboboxItemElement[]; }",
          "references": {
            "HTMLCalciteComboboxItemElement": {
              "location": "global"
            }
          }
        }
      }, {
        "method": "calciteComboboxFilterChange",
        "name": "calciteComboboxFilterChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when text is added to filter the options list."
        },
        "complexType": {
          "original": "{\n    visibleItems: HTMLCalciteComboboxItemElement[];\n    text: string;\n  }",
          "resolved": "{ visibleItems: HTMLCalciteComboboxItemElement[]; text: string; }",
          "references": {
            "HTMLCalciteComboboxItemElement": {
              "location": "global"
            }
          }
        }
      }, {
        "method": "calciteComboboxChipDismiss",
        "name": "calciteComboboxChipDismiss",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when a selected item in the component is dismissed via its `calcite-chip`.\n\n**Note:**: The event payload is deprecated, please use the `value` property on the component to determine the removed value instead."
        },
        "complexType": {
          "original": "DeprecatedEventPayload",
          "resolved": "any",
          "references": {
            "DeprecatedEventPayload": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        }
      }, {
        "method": "calciteComboboxBeforeClose",
        "name": "calciteComboboxBeforeClose",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is requested to be closed, and before the closing transition begins."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteComboboxClose",
        "name": "calciteComboboxClose",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is closed and animation is complete."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteComboboxBeforeOpen",
        "name": "calciteComboboxBeforeOpen",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is added to the DOM but not rendered, and before the opening transition begins."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteComboboxOpen",
        "name": "calciteComboboxOpen",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is open and animation is complete."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }];
  }
  static get methods() {
    return {
      "reposition": {
        "complexType": {
          "signature": "(delayed?: boolean) => Promise<void>",
          "parameters": [{
              "tags": [{
                  "name": "param",
                  "text": "delayed"
                }],
              "text": ""
            }],
          "references": {
            "Promise": {
              "location": "global"
            }
          },
          "return": "Promise<void>"
        },
        "docs": {
          "text": "Updates the position of the component.",
          "tags": [{
              "name": "param",
              "text": "delayed"
            }]
        }
      },
      "setFocus": {
        "complexType": {
          "signature": "() => Promise<void>",
          "parameters": [],
          "references": {
            "Promise": {
              "location": "global"
            }
          },
          "return": "Promise<void>"
        },
        "docs": {
          "text": "Sets focus on the component.",
          "tags": []
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "active",
        "methodName": "activeHandler"
      }, {
        "propName": "open",
        "methodName": "openHandler"
      }, {
        "propName": "disabled",
        "methodName": "handleDisabledChange"
      }, {
        "propName": "maxItems",
        "methodName": "maxItemsHandler"
      }, {
        "propName": "overlayPositioning",
        "methodName": "overlayPositioningHandler"
      }, {
        "propName": "value",
        "methodName": "valueHandler"
      }, {
        "propName": "flipPlacements",
        "methodName": "flipPlacementsHandler"
      }, {
        "propName": "selectedItems",
        "methodName": "selectedItemsHandler"
      }, {
        "propName": "text",
        "methodName": "textHandler"
      }];
  }
  static get listeners() {
    return [{
        "name": "pointerdown",
        "method": "documentClickHandler",
        "target": "document",
        "capture": false,
        "passive": true
      }, {
        "name": "calciteComboboxItemChange",
        "method": "calciteComboboxItemChangeHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }];
  }
}
