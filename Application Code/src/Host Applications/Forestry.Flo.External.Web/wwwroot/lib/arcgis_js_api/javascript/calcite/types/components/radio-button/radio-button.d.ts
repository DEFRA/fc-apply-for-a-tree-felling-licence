import { EventEmitter, VNode } from "../../stencil-public-runtime";
import { Scale } from "../interfaces";
import { LabelableComponent } from "../../utils/label";
import { CheckableFormComponent } from "../../utils/form";
import { InteractiveComponent } from "../../utils/interactive";
export declare class RadioButton implements LabelableComponent, CheckableFormComponent, InteractiveComponent {
  el: HTMLCalciteRadioButtonElement;
  /** When `true`, the component is checked. */
  checked: boolean;
  checkedChanged(newChecked: boolean): void;
  /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
  disabled: boolean;
  /**
   * The focused state of the component.
   *
   * @internal
   */
  focused: boolean;
  /** The `id` of the component. When omitted, a globally unique identifier is used. */
  guid: string;
  /** When `true`, the component is not displayed and is not focusable or checkable. */
  hidden: boolean;
  /**
   * The hovered state of the component.
   *
   * @internal
   */
  hovered: boolean;
  /**
   * Accessible name for the component.
   *
   * @internal
   */
  label?: string;
  /** Specifies the name of the component, passed from the `calcite-radio-button-group` on form submission. */
  name: string;
  nameChanged(): void;
  /** When `true`, the component must have a value selected from the `calcite-radio-button-group` in order for the form to submit. */
  required: boolean;
  /** Specifies the size of the component inherited from the `calcite-radio-button-group`. */
  scale: Scale;
  /** The component's value. */
  value: any;
  labelEl: HTMLCalciteLabelElement;
  formEl: HTMLFormElement;
  defaultChecked: boolean;
  defaultValue: RadioButton["value"];
  rootNode: HTMLElement;
  containerEl: HTMLDivElement;
  /** Sets focus on the component. */
  setFocus(): Promise<void>;
  selectItem: (items: HTMLCalciteRadioButtonElement[], selectedIndex: number) => void;
  queryButtons: () => HTMLCalciteRadioButtonElement[];
  isDefaultSelectable: () => boolean;
  check: () => void;
  private clickHandler;
  onLabelClick(event: CustomEvent): void;
  private checkLastRadioButton;
  /** @internal */
  emitCheckedChange(): Promise<void>;
  private setContainerEl;
  private uncheckAllRadioButtonsInGroup;
  private uncheckOtherRadioButtonsInGroup;
  private getTabIndex;
  /**
   * Fires when the radio button is blurred.
   *
   * @internal
   */
  calciteInternalRadioButtonBlur: EventEmitter<void>;
  /**
   * Fires only when the radio button is checked.  This behavior is identical to the native HTML input element.
   * Since this event does not fire when the radio button is unchecked, it's not recommended to attach a listener for this event
   * directly on the element, but instead either attach it to a node that contains all of the radio buttons in the group
   * or use the `calciteRadioButtonGroupChange` event if using this with `calcite-radio-button-group`.
   */
  calciteRadioButtonChange: EventEmitter<void>;
  /**
   * Fires when the checked property changes.  This is an internal event used for styling purposes only.
   * Use calciteRadioButtonChange or calciteRadioButtonGroupChange for responding to changes in the checked value for forms.
   *
   * @internal
   */
  calciteInternalRadioButtonCheckedChange: EventEmitter<void>;
  /**
   * Fires when the radio button is focused.
   *
   * @internal
   */
  calciteInternalRadioButtonFocus: EventEmitter<void>;
  mouseenter(): void;
  mouseleave(): void;
  handleKeyDown: (event: KeyboardEvent) => void;
  private onContainerBlur;
  private onContainerFocus;
  connectedCallback(): void;
  componentDidLoad(): void;
  disconnectedCallback(): void;
  componentDidRender(): void;
  render(): VNode;
}
