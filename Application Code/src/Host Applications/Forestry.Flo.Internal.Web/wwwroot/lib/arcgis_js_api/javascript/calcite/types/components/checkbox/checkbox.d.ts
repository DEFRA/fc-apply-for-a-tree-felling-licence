import { EventEmitter, VNode } from "../../stencil-public-runtime";
import { Scale } from "../interfaces";
import { CheckableFormComponent } from "../../utils/form";
import { LabelableComponent } from "../../utils/label";
import { InteractiveComponent } from "../../utils/interactive";
export declare class Checkbox implements LabelableComponent, CheckableFormComponent, InteractiveComponent {
  el: HTMLCalciteCheckboxElement;
  /** When `true`, the component is checked. */
  checked: boolean;
  /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
  disabled: boolean;
  /** The `id` attribute of the component. When omitted, a globally unique identifier is used. */
  guid: string;
  /**
   * The hovered state of the checkbox.
   *
   * @internal
   */
  hovered: boolean;
  /**
   * When `true`, the component is initially indeterminate, which is independent from its `checked` value.
   *
   * The state is visual only, and can look different across browsers.
   *
   * @mdn [indeterminate](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input/checkbox#indeterminate_state_checkboxes)
   */
  indeterminate: boolean;
  /**
   * Accessible name for the component.
   *
   * @internal
   */
  label?: string;
  /** Specifies the name of the component on form submission. */
  name: any;
  /**
   * When `true`, the component must have a value in order for the form to submit.
   *
   * @internal
   */
  required: boolean;
  /** Specifies the size of the component. */
  scale: Scale;
  /** The component's value. */
  value: any;
  readonly checkedPath = "M5.5 12L2 8.689l.637-.636L5.5 10.727l8.022-7.87.637.637z";
  readonly indeterminatePath = "M13 8v1H3V8z";
  labelEl: HTMLCalciteLabelElement;
  formEl: HTMLFormElement;
  defaultChecked: boolean;
  defaultValue: Checkbox["checked"];
  toggleEl: HTMLDivElement;
  /** Sets focus on the component. */
  setFocus(): Promise<void>;
  getPath: () => string;
  toggle: () => void;
  keyDownHandler: (event: KeyboardEvent) => void;
  clickHandler: () => void;
  /**
   * Emits when the component is blurred.
   *
   * @internal
   */
  calciteInternalCheckboxBlur: EventEmitter<boolean>;
  /** Emits when the component's `checked` status changes. */
  calciteCheckboxChange: EventEmitter<void>;
  /**
   * Emits when the component is focused.
   *
   * @internal
   */
  calciteInternalCheckboxFocus: EventEmitter<boolean>;
  onToggleBlur: () => void;
  onToggleFocus: () => void;
  onLabelClick: () => void;
  connectedCallback(): void;
  disconnectedCallback(): void;
  componentDidRender(): void;
  render(): VNode;
}
