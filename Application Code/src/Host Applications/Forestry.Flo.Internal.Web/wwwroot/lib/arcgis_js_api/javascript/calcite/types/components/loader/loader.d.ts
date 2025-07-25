import { VNode } from "../../stencil-public-runtime";
import { Scale } from "../interfaces";
export declare class Loader {
  el: HTMLCalciteLoaderElement;
  /** When `true`, the component is active. */
  active: boolean;
  /** When `true`, displays smaller and appears to the left of the text. */
  inline: boolean;
  /** Accessible name for the component. */
  label: string;
  /** Specifies the size of the component. */
  scale: Scale;
  /**
   * Specifies the component type.
   *
   * Use `"indeterminate"` if finding actual progress value is impossible.
   *
   */
  type: "indeterminate" | "determinate";
  /** The component's value. Valid only for `"determinate"` indicators. Percent complete of 100. */
  value: number;
  /** Text that displays under the component's indicator. */
  text?: string;
  /**
   * Disables spacing around the component.
   *
   * @deprecated Use `--calcite-loader-padding` CSS variable instead.
   */
  noPadding: boolean;
  render(): VNode;
  /**
   * Return the proper sizes based on the scale property
   *
   * @param scale
   */
  private getSize;
  private getInlineSize;
}
