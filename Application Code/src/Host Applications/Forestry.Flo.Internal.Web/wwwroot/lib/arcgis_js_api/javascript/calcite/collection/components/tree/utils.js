/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
function isTreeItem(element) {
  return element === null || element === void 0 ? void 0 : element.matches("calcite-tree-item");
}
export function getEnabledSiblingItem(el, direction) {
  const directionProp = direction === "down" ? "nextElementSibling" : "previousElementSibling";
  let currentEl = el;
  let enabledEl = null;
  while (isTreeItem(currentEl)) {
    if (!currentEl.disabled) {
      enabledEl = currentEl;
      break;
    }
    currentEl = currentEl[directionProp];
  }
  return enabledEl;
}
