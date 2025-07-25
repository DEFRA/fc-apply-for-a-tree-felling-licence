import { EventEmitter, VNode } from "../../stencil-public-runtime";
import { Layout, Scale } from "../interfaces";
import { InteractiveComponent } from "../../utils/interactive";
import { StepperItemChangeEventDetail, StepperItemEventDetail, StepperItemKeyEventDetail } from "../stepper/interfaces";
import { LocalizedComponent } from "../../utils/locale";
/**
 * @slot - A slot for adding custom content.
 */
export declare class StepperItem implements InteractiveComponent, LocalizedComponent {
  el: HTMLCalciteStepperItemElement;
  /**
   *  When `true`, the component is selected.
   *
   * @deprecated Use `selected` instead.
   */
  active: boolean;
  activeHandler(value: boolean): void;
  /**
   * When `true`, the component is selected.
   */
  selected: boolean;
  selectedHandler(value: boolean): void;
  /** When `true`, the step has been completed. */
  complete: boolean;
  /** When `true`, the component contains an error that requires resolution from the user. */
  error: boolean;
  /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
  disabled: boolean;
  /**
   * The component header text.
   *
   * @deprecated use `heading` instead.
   */
  itemTitle?: string;
  /** The component header text. */
  heading?: string;
  /**
   * A description for the component. Displays below the header text.
   *
   * @deprecated use `description` instead.
   */
  itemSubtitle?: string;
  /** A description for the component. Displays below the header text. */
  description: string;
  /** Defines the layout of the component. */
  /** @internal */
  layout?: Extract<"horizontal" | "vertical", Layout>;
  /** When `true`, displays a status icon in the component's heading. */
  /** @internal */
  icon: boolean;
  /** When `true`, displays the step number in the component's heading. */
  /** @internal */
  numbered: boolean;
  /** Specifies the size of the component. */
  /** @internal */
  scale: Scale;
  disabledWatcher(): void;
  effectiveLocale: string;
  effectiveLocaleWatcher(locale: string): void;
  headerEl: HTMLDivElement;
  /**
   * @internal
   */
  calciteInternalStepperItemKeyEvent: EventEmitter<StepperItemKeyEventDetail>;
  /**
   * @internal
   */
  calciteInternalStepperItemSelect: EventEmitter<StepperItemEventDetail>;
  /**
   * @internal
   */
  calciteInternalUserRequestedStepperItemSelect: EventEmitter<StepperItemChangeEventDetail>;
  /**
   * @internal
   */
  calciteInternalStepperItemRegister: EventEmitter<StepperItemEventDetail>;
  connectedCallback(): void;
  componentWillLoad(): void;
  componentDidRender(): void;
  disconnectedCallback(): void;
  render(): VNode;
  updateActiveItemOnChange(event: CustomEvent<StepperItemChangeEventDetail>): void;
  setFocus(): Promise<void>;
  /** position within parent */
  private itemPosition;
  /** the latest requested item position*/
  private selectedPosition;
  /** the parent stepper component */
  private parentStepperEl;
  private keyDownHandler;
  private renderIcon;
  private determineSelectedItem;
  private registerStepperItem;
  private handleItemClick;
  private emitUserRequestedItem;
  private emitRequestedItem;
  private getItemPosition;
}
