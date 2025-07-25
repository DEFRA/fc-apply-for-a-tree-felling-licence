import { VNode, EventEmitter } from "../../stencil-public-runtime";
import { Layout, Scale } from "../interfaces";
/**
 * @slot - A slot for adding `calcite-radio-button`s.
 */
export declare class RadioButtonGroup {
  el: HTMLCalciteRadioButtonGroupElement;
  /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
  disabled: boolean;
  onDisabledChange(): void;
  /** When `true`, the component is not displayed and its `calcite-radio-button`s are not focusable or checkable. */
  hidden: boolean;
  onHiddenChange(): void;
  /** Defines the layout of the component. */
  layout: Layout;
  onLayoutChange(): void;
  /** Specifies the name of the component on form submission. Must be unique to other component instances. */
  name: string;
  /** When `true`, the component must have a value in order for the form to submit. */
  required: boolean;
  /** Specifies the size of the component. */
  scale: Scale;
  onScaleChange(): void;
  mutationObserver: import("../../utils/observers").ExtendedMutationObserver;
  connectedCallback(): void;
  disconnectedCallback(): void;
  private passPropsToRadioButtons;
  /**
   * Fires when the component has changed.
   */
  calciteRadioButtonGroupChange: EventEmitter<any>;
  radioButtonChangeHandler(event: CustomEvent): void;
  render(): VNode;
}
