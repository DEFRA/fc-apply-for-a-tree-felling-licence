import { VNode } from "../../stencil-public-runtime";
import { Position, Scale } from "../interfaces";
import { ConditionalSlotComponent } from "../../utils/conditionalSlot";
/**
 * @slot - A slot for adding content to the `calcite-shell-panel`.
 * @slot action-bar - A slot for adding a `calcite-action-bar` to the `calcite-shell-panel`.
 */
export declare class ShellCenterRow implements ConditionalSlotComponent {
  /**
   * When `true`, the content area displays like a floating panel.
   */
  detached: boolean;
  /**
   * Specifies the maximum height of the component.
   */
  heightScale: Scale;
  /**
   * Specifies the component's position. Will be flipped when the element direction is right-to-left (`"rtl"`).
   */
  position: Position;
  el: HTMLCalciteShellCenterRowElement;
  connectedCallback(): void;
  disconnectedCallback(): void;
  render(): VNode;
}
