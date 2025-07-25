/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { getItemIndex } from "../pick-list/shared-list-logic";
export function getScreenReaderText(item, status, valueList) {
  const { items, intlDragHandleIdle, intlDragHandleActive, intlDragHandleChange, intlDragHandleCommit } = valueList;
  const total = items.length;
  const position = getItemIndex(valueList, item) + 1;
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
export function getHandleAndItemElement(event) {
  const handle = event
    .composedPath()
    .find((item) => { var _a; return ((_a = item.dataset) === null || _a === void 0 ? void 0 : _a.jsHandle) !== undefined; });
  const item = event
    .composedPath()
    .find((item) => { var _a; return ((_a = item.tagName) === null || _a === void 0 ? void 0 : _a.toLowerCase()) === "calcite-value-list-item"; });
  return { handle, item };
}
export function replacePlaceholders(text, label, position, total) {
  const replacePosition = text.replace("${position}", position.toString());
  const replaceLabel = replacePosition.replace("${item.label}", label);
  return replaceLabel.replace("${total}", total.toString());
}
