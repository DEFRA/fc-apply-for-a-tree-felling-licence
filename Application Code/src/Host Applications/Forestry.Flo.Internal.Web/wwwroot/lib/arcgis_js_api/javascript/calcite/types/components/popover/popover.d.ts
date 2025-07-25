import { EventEmitter, VNode } from "../../stencil-public-runtime";
import { OverlayPositioning, FloatingUIComponent, LogicalPlacement, EffectivePlacement, ReferenceElement } from "../../utils/floating-ui";
import { OpenCloseComponent } from "../../utils/openCloseComponent";
import { HeadingLevel } from "../functional/Heading";
import { Scale } from "../interfaces";
/**
 * @slot - A slot for adding custom content.
 */
export declare class Popover implements FloatingUIComponent, OpenCloseComponent {
  /**
   * When `true`, clicking outside of the component automatically closes open `calcite-popover`s.
   */
  autoClose: boolean;
  /**
   * When `true`, a close button is added to the component.
   *
   * @deprecated use dismissible instead.
   */
  closeButton: boolean;
  /**
   * When `true`, a close button is added to the component.
   *
   * @deprecated use `closable` instead.
   */
  dismissible: boolean;
  handleDismissible(value: boolean): void;
  /** When `true`, display a close button within the component. */
  closable: boolean;
  handleClosable(value: boolean): void;
  /**
   * When `true`, prevents flipping the component's placement when overlapping its `referenceElement`.
   */
  disableFlip: boolean;
  /**
   * When `true`, removes the caret pointer.
   */
  disablePointer: boolean;
  /**
   * Defines the available placements that can be used when a flip occurs.
   */
  flipPlacements?: EffectivePlacement[];
  flipPlacementsHandler(): void;
  /**
   * The component header text.
   */
  heading?: string;
  /**
   * Specifies the number at which section headings should start.
   */
  headingLevel: HeadingLevel;
  /** Accessible name for the component. */
  label: string;
  /**
   * Offsets the position of the component away from the `referenceElement`.
   *
   * @default 6
   */
  offsetDistance: number;
  offsetDistanceOffsetHandler(): void;
  /**
   * Offsets the position of the component along the `referenceElement`.
   */
  offsetSkidding: number;
  offsetSkiddingHandler(): void;
  /**
   * When `true`, displays and positions the component.
   */
  open: boolean;
  openHandler(value: boolean): void;
  /**
   * Determines the type of positioning to use for the overlaid content.
   *
   * Using `"absolute"` will work for most cases. The component will be positioned inside of overflowing parent containers and will affect the container's layout.
   *
   * `"fixed"` value should be used to escape an overflowing parent container, or when the reference element's `position` CSS property is `"fixed"`.
   *
   */
  overlayPositioning: OverlayPositioning;
  overlayPositioningHandler(): void;
  /**
   * Determines where the component will be positioned relative to the `referenceElement`.
   *
   * @see [LogicalPlacement](https://github.com/Esri/calcite-components/blob/master/src/utils/floating-ui.ts#L25)
   */
  placement: LogicalPlacement;
  placementHandler(): void;
  /**
   *  The `referenceElement` used to position the component according to its `placement` value. Setting to an `HTMLElement` is preferred so the component does not need to query the DOM. However, a string `id` of the reference element can also be used.
   */
  referenceElement: ReferenceElement | string;
  referenceElementHandler(): void;
  /** Specifies the size of the component. */
  scale: Scale;
  /**
   * When `true`, disables automatically toggling the component when its `referenceElement` has been triggered.
   *
   * This property can be set to `true` to manage when the component is open.
   */
  triggerDisabled: boolean;
  /**
   * Accessible name for the component's close button.
   *
   * @default "Close"
   */
  intlClose: string;
  filteredFlipPlacements: EffectivePlacement[];
  el: HTMLCalcitePopoverElement;
  effectiveReferenceElement: ReferenceElement;
  arrowEl: HTMLDivElement;
  closeButtonEl: HTMLCalciteActionElement;
  guid: string;
  openTransitionProp: string;
  transitionEl: HTMLDivElement;
  hasLoaded: boolean;
  connectedCallback(): void;
  componentDidLoad(): void;
  disconnectedCallback(): void;
  /** Fires when the component is requested to be closed and before the closing transition begins. */
  calcitePopoverBeforeClose: EventEmitter<void>;
  /** Fires when the component is closed and animation is complete. */
  calcitePopoverClose: EventEmitter<void>;
  /** Fires when the component is added to the DOM but not rendered, and before the opening transition begins. */
  calcitePopoverBeforeOpen: EventEmitter<void>;
  /** Fires when the component is open and animation is complete. */
  calcitePopoverOpen: EventEmitter<void>;
  /**
   * Updates the position of the component.
   *
   * @param delayed
   */
  reposition(delayed?: boolean): Promise<void>;
  /**
   * Sets focus on the component.
   *
   * @param focusId
   */
  setFocus(focusId?: "close-button"): Promise<void>;
  /**
   * Toggles the component's open property.
   *
   * @param value
   */
  toggle(value?: boolean): Promise<void>;
  private setTransitionEl;
  setFilteredPlacements: () => void;
  setUpReferenceElement: (warn?: boolean) => void;
  getId: () => string;
  setExpandedAttr: () => void;
  addReferences: () => void;
  removeReferences: () => void;
  getReferenceElement(): ReferenceElement;
  hide: () => void;
  onBeforeOpen(): void;
  onOpen(): void;
  onBeforeClose(): void;
  onClose(): void;
  storeArrowEl: (el: HTMLDivElement) => void;
  renderCloseButton(): VNode;
  renderHeader(): VNode;
  render(): VNode;
}
