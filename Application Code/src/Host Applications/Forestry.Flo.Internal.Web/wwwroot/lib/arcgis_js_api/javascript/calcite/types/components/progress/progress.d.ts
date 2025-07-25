import { VNode } from "../../stencil-public-runtime";
export declare class Progress {
  el: HTMLCalciteProgressElement;
  /**
   * Specifies the component type.
   *
   * Use `"indeterminate"` if finding actual progress value is impossible.
   *
   */
  type: "indeterminate" | "determinate";
  /** The component's progress value, with a range of 0.0 - 1.0. */
  value: number;
  /** Accessible name for the component. */
  label: string;
  /** Text that displays under the component's indicator. */
  text: string;
  /** When `true` and for `"indeterminate"` progress bars, reverses the animation direction. */
  reversed: boolean;
  render(): VNode;
}
