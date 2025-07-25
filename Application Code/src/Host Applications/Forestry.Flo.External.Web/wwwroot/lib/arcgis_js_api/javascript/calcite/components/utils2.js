/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { n as nodeListToArray } from './dom.js';
import { Build } from '@stencil/core/internal/client/index.js';

const ComboboxItem = "CALCITE-COMBOBOX-ITEM";
const ComboboxItemGroup = "CALCITE-COMBOBOX-ITEM-GROUP";
const ComboboxChildSelector = `${ComboboxItem}, ${ComboboxItemGroup}`;
const TEXT = {
  removeTag: "Remove tag"
};

function getAncestors(element) {
  var _a, _b;
  const parent = (_a = element.parentElement) === null || _a === void 0 ? void 0 : _a.closest(ComboboxChildSelector);
  const grandparent = (_b = parent === null || parent === void 0 ? void 0 : parent.parentElement) === null || _b === void 0 ? void 0 : _b.closest(ComboboxChildSelector);
  return [parent, grandparent].filter((el) => el);
}
function getItemAncestors(item) {
  var _a;
  return (((_a = item.ancestors) === null || _a === void 0 ? void 0 : _a.filter((el) => el.nodeName === "CALCITE-COMBOBOX-ITEM")) || []);
}
function getItemChildren(item) {
  return nodeListToArray(item.querySelectorAll("calcite-combobox-item"));
}
function hasActiveChildren(node) {
  const items = nodeListToArray(node.querySelectorAll("calcite-combobox-item"));
  return items.filter((item) => item.selected).length > 0;
}
function getDepth(element) {
  if (!Build.isBrowser) {
    return 0;
  }
  const result = document.evaluate("ancestor::calcite-combobox-item | ancestor::calcite-combobox-item-group", element, null, XPathResult.UNORDERED_NODE_SNAPSHOT_TYPE, null);
  return result.snapshotLength;
}

export { ComboboxItemGroup as C, TEXT as T, ComboboxChildSelector as a, getItemChildren as b, ComboboxItem as c, getAncestors as d, getDepth as e, getItemAncestors as g, hasActiveChildren as h };
