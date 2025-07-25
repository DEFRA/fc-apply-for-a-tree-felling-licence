import { VNode } from "../../stencil-public-runtime";
/**
 * @slot - A slot for adding elements that reference a 'calcite-popover' by the 'selector' property.
 * @deprecated No longer required for popover usage.
 */
export declare class PopoverManager {
  el: HTMLCalcitePopoverManagerElement;
  mutationObserver: import("../../utils/observers").ExtendedMutationObserver;
  /**
   * CSS Selector to match reference elements for popovers. Reference elements will be identified by this selector in order to open their associated popover.
   *
   * @default `[data-calcite-popover-reference]`
   */
  selector: string;
  /**
   * Automatically closes any currently open popovers when clicking outside of a popover.
   */
  autoClose: boolean;
  autoCloseHandler(): void;
  connectedCallback(): void;
  disconnectedCallback(): void;
  render(): VNode;
  setAutoClose(): void;
}
