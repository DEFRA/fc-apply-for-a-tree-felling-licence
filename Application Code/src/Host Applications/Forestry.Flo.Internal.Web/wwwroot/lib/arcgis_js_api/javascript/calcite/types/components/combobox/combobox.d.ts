import { Event, EventEmitter, VNode } from "../../stencil-public-runtime";
import { OverlayPositioning, FloatingUIComponent, LogicalPlacement, EffectivePlacement } from "../../utils/floating-ui";
import { DeprecatedEventPayload, Scale } from "../interfaces";
import { ComboboxSelectionMode, ComboboxChildElement } from "./interfaces";
import { LabelableComponent } from "../../utils/label";
import { FormComponent } from "../../utils/form";
import { InteractiveComponent } from "../../utils/interactive";
import { OpenCloseComponent } from "../../utils/openCloseComponent";
interface ItemData {
  label: string;
  value: string;
}
/**
 * @slot - A slot for adding `calcite-combobox-item`s.
 */
export declare class Combobox implements LabelableComponent, FormComponent, InteractiveComponent, OpenCloseComponent, FloatingUIComponent {
  el: HTMLCalciteComboboxElement;
  /**
   * When `true`, displays and positions the component.
   *
   * @deprecated use `open` instead.
   */
  active: boolean;
  activeHandler(value: boolean): void;
  /**When `true`, displays and positions the component. */
  open: boolean;
  openHandler(value: boolean): void;
  /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
  disabled: boolean;
  handleDisabledChange(value: boolean): void;
  /** Accessible name for the component. */
  label: string;
  /** Specifies the placeholder text for the input. */
  placeholder?: string;
  /** Specifies the placeholder icon for the input. */
  placeholderIcon?: string;
  /** Specifies the maximum number of `calcite-combobox-item`s (including nested children) to display before displaying a scrollbar. */
  maxItems: number;
  maxItemsHandler(): void;
  /** Specifies the name of the component on form submission. */
  name: string;
  /** When `true`, allows entry of custom values, which are not in the original set of items. */
  allowCustomValues: boolean;
  /**
   * Determines the type of positioning to use for the overlaid content.
   *
   * Using `"absolute"` will work for most cases. The component will be positioned inside of overflowing parent containers and will affect the container's layout.
   *
   * `"fixed"` should be used to escape an overflowing parent container, or when the reference element's `position` CSS property is `"fixed"`.
   *
   */
  overlayPositioning: OverlayPositioning;
  overlayPositioningHandler(): void;
  /**
   * When `true`, the component must have a value in order for the form to submit.
   *
   * @internal
   */
  required: boolean;
  /**
   * specify the selection mode
   * - multiple: allow any number of selected items (default)
   * - single: only one selection)
   * - ancestors: like multiple, but show ancestors of selected items as selected, only deepest children shown in chips
   */
  selectionMode: ComboboxSelectionMode;
  /** Specifies the size of the component. */
  scale: Scale;
  /** The component's value(s) from the selected `calcite-combobox-item`(s). */
  value: string | string[];
  valueHandler(value: string | string[]): void;
  /**
   * Accessible name for the component's remove tag when a `calcite-combobox-item` is selected.
   *
   * @default "Remove tag"
   */
  intlRemoveTag: string;
  /**
   * Defines the available placements that can be used when a flip occurs.
   */
  flipPlacements?: EffectivePlacement[];
  flipPlacementsHandler(): void;
  documentClickHandler(event: PointerEvent): void;
  calciteComboboxItemChangeHandler(event: CustomEvent<HTMLCalciteComboboxItemElement>): void;
  /**
   * Updates the position of the component.
   *
   * @param delayed
   */
  reposition(delayed?: boolean): Promise<void>;
  /** Sets focus on the component. */
  setFocus(): Promise<void>;
  /**
   * Fires when the selected items set changes.
   *
   * @deprecated use `calciteComboboxChange` instead.
   */
  calciteLookupChange: EventEmitter<HTMLCalciteComboboxItemElement[]>;
  /**
   * Fires when the selected item(s) changes.
   */
  calciteComboboxChange: EventEmitter<{
    selectedItems: HTMLCalciteComboboxItemElement[];
  }>;
  /** Fires when text is added to filter the options list. */
  calciteComboboxFilterChange: EventEmitter<{
    visibleItems: HTMLCalciteComboboxItemElement[];
    text: string;
  }>;
  /**
   * Fires when a selected item in the component is dismissed via its `calcite-chip`.
   *
   * **Note:**: The event payload is deprecated, please use the `value` property on the component to determine the removed value instead.
   */
  calciteComboboxChipDismiss: EventEmitter<DeprecatedEventPayload>;
  /** Fires when the component is requested to be closed, and before the closing transition begins. */
  calciteComboboxBeforeClose: EventEmitter<void>;
  /** Fires when the component is closed and animation is complete. */
  calciteComboboxClose: EventEmitter<void>;
  /** Fires when the component is added to the DOM but not rendered, and before the opening transition begins. */
  calciteComboboxBeforeOpen: EventEmitter<void>;
  /** Fires when the component is open and animation is complete. */
  calciteComboboxOpen: EventEmitter<void>;
  connectedCallback(): void;
  componentWillLoad(): void;
  componentDidLoad(): void;
  componentDidRender(): void;
  disconnectedCallback(): void;
  placement: LogicalPlacement;
  filteredFlipPlacements: EffectivePlacement[];
  internalValueChangeFlag: boolean;
  labelEl: HTMLCalciteLabelElement;
  formEl: HTMLFormElement;
  defaultValue: Combobox["value"];
  items: HTMLCalciteComboboxItemElement[];
  groupItems: HTMLCalciteComboboxItemGroupElement[];
  selectedItems: HTMLCalciteComboboxItemElement[];
  selectedItemsHandler(): void;
  visibleItems: HTMLCalciteComboboxItemElement[];
  needsIcon: boolean;
  activeItemIndex: number;
  activeChipIndex: number;
  activeDescendant: string;
  text: string;
  /** when search text is cleared, reset active to  */
  textHandler(): void;
  textInput: HTMLInputElement;
  data: ItemData[];
  mutationObserver: import("../../utils/observers").ExtendedMutationObserver;
  resizeObserver: ResizeObserver;
  private guid;
  private inputHeight;
  private floatingEl;
  private referenceEl;
  private listContainerEl;
  private ignoreSelectedEventsFlag;
  openTransitionProp: string;
  transitionEl: HTMLDivElement;
  setFilteredPlacements: () => void;
  getValue: () => string | string[];
  onLabelClick: () => void;
  private comboboxInViewport;
  keydownHandler: (event: KeyboardEvent) => void;
  private toggleCloseEnd;
  private toggleOpenEnd;
  onBeforeOpen(): void;
  onOpen(): void;
  onBeforeClose(): void;
  onClose(): void;
  setMaxScrollerHeight: () => Promise<void>;
  calciteChipDismissHandler: (event: CustomEvent<HTMLCalciteChipElement>, comboboxItem: HTMLCalciteComboboxItemElement) => void;
  clickHandler: (event: MouseEvent) => void;
  setInactiveIfNotContained: (event: Event) => void;
  setFloatingEl: (el: HTMLDivElement) => void;
  setContainerEl: (el: HTMLDivElement) => void;
  setReferenceEl: (el: HTMLDivElement) => void;
  private getMaxScrollerHeight;
  private calculateSingleItemHeight;
  inputHandler: (event: Event) => void;
  getCombinedItems(): ComboboxChildElement[];
  private filterItems;
  internalCalciteLookupChangeEvent: () => void;
  private emitCalciteLookupChange;
  internalComboboxChangeEvent: () => void;
  private emitComboboxChange;
  toggleSelection(item: HTMLCalciteComboboxItemElement, value?: boolean): void;
  updateAncestors(item: HTMLCalciteComboboxItemElement): void;
  getVisibleItems(): HTMLCalciteComboboxItemElement[];
  getSelectedItems(): HTMLCalciteComboboxItemElement[];
  updateItems: () => void;
  getData(): ItemData[];
  getNeedsIcon(): boolean;
  resetText(): void;
  getItems(): HTMLCalciteComboboxItemElement[];
  getGroupItems(): HTMLCalciteComboboxItemGroupElement[];
  addCustomChip(value: string, focus?: boolean): void;
  removeActiveChip(): void;
  removeLastChip(): void;
  previousChip(): void;
  nextChip(): void;
  focusChip(): void;
  private scrollToActiveItem;
  shiftActiveItemIndex(delta: number): void;
  updateActiveItemIndex(index: number): void;
  isMulti(): boolean;
  comboboxFocusHandler: () => void;
  comboboxBlurHandler: (event: FocusEvent) => void;
  renderChips(): VNode[];
  renderInput(): VNode;
  renderListBoxOptions(): VNode[];
  renderFloatingUIContainer(): VNode;
  renderIconStart(): VNode;
  renderIconEnd(): VNode;
  render(): VNode;
}
export {};
