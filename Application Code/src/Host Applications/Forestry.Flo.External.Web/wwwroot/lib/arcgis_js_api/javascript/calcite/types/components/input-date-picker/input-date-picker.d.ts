import { VNode, EventEmitter } from "../../stencil-public-runtime";
import { HeadingLevel } from "../functional/Heading";
import { LabelableComponent } from "../../utils/label";
import { FormComponent } from "../../utils/form";
import { OverlayPositioning, FloatingUIComponent, EffectivePlacement, MenuPlacement } from "../../utils/floating-ui";
import { DateRangeChange } from "../date-picker/interfaces";
import { InteractiveComponent } from "../../utils/interactive";
import { OpenCloseComponent } from "../../utils/openCloseComponent";
import { LocalizedComponent, NumberingSystem } from "../../utils/locale";
export declare class InputDatePicker implements LabelableComponent, FormComponent, InteractiveComponent, OpenCloseComponent, FloatingUIComponent, LocalizedComponent {
  el: HTMLCalciteInputDatePickerElement;
  /**
   * When `true`, interaction is prevented and the component is displayed with lower opacity.
   */
  disabled: boolean;
  /**
   * When `true`, the component's value can be read, but controls are not accessible and the value cannot be modified.
   *
   * @mdn [readOnly](https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/readonly)
   */
  readOnly: boolean;
  handleDisabledAndReadOnlyChange(value: boolean): void;
  /** Selected date as a string in ISO format (YYYY-MM-DD) */
  value: string | string[];
  valueWatcher(newValue: string | string[]): void;
  valueAsDateWatcher(valueAsDate: Date): void;
  /**
   * Defines the available placements that can be used when a flip occurs.
   */
  flipPlacements?: EffectivePlacement[];
  flipPlacementsHandler(): void;
  /**
   * Specifies the number at which section headings should start.
   */
  headingLevel: HeadingLevel;
  /** The component's value as a full date object. */
  valueAsDate?: Date | Date[];
  /**
   * The component's start date as a full date object.
   *
   * @deprecated use `valueAsDate` instead.
   */
  startAsDate?: Date;
  /**
   * The component's end date as a full date object.
   *
   * @deprecated use `valueAsDate` instead.
   */
  endAsDate?: Date;
  /** Specifies the earliest allowed date as a full date object. */
  minAsDate?: Date;
  /** Specifies the latest allowed date as a full date object. */
  maxAsDate?: Date;
  /** Specifies the earliest allowed date ("yyyy-mm-dd"). */
  min?: string;
  onMinChanged(min: string): void;
  /** Specifies the latest allowed date ("yyyy-mm-dd"). */
  max?: string;
  onMaxChanged(max: string): void;
  /**
   * When `true`, the component is active.
   *
   * @deprecated use `open` instead.
   */
  active: boolean;
  activeHandler(value: boolean): void;
  /** When `true`, displays the `calcite-date-picker` component. */
  open: boolean;
  openHandler(value: boolean): void;
  /**
   * Specifies the name of the component on form submission.
   */
  name: string;
  /**
   * Accessible name for the component's previous month button.
   *
   * @default "Previous month"
   */
  intlPrevMonth?: string;
  /**
   * Accessible name for the component's next month button.
   *
   * @default "Next month"
   */
  intlNextMonth?: string;
  /**
   * Accessible name for the component's year input.
   *
   * @default "Year"
   */
  intlYear?: string;
  /**
   * Specifies the BCP 47 language tag for the desired language and country format.
   *
   * @deprecated set the global `lang` attribute on the element instead.
   * @mdn [lang](https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/lang)
   */
  locale?: string;
  /**
   * Specifies the Unicode numeral system used by the component for localization. This property cannot be dynamically changed.
   *
   */
  numberingSystem?: NumberingSystem;
  /** Specifies the size of the component. */
  scale: "s" | "m" | "l";
  /**
   * Specifies the placement of the `calcite-date-picker` relative to the component.
   *
   * @default "bottom-start"
   */
  placement: MenuPlacement;
  /** When `true`, activates a range for the component. */
  range: boolean;
  /**
   * When `true`, the component must have a value in order for the form to submit.
   *
   * @internal
   */
  required: boolean;
  /**
   * The component's start date.
   *
   * @deprecated use `value` instead.
   */
  start?: string;
  /**
   * The component's end date.
   *
   * @deprecated use `value` instead.
   */
  end?: string;
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
   * When `true`, disables the default behavior on the third click of narrowing or extending the range.
   * Instead starts a new range.
   */
  proximitySelectionDisabled: boolean;
  /** Defines the layout of the component. */
  layout: "horizontal" | "vertical";
  calciteDaySelectHandler(): void;
  private calciteInternalInputInputHandler;
  private calciteInternalInputBlurHandler;
  /**
   * Fires when a user changes the date.
   *
   * @deprecated use `calciteInputDatePickerChange` instead.
   */
  calciteDatePickerChange: EventEmitter<Date>;
  /**
   * Fires when a user changes the date range.
   *
   * @see [DateRangeChange](https://github.com/Esri/calcite-components/blob/master/src/components/date-picker/interfaces.ts#L1)
   * @deprecated use `calciteInputDatePickerChange` instead.
   */
  calciteDatePickerRangeChange: EventEmitter<DateRangeChange>;
  /**
   * Fires when the component's value changes.
   */
  calciteInputDatePickerChange: EventEmitter<void>;
  /** Fires when the component is requested to be closed and before the closing transition begins. */
  calciteInputDatePickerBeforeClose: EventEmitter<void>;
  /** Fires when the component is closed and animation is complete. */
  calciteInputDatePickerClose: EventEmitter<void>;
  /** Fires when the component is added to the DOM but not rendered, and before the opening transition begins. */
  calciteInputDatePickerBeforeOpen: EventEmitter<void>;
  /** Fires when the component is open and animation is complete. */
  calciteInputDatePickerOpen: EventEmitter<void>;
  /** Sets focus on the component. */
  setFocus(): Promise<void>;
  /**
   * Updates the position of the component.
   *
   * @param delayed
   */
  reposition(delayed?: boolean): Promise<void>;
  connectedCallback(): void;
  componentWillLoad(): Promise<void>;
  componentDidLoad(): void;
  disconnectedCallback(): void;
  componentDidRender(): void;
  render(): VNode;
  filteredFlipPlacements: EffectivePlacement[];
  labelEl: HTMLCalciteLabelElement;
  formEl: HTMLFormElement;
  defaultValue: InputDatePicker["value"];
  datePickerActiveDate: Date;
  effectiveLocale: string;
  focusedInput: "start" | "end";
  globalAttributes: {};
  private localeData;
  private startInput;
  private endInput;
  private floatingEl;
  private referenceEl;
  private startWrapper;
  private endWrapper;
  private userChangedValue;
  openTransitionProp: string;
  transitionEl: HTMLDivElement;
  setReferenceEl(): void;
  setFilteredPlacements: () => void;
  private setTransitionEl;
  onLabelClick(): void;
  onBeforeOpen(): void;
  onOpen(): void;
  onBeforeClose(): void;
  onClose(): void;
  setStartInput: (el: HTMLCalciteInputElement) => void;
  setEndInput: (el: HTMLCalciteInputElement) => void;
  deactivate: () => void;
  private commitValue;
  keyDownHandler: (event: KeyboardEvent) => void;
  startInputFocus: () => void;
  endInputFocus: () => void;
  setFloatingEl: (el: HTMLDivElement) => void;
  setStartWrapper: (el: HTMLDivElement) => void;
  setEndWrapper: (el: HTMLDivElement) => void;
  startWatcher(start: string): void;
  endWatcher(end: string): void;
  private loadLocaleData;
  /**
   * Event handler for when the selected date changes
   *
   * @param event CalciteDatePicker custom change event
   */
  handleDateChange: (event: CustomEvent<Date>) => void;
  private shouldFocusRangeStart;
  private shouldFocusRangeEnd;
  private handleDateRangeChange;
  private localizeInputValues;
  private setInputValue;
  private setRangeValue;
  private setValue;
  private warnAboutInvalidValue;
  private commonDateSeparators;
  private formatNumerals;
}
