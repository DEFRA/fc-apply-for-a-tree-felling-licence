import { EventEmitter, VNode } from "../../stencil-public-runtime";
import { AccordionAppearance, AccordionSelectionMode, RequestedItem } from "./interfaces";
import { Position, Scale } from "../interfaces";
/**
 * @slot - A slot for adding `calcite-accordion-item`s. `calcite-accordion` cannot be nested, however `calcite-accordion-item`s can.
 */
export declare class Accordion {
  el: HTMLCalciteAccordionElement;
  /** Specifies the appearance of the component. */
  appearance: AccordionAppearance;
  /** Specifies the placement of the icon in the header. */
  iconPosition: Position;
  /** Specifies the type of the icon in the header. */
  iconType: "chevron" | "caret" | "plus-minus";
  /** Specifies the size of the component. */
  scale: Scale;
  /**
   * Specifies the selection mode - "multiple" (allow any number of open items), "single" (allow one open item),
   * or "single-persist" (allow and require one open item).
   */
  selectionMode: AccordionSelectionMode;
  /**
   * @internal
   */
  calciteInternalAccordionChange: EventEmitter<RequestedItem>;
  componentDidLoad(): void;
  render(): VNode;
  calciteInternalAccordionItemKeyEvent(event: CustomEvent): void;
  registerCalciteAccordionItem(event: CustomEvent): void;
  updateActiveItemOnChange(event: CustomEvent): void;
  /** created list of Accordion items */
  private items;
  /** keep track of whether the items have been sorted so we don't re-sort */
  private sorted;
  /** keep track of the requested item for multi mode */
  private requestedAccordionItem;
  private focusFirstItem;
  private focusLastItem;
  private focusNextItem;
  private focusPrevItem;
  private itemIndex;
  private focusElement;
  private sortItems;
}
