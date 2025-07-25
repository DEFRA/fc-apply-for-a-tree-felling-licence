/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { toAriaBoolean } from "../../utils/dom";
import { CSS, SLOTS } from "./resources";
export const List = ({ props: { disabled, loading, filterEnabled, dataForFilter, handleFilter, filterPlaceholder, setFilterEl, dragEnabled, storeAssistiveEl }, ...rest }) => {
  const defaultSlot = h("slot", null);
  return (h(Host, { "aria-busy": toAriaBoolean(loading), role: "menu", ...rest }, h("section", null, dragEnabled ? (h("span", { "aria-live": "assertive", class: "assistive-text", ref: storeAssistiveEl })) : null, h("header", { class: { [CSS.sticky]: true } }, filterEnabled ? (h("calcite-filter", { "aria-label": filterPlaceholder, disabled: loading || disabled, items: dataForFilter, onCalciteFilterChange: handleFilter, placeholder: filterPlaceholder, ref: setFilterEl })) : null, h("slot", { name: SLOTS.menuActions })), loading ? h("calcite-scrim", { loading: loading }) : null, defaultSlot)));
};
export default List;
