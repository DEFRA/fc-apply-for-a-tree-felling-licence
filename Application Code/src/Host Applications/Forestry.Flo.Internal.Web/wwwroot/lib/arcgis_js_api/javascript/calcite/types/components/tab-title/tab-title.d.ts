import { EventEmitter, VNode } from "../../stencil-public-runtime";
import { TabChangeEventDetail } from "../tab/interfaces";
import { TabID, TabLayout, TabPosition } from "../tabs/interfaces";
import { FlipContext, Scale } from "../interfaces";
import { InteractiveComponent } from "../../utils/interactive";
/**
 * @slot - A slot for adding text.
 */
export declare class TabTitle implements InteractiveComponent {
  el: HTMLCalciteTabTitleElement;
  /**
   * When `true`, the component and its respective `calcite-tab` contents are selected.
   *
   * Only one tab can be selected within the `calcite-tabs` parent.
   *
   * @deprecated Use `selected` instead.
   */
  active: boolean;
  activeHandler(value: boolean): void;
  /**
   * When `true`, the component and its respective `calcite-tab` contents are selected.
   *
   * Only one tab can be selected within the `calcite-tabs` parent.
   */
  selected: boolean;
  selectedHandler(value: boolean): void;
  /** When `true`, interaction is prevented and the component is displayed with lower opacity.  */
  disabled: boolean;
  /** Specifies an icon to display at the end of the component. */
  iconEnd?: string;
  /** When `true`, the icon will be flipped when the element direction is right-to-left (`"rtl"`). */
  iconFlipRtl?: FlipContext;
  /** Specifies an icon to display at the start of the component. */
  iconStart?: string;
  /**
   * @internal
   */
  layout: TabLayout;
  /**
   * @internal
   */
  position: TabPosition;
  /**
   * @internal
   */
  scale: Scale;
  /**
   * @internal
   */
  bordered: boolean;
  /**
   * Specifies a unique name for the component.
   *
   * When specified, use the same value on the `calcite-tab`.
   */
  tab?: string;
  connectedCallback(): void;
  disconnectedCallback(): void;
  componentWillLoad(): void;
  componentWillRender(): void;
  render(): VNode;
  componentDidLoad(): Promise<void>;
  componentDidRender(): void;
  internalTabChangeHandler(event: CustomEvent<TabChangeEventDetail>): void;
  onClick(): void;
  keyDownHandler(event: KeyboardEvent): void;
  /**
   * Fires when a `calcite-tab` is selected. Emits the `tab` property, or the index position.
   *
   * @see [TabChangeEventDetail](https://github.com/Esri/calcite-components/blob/master/src/components/tab/interfaces.ts#L1)
   */
  calciteTabsActivate: EventEmitter<TabChangeEventDetail>;
  /**
   * Fires when a `calcite-tab` is selected (`event.details`).
   *
   * @see [TabChangeEventDetail](https://github.com/Esri/calcite-components/blob/master/src/components/tab/interfaces.ts#L1)
   * @internal
   */
  calciteInternalTabsActivate: EventEmitter<TabChangeEventDetail>;
  /**
   * @internal
   */
  calciteInternalTabsFocusNext: EventEmitter<void>;
  /**
   * @internal
   */
  calciteInternalTabsFocusPrevious: EventEmitter<void>;
  /**
   * @internal
   */
  calciteInternalTabTitleRegister: EventEmitter<TabID>;
  /**
   * @internal
   */
  calciteInternalTabIconChanged: EventEmitter<void>;
  /**
   * Returns the index of the title within the `calcite-tab-nav`.
   */
  getTabIndex(): Promise<number>;
  /**
   * @internal
   */
  getTabIdentifier(): Promise<TabID>;
  /**
   * @param tabIds
   * @param titleIds
   * @internal
   */
  updateAriaInfo(tabIds?: string[], titleIds?: string[]): Promise<void>;
  /** watches for changing text content */
  mutationObserver: MutationObserver;
  controls: string;
  /** determine if there is slotted text for styling purposes */
  hasText: boolean;
  parentTabNavEl: HTMLCalciteTabNavElement;
  parentTabsEl: HTMLCalciteTabsElement;
  containerEl: HTMLDivElement;
  resizeObserver: ResizeObserver;
  updateHasText(): void;
  setupTextContentObserver(): void;
  emitActiveTab(userTriggered?: boolean): void;
  guid: string;
}
