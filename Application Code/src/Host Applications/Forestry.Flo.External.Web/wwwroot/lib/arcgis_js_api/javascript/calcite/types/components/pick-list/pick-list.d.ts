import { EventEmitter, VNode } from "../../stencil-public-runtime";
import { ICON_TYPES } from "./resources";
import { ListFocusId, ItemData } from "./shared-list-logic";
import { HeadingLevel } from "../functional/Heading";
import { InteractiveComponent } from "../../utils/interactive";
/**
 * @slot - A slot for adding `calcite-pick-list-item` or `calcite-pick-list-group` elements. Items are displayed as a vertical list.
 * @slot menu-actions - A slot for adding a button and menu combination for performing actions, such as sorting.
 */
export declare class PickList<ItemElement extends HTMLCalcitePickListItemElement = HTMLCalcitePickListItemElement> implements InteractiveComponent {
  /**
   * When `true`, interaction is prevented and the component is displayed with lower opacity.
   */
  disabled: boolean;
  /**
   * When `true`, an input appears at the top of the list that can be used by end users to filter items in the list.
   */
  filterEnabled: boolean;
  /**
   * Placeholder text for the filter input field.
   */
  filterPlaceholder: string;
  /**
   * Specifies the number at which section headings should start.
   */
  headingLevel: HeadingLevel;
  /**
   * When `true`, a busy indicator is displayed.
   */
  loading: boolean;
  /**
   * Similar to standard radio buttons and checkboxes.
   * When `true`, a user can select multiple `calcite-pick-list-item`s at a time.
   * When `false`, only a single `calcite-pick-list-item` can be selected at a time,
   * and a new selection will deselect previous selections.
   */
  multiple: boolean;
  /**
   * When `true` and single selection is enabled, the selection changes when navigating `calcite-pick-list-item`s via keyboard.
   */
  selectionFollowsFocus: boolean;
  selectedValues: Map<string, ItemElement>;
  dataForFilter: ItemData;
  items: ItemElement[];
  lastSelectedItem: ItemElement;
  mutationObserver: import("../../utils/observers").ExtendedMutationObserver;
  el: HTMLCalcitePickListElement;
  emitCalciteListChange: () => void;
  filterEl: HTMLCalciteFilterElement;
  connectedCallback(): void;
  disconnectedCallback(): void;
  componentDidRender(): void;
  /**
   * Emits when any of the `calcite-pick-list-item` selections have changed.
   */
  calciteListChange: EventEmitter<Map<string, HTMLCalcitePickListItemElement>>;
  calciteListItemRemoveHandler(event: CustomEvent<void>): void;
  calciteListItemChangeHandler(event: CustomEvent): void;
  calciteInternalListItemPropsChangeHandler(event: CustomEvent): void;
  calciteInternalListItemValueChangeHandler(event: CustomEvent): void;
  calciteListFocusOutHandler(event: FocusEvent): void;
  setUpItems(): void;
  setUpFilter(): void;
  setFilterEl: (el: HTMLCalciteFilterElement) => void;
  deselectRemovedItems: any;
  deselectSiblingItems: any;
  selectSiblings: any;
  handleFilter: any;
  getItemData: any;
  keyDownHandler: any;
  /** Returns the component's selected `calcite-pick-list-item`s. */
  getSelectedItems(): Promise<Map<string, HTMLCalcitePickListItemElement>>;
  /**
   * Sets focus on the component.
   *
   * @param focusId
   */
  setFocus(focusId?: ListFocusId): Promise<void>;
  getIconType(): ICON_TYPES;
  render(): VNode;
}
