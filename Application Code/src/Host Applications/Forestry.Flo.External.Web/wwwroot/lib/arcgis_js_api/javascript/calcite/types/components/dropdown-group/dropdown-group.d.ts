import { EventEmitter, VNode } from "../../stencil-public-runtime";
import { RequestedItem, SelectionMode } from "./interfaces";
import { Scale } from "../interfaces";
/**
 * @slot - A slot for adding `calcite-dropdown-item` components.
 */
export declare class DropdownGroup {
  el: HTMLCalciteDropdownGroupElement;
  /** optionally set a group title for display */
  groupTitle?: string;
  /**
    specify the selection mode - multiple (allow any number of (or no) active items), single (allow and require one active item),
   none (no active items), defaults to single
   */
  selectionMode: SelectionMode;
  /**
   * Specifies the size of the action.
   */
  scale: Scale;
  /**
   * @internal
   */
  calciteInternalDropdownItemChange: EventEmitter<RequestedItem>;
  componentWillLoad(): void;
  render(): VNode;
  updateActiveItemOnChange(event: CustomEvent): void;
  /** position of group within a dropdown */
  private groupPosition;
  /** the requested group */
  private requestedDropdownGroup;
  /** the requested item */
  private requestedDropdownItem;
  private getGroupPosition;
}
