/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { forceUpdate } from "@stencil/core";
import { SLOTS as ACTION_MENU_SLOTS } from "../action-menu/resources";
import { SLOTS as ACTION_GROUP_SLOTS } from "../action-group/resources";
export const overflowActionsDebounceInMs = 150;
const groupBufferPx = 2;
const getAverage = (arr) => arr.reduce((p, c) => p + c, 0) / arr.length;
export const geActionDimensions = (actions) => {
  const actionLen = actions === null || actions === void 0 ? void 0 : actions.length;
  return {
    actionWidth: actionLen ? getAverage(actions.map((action) => action.clientWidth || 0)) : 0,
    actionHeight: actionLen ? getAverage(actions.map((action) => action.clientHeight || 0)) : 0
  };
};
const getMaxActionCount = ({ width, actionWidth, layout, height, actionHeight, groupCount }) => {
  const maxContainerPx = layout === "horizontal" ? width : height;
  const avgItemPx = layout === "horizontal" ? actionWidth : actionHeight;
  return Math.floor((maxContainerPx - groupCount * groupBufferPx) / avgItemPx);
};
export const getOverflowCount = ({ layout, actionCount, actionWidth, width, actionHeight, height, groupCount }) => {
  return Math.max(actionCount - getMaxActionCount({ width, actionWidth, layout, height, actionHeight, groupCount }), 0);
};
export const queryActions = (el) => {
  return Array.from(el.querySelectorAll("calcite-action")).filter((action) => action.closest("calcite-action-menu") ? action.slot === ACTION_MENU_SLOTS.trigger : true);
};
export const overflowActions = ({ actionGroups, expanded, overflowCount }) => {
  let needToSlotCount = overflowCount;
  actionGroups.reverse().forEach((group) => {
    let slottedWithinGroupCount = 0;
    const groupActions = queryActions(group).reverse();
    groupActions.forEach((groupAction) => {
      if (groupAction.slot === ACTION_GROUP_SLOTS.menuActions) {
        groupAction.removeAttribute("slot");
        groupAction.textEnabled = expanded;
      }
    });
    if (needToSlotCount > 0) {
      groupActions.some((groupAction) => {
        const unslottedActions = groupActions.filter((action) => !action.slot);
        if (unslottedActions.length > 1 && groupActions.length > 2 && !groupAction.closest("calcite-action-menu")) {
          groupAction.textEnabled = true;
          groupAction.setAttribute("slot", ACTION_GROUP_SLOTS.menuActions);
          slottedWithinGroupCount++;
          if (slottedWithinGroupCount > 1) {
            needToSlotCount--;
          }
        }
        return needToSlotCount < 1;
      });
    }
    forceUpdate(group);
  });
};
