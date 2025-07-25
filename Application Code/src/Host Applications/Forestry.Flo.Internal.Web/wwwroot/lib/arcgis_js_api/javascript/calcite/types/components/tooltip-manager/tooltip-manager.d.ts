import { VNode } from "../../stencil-public-runtime";
/**
 * @slot - A slot for adding elements that reference a `calcite-tooltip` by the `selector` property.
 * @deprecated No longer required for tooltip usage.
 */
export declare class TooltipManager {
  /**
   * CSS Selector to match reference elements for tooltips. Reference elements will be identified by this selector in order to open their associated tooltip.
   *
   * @default `[data-calcite-tooltip-reference]`
   */
  selector: string;
  render(): VNode;
}
