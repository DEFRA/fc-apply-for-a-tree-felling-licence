/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
'use strict';

const dom = require('./dom-2b919cb6.js');
const array = require('./array-e3a23914.js');
const debounce = require('./debounce-3c20d30d.js');
const index = require('./index-b27b231c.js');

const CSS$2 = {
  heading: "heading",
  container: "container",
  indented: "container--indented"
};
const SLOTS$2 = {
  parentItem: "parent-item"
};
const HEADING_LEVEL = 3;

function mutationObserverCallback() {
  this.setUpItems();
  this.setUpFilter();
  this.deselectRemovedItems();
}
const SUPPORTED_ARROW_KEYS = ["ArrowUp", "ArrowDown"];
// --------------------------------------------------------------------------
//
//  Lifecycle
//
// --------------------------------------------------------------------------
function initialize() {
  this.setUpItems();
  this.setUpFilter();
  this.emitCalciteListChange = debounce.debounce(internalCalciteListChangeEvent.bind(this), 0);
}
function initializeObserver() {
  var _a;
  (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el, { childList: true, subtree: true });
}
function cleanUpObserver() {
  var _a;
  (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
}
// --------------------------------------------------------------------------
//
//  Listeners
//
// --------------------------------------------------------------------------
function calciteListItemChangeHandler(event) {
  const { selectedValues } = this;
  const { item, value, selected, shiftPressed } = event.detail;
  if (selected) {
    if (this.multiple && shiftPressed) {
      this.selectSiblings(item);
    }
    if (!this.multiple) {
      this.deselectSiblingItems(item);
    }
    selectedValues.set(value, item);
  }
  else {
    selectedValues.delete(value);
    if (this.multiple && shiftPressed) {
      this.selectSiblings(item, true);
    }
  }
  if (!this.multiple) {
    toggleSingleSelectItemTabbing(item, selected);
    if (selected) {
      dom.focusElement(item);
    }
  }
  this.lastSelectedItem = item;
  this.emitCalciteListChange();
}
function calciteInternalListItemValueChangeHandler(event) {
  const oldValue = event.detail.oldValue;
  const selectedValues = this.selectedValues;
  if (selectedValues.has(oldValue)) {
    const item = selectedValues.get(oldValue);
    selectedValues.delete(oldValue);
    selectedValues.set(event.detail.newValue, item);
  }
  event.stopPropagation();
}
// --------------------------------------------------------------------------
//
//  Private Methods
//
// --------------------------------------------------------------------------
function isValidNavigationKey(key) {
  return !!SUPPORTED_ARROW_KEYS.find((k) => k === key);
}
function calciteListFocusOutHandler(event) {
  const { el, items, multiple, selectedValues } = this;
  if (multiple) {
    return;
  }
  const focusedInside = el.contains(event.relatedTarget);
  if (focusedInside) {
    return;
  }
  filterOutDisabled(items).forEach((item) => {
    toggleSingleSelectItemTabbing(item, selectedValues.size === 0 ? item.contains(event.target) || event.target === item : item.selected);
  });
}
function keyDownHandler(event) {
  const { key, target } = event;
  if (!isValidNavigationKey(key)) {
    return;
  }
  const { items, multiple, selectionFollowsFocus } = this;
  const { length: totalItems } = items;
  const currentIndex = items.indexOf(target);
  if (!totalItems || currentIndex === -1) {
    return;
  }
  event.preventDefault();
  const index = moveItemIndex(this, target, key === "ArrowUp" ? "up" : "down");
  const item = items[index];
  items.forEach((i) => toggleSingleSelectItemTabbing(i, i === item));
  if (!multiple && selectionFollowsFocus) {
    item.selected = true;
  }
  dom.focusElement(item);
}
function moveItemIndex(list, item, direction) {
  const { items } = list;
  const { length: totalItems } = items;
  const currentIndex = items.indexOf(item);
  const directionFactor = direction === "up" ? -1 : 1;
  let moveOffset = 1;
  let index = array.getRoundRobinIndex(currentIndex + directionFactor * moveOffset++, totalItems);
  const firstMovedIndex = index;
  while (items[index].disabled) {
    index = array.getRoundRobinIndex(currentIndex + directionFactor * moveOffset++, totalItems);
    if (index === firstMovedIndex) {
      break;
    }
  }
  return index;
}
function getItemIndex(list, item) {
  const { items } = list;
  return items.indexOf(item);
}
function filterOutDisabled(items) {
  return items.filter((item) => !item.disabled);
}
function internalCalciteListChangeEvent() {
  this.calciteListChange.emit(this.selectedValues);
}
function removeItem(event) {
  if (event.defaultPrevented) {
    return;
  }
  const item = event.target;
  const selectedValues = this.selectedValues;
  if (item.parentElement.tagName === "CALCITE-PICK-LIST-GROUP" && item.slot === SLOTS$2.parentItem) {
    item.parentElement.remove();
    Array.from(item.parentElement.children).forEach((item) => selectedValues.delete(item.value));
  }
  else {
    item.remove();
    selectedValues.delete(item.value);
  }
  this.emitCalciteListChange();
}
function toggleSingleSelectItemTabbing(item, selectable) {
  if (item.disabled) {
    return;
  }
  // using attribute intentionally
  if (selectable) {
    item.removeAttribute("tabindex");
  }
  else {
    item.setAttribute("tabindex", "-1");
  }
}
async function setFocus(focusId) {
  var _a;
  if (this.filterEnabled && focusId === "filter") {
    await dom.focusElement(this.filterEl);
    return;
  }
  const { items, multiple, selectionFollowsFocus } = this;
  if (items.length === 0) {
    return;
  }
  if (multiple) {
    return (_a = filterOutDisabled(items)[0]) === null || _a === void 0 ? void 0 : _a.setFocus();
  }
  const filtered = filterOutDisabled(items);
  const focusTarget = filtered.find((item) => item.selected) || filtered[0];
  if (selectionFollowsFocus && focusTarget) {
    focusTarget.selected = true;
  }
  return focusTarget.setFocus();
}
function setUpItems(tagName) {
  this.items = Array.from(this.el.querySelectorAll(tagName));
  let hasSelected = false;
  const { items } = this;
  items.forEach((item) => {
    item.icon = this.getIconType();
    if (!this.multiple) {
      item.disableDeselect = true;
      toggleSingleSelectItemTabbing(item, false);
    }
    if (item.selected) {
      hasSelected = true;
      toggleSingleSelectItemTabbing(item, true);
      this.selectedValues.set(item.value, item);
    }
  });
  const [first] = items;
  if (!hasSelected && first && !first.disabled) {
    toggleSingleSelectItemTabbing(first, true);
  }
}
function deselectRemovedItems() {
  const selectedValues = this.selectedValues;
  const itemValues = this.items.map(({ value }) => value);
  selectedValues.forEach((selectedItem) => {
    if (!itemValues.includes(selectedItem.value)) {
      this.selectedValues.delete(selectedItem.value);
    }
  });
}
function deselectSiblingItems(item) {
  this.items.forEach((currentItem) => {
    if (currentItem.value !== item.value) {
      currentItem.toggleSelected(false);
      if (this.selectedValues.has(currentItem.value)) {
        this.selectedValues.delete(currentItem.value);
      }
    }
  });
}
function selectSiblings(item, deselect = false) {
  if (!this.lastSelectedItem) {
    return;
  }
  const { items } = this;
  const start = items.findIndex((currentItem) => {
    return currentItem.value === this.lastSelectedItem.value;
  });
  const end = items.findIndex((currentItem) => {
    return currentItem.value === item.value;
  });
  items.slice(Math.min(start, end), Math.max(start, end)).forEach((currentItem) => {
    currentItem.toggleSelected(!deselect);
    if (!deselect) {
      this.selectedValues.set(currentItem.value, currentItem);
    }
    else {
      this.selectedValues.delete(currentItem.value);
    }
  });
}
let groups;
function handleFilter(event) {
  const { filteredItems } = event.currentTarget;
  const values = filteredItems.map((item) => item.value);
  let hasSelectedMatch = false;
  if (!groups) {
    groups = new Set();
  }
  const matchedItems = this.items.filter((item) => {
    const parent = item.parentElement;
    const grouped = parent.matches("calcite-pick-list-group");
    if (grouped) {
      groups.add(parent);
    }
    const matches = values.includes(item.value);
    item.hidden = !matches;
    if (!hasSelectedMatch) {
      hasSelectedMatch = matches && item.selected;
    }
    return matches;
  });
  groups.forEach((group) => {
    const hasAtLeastOneMatch = matchedItems.some((item) => group.contains(item));
    group.hidden = !hasAtLeastOneMatch;
    if (!hasAtLeastOneMatch) {
      return;
    }
    const parentItem = dom.getSlotted(group, "parent-item");
    if (parentItem) {
      parentItem.hidden = false;
      if (matchedItems.includes(parentItem)) {
        Array.from(group.children).forEach((child) => (child.hidden = false));
      }
    }
  });
  groups.clear();
  if (matchedItems.length > 0 && !hasSelectedMatch && !this.multiple) {
    toggleSingleSelectItemTabbing(matchedItems[0], true);
  }
}
function getItemData() {
  return this.items.map((item) => ({
    label: item.label,
    description: item.description,
    metadata: item.metadata,
    value: item.value
  }));
}

const CSS$1 = {
  sticky: "sticky-pos"
};
exports.ICON_TYPES = void 0;
(function (ICON_TYPES) {
  ICON_TYPES["circle"] = "circle";
  ICON_TYPES["square"] = "square";
  ICON_TYPES["grip"] = "grip";
})(exports.ICON_TYPES || (exports.ICON_TYPES = {}));
const SLOTS$1 = {
  menuActions: "menu-actions"
};

const List = ({ props: { disabled, loading, filterEnabled, dataForFilter, handleFilter, filterPlaceholder, setFilterEl, dragEnabled, storeAssistiveEl }, ...rest }) => {
  const defaultSlot = index.h("slot", null);
  return (index.h(index.Host, { "aria-busy": dom.toAriaBoolean(loading), role: "menu", ...rest },
    index.h("section", null,
      dragEnabled ? (index.h("span", { "aria-live": "assertive", class: "assistive-text", ref: storeAssistiveEl })) : null,
      index.h("header", { class: { [CSS$1.sticky]: true } },
        filterEnabled ? (index.h("calcite-filter", { "aria-label": filterPlaceholder, disabled: loading || disabled, items: dataForFilter, onCalciteFilterChange: handleFilter, placeholder: filterPlaceholder, ref: setFilterEl })) : null,
        index.h("slot", { name: SLOTS$1.menuActions })),
      loading ? index.h("calcite-scrim", { loading: loading }) : null,
      defaultSlot)));
};

const CSS = {
  actions: "actions",
  actionsEnd: "actions--end",
  actionsStart: "actions--start",
  description: "description",
  handle: "handle",
  handleActivated: "handle--activated",
  highlight: "highlight",
  icon: "icon",
  iconDot: "icon-dot",
  label: "label",
  remove: "remove",
  title: "title",
  textContainer: "text-container"
};
const ICONS = {
  checked: "check",
  remove: "x"
};
const SLOTS = {
  actionsEnd: "actions-end",
  actionsStart: "actions-start"
};
const TEXT = {
  remove: "Remove"
};

exports.CSS = CSS;
exports.CSS$1 = CSS$2;
exports.HEADING_LEVEL = HEADING_LEVEL;
exports.ICONS = ICONS;
exports.List = List;
exports.SLOTS = SLOTS;
exports.SLOTS$1 = SLOTS$2;
exports.TEXT = TEXT;
exports.calciteInternalListItemValueChangeHandler = calciteInternalListItemValueChangeHandler;
exports.calciteListFocusOutHandler = calciteListFocusOutHandler;
exports.calciteListItemChangeHandler = calciteListItemChangeHandler;
exports.cleanUpObserver = cleanUpObserver;
exports.deselectRemovedItems = deselectRemovedItems;
exports.deselectSiblingItems = deselectSiblingItems;
exports.getItemData = getItemData;
exports.getItemIndex = getItemIndex;
exports.handleFilter = handleFilter;
exports.initialize = initialize;
exports.initializeObserver = initializeObserver;
exports.keyDownHandler = keyDownHandler;
exports.moveItemIndex = moveItemIndex;
exports.mutationObserverCallback = mutationObserverCallback;
exports.removeItem = removeItem;
exports.selectSiblings = selectSiblings;
exports.setFocus = setFocus;
exports.setUpItems = setUpItems;
