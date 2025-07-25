import { EventEmitter, VNode } from "../../stencil-public-runtime";
import { Scale, Width } from "../interfaces";
import { LabelableComponent } from "../../utils/label";
import { FormComponent } from "../../utils/form";
import { InteractiveComponent } from "../../utils/interactive";
/**
 * @slot - A slot for adding `calcite-option`s.
 */
export declare class Select implements LabelableComponent, FormComponent, InteractiveComponent {
  /**
   * When `true`, interaction is prevented and the component is displayed with lower opacity.
   */
  disabled: boolean;
  /**
   * Accessible name for the component.
   *
   */
  label: string;
  /**
   * Specifies the name of the component on form submission.
   */
  name: string;
  /**
   * When `true`, the component must have a value in order for the form to submit.
   *
   * @internal
   */
  required: boolean;
  /**
   * Specifies the size of the component.
   */
  scale: Scale;
  /** The component's `selectedOption` value. */
  value: string;
  valueHandler(value: string): void;
  /**
   * The component's selected option `HTMLElement`.
   *
   * @readonly
   */
  selectedOption: HTMLCalciteOptionElement;
  selectedOptionHandler(selectedOption: HTMLCalciteOptionElement): void;
  /**
   * Specifies the width of the component.
   */
  width: Width;
  labelEl: HTMLCalciteLabelElement;
  formEl: HTMLFormElement;
  defaultValue: Select["value"];
  el: HTMLCalciteSelectElement;
  private componentToNativeEl;
  private mutationObserver;
  private selectEl;
  connectedCallback(): void;
  disconnectedCallback(): void;
  componentDidLoad(): void;
  componentDidRender(): void;
  /** Sets focus on the component. */
  setFocus(): Promise<void>;
  /**
   * Fires when the `selectedOption` changes.
   */
  calciteSelectChange: EventEmitter<void>;
  private handleInternalSelectChange;
  protected handleOptionOrGroupChange(event: CustomEvent): void;
  onLabelClick(): void;
  private updateNativeElement;
  private populateInternalSelect;
  private clearInternalSelect;
  private storeSelectRef;
  private selectFromNativeOption;
  private toNativeElement;
  private deselectAllExcept;
  private emitChangeEvent;
  renderChevron(): VNode;
  render(): VNode;
}
