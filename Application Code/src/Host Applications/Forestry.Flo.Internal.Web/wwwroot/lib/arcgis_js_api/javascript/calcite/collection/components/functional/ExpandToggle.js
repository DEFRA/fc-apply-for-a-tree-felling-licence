/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { getElementDir } from "../../utils/dom";
import { queryActions } from "../action-bar/utils";
import { SLOTS as ACTION_GROUP_SLOTS } from "../action-group/resources";
const ICONS = {
  chevronsLeft: "chevrons-left",
  chevronsRight: "chevrons-right"
};
function getCalcitePosition(position, el) {
  var _a;
  return position || ((_a = el.closest("calcite-shell-panel")) === null || _a === void 0 ? void 0 : _a.position) || "start";
}
export function toggleChildActionText({ parent, expanded }) {
  queryActions(parent)
    .filter((el) => el.slot !== ACTION_GROUP_SLOTS.menuActions)
    .forEach((action) => (action.textEnabled = expanded));
  parent
    .querySelectorAll("calcite-action-group, calcite-action-menu")
    .forEach((el) => (el.expanded = expanded));
}
const setTooltipReference = ({ tooltip, referenceElement, expanded, ref }) => {
  if (tooltip) {
    tooltip.referenceElement = !expanded && referenceElement ? referenceElement : null;
  }
  if (ref) {
    ref(referenceElement);
  }
  return referenceElement;
};
export const ExpandToggle = ({ expanded, intlExpand, intlCollapse, toggle, el, position, tooltip, ref, scale }) => {
  const rtl = getElementDir(el) === "rtl";
  const expandText = expanded ? intlCollapse : intlExpand;
  const icons = [ICONS.chevronsLeft, ICONS.chevronsRight];
  if (rtl) {
    icons.reverse();
  }
  const end = getCalcitePosition(position, el) === "end";
  const expandIcon = end ? icons[1] : icons[0];
  const collapseIcon = end ? icons[0] : icons[1];
  const actionNode = (h("calcite-action", { icon: expanded ? expandIcon : collapseIcon, onClick: toggle, ref: (referenceElement) => setTooltipReference({ tooltip, referenceElement, expanded, ref }), scale: scale, text: expandText, textEnabled: expanded }));
  return actionNode;
};
