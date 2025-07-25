import { EventEmitter, VNode } from "../../stencil-public-runtime";
import { Scale } from "../interfaces";
import { InteractiveComponent } from "../../utils/interactive";
export declare class Filter implements InteractiveComponent {
  /**
   * The items to filter through. The filter uses this as the starting point, and returns items
   * that contain the string entered in the input, using a partial match and recursive search.
   *
   * This property is required.
   */
  items: object[];
  watchItemsHandler(): void;
  /**
   * When true, disabled prevents interaction. This state shows items with lower opacity/grayed.
   */
  disabled: boolean;
  /**
   * The resulting items after filtering.
   *
   * @readonly
   */
  filteredItems: object[];
  /**
   * A text label that will appear on the clear button.
   */
  intlClear?: string;
  /**
   * A text label that will appear next to the input field.
   */
  intlLabel?: string;
  /**
   * Placeholder text for the input element's placeholder attribute
   */
  placeholder?: string;
  /** specify the scale of filter, defaults to m */
  scale: Scale;
  /**
   * Filter value.
   */
  value: string;
  valueHandler(value: string): void;
  el: HTMLCalciteFilterElement;
  textInput: HTMLCalciteInputElement;
  componentDidRender(): void;
  /**
   * This event fires when the filter text changes.
   */
  calciteFilterChange: EventEmitter<void>;
  componentWillLoad(): void;
  /** Sets focus on the component. */
  setFocus(): Promise<void>;
  private filter;
  inputHandler: (event: CustomEvent) => void;
  keyDownHandler: (event: KeyboardEvent) => void;
  clear: () => void;
  updateFiltered(filtered: any[], emit?: boolean): void;
  render(): VNode;
}
