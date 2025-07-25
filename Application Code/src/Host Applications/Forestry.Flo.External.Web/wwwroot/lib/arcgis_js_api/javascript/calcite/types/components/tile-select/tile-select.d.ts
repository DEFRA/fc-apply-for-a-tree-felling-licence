import { EventEmitter, VNode } from "../../stencil-public-runtime";
import { Alignment, Width } from "../interfaces";
import { TileSelectType } from "./interfaces";
import { InteractiveComponent } from "../../utils/interactive";
/**
 * @slot - A slot for adding custom content.
 */
export declare class TileSelect implements InteractiveComponent {
  el: HTMLCalciteTileSelectElement;
  /** When `true`, the component is checked. */
  checked: boolean;
  checkedChanged(newChecked: boolean): void;
  /** A description for the component, which displays below the heading. */
  description?: string;
  /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
  disabled: boolean;
  /** The component header text, which displays between the icon and description. */
  heading?: string;
  /** When `true`, the component is not displayed and is not focusable or checkable. */
  hidden: boolean;
  /** Specifies an icon to display. */
  icon?: string;
  /** Specifies the name of the component on form submission. */
  name: any;
  nameChanged(newName: string): void;
  /** When `true`, displays an interactive input based on the `type` property. */
  inputEnabled: boolean;
  /** When `inputEnabled` is `true`, specifies the placement of the interactive input on the component. */
  inputAlignment: Extract<"end" | "start", Alignment>;
  /**
   * The selection mode of the component.
   *
   * Use radio for single selection, and checkbox for multiple selections.
   */
  type: TileSelectType;
  /** The component's value. */
  value?: any;
  /** Specifies the width of the component. */
  width: Extract<"auto" | "full", Width>;
  private input;
  guid: string;
  /** The focused state of the tile-select. */
  focused: boolean;
  /**
   * Emits a custom change event.
   *
   * For checkboxes it emits when checked or unchecked.
   *
   * For radios it only emits when checked.
   */
  calciteTileSelectChange: EventEmitter<void>;
  /** Sets focus on the component. */
  setFocus(): Promise<void>;
  checkboxChangeHandler(event: CustomEvent): void;
  checkboxFocusBlurHandler(event: CustomEvent): void;
  radioButtonChangeHandler(event: CustomEvent): void;
  radioButtonCheckedChangeHandler(event: CustomEvent): void;
  radioButtonFocusBlurHandler(event: CustomEvent): void;
  click(event: MouseEvent): void;
  mouseenter(): void;
  mouseleave(): void;
  connectedCallback(): void;
  disconnectedCallback(): void;
  componentDidRender(): void;
  private renderInput;
  render(): VNode;
}
