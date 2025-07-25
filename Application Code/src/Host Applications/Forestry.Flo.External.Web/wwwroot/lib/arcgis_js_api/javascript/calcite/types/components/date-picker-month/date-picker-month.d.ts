import { EventEmitter, VNode } from "../../stencil-public-runtime";
import { HoverRange } from "../../utils/date";
import { DateLocaleData } from "../date-picker/utils";
import { Scale } from "../interfaces";
export declare class DatePickerMonth {
  el: HTMLCalciteDatePickerMonthElement;
  /** Already selected date.*/
  selectedDate: Date;
  /** Date currently active.*/
  activeDate: Date;
  /** Start date currently active. */
  startDate?: Date;
  /** End date currently active  */
  endDate?: Date;
  /** Minimum date of the calendar below which is disabled.*/
  min: Date;
  /** Maximum date of the calendar above which is disabled.*/
  max: Date;
  /** specify the scale of the date picker */
  scale: Scale;
  /**
   * CLDR locale data for current locale
   *
   * @internal
   */
  localeData: DateLocaleData;
  /** The range of dates currently being hovered */
  hoverRange: HoverRange;
  /**
   * Event emitted when user selects the date.
   */
  calciteDatePickerSelect: EventEmitter<Date>;
  /**
   * Event emitted when user hovers the date.
   *
   * @internal
   */
  calciteInternalDatePickerHover: EventEmitter<Date>;
  /**
   * Active date for the user keyboard access.
   */
  calciteDatePickerActiveDateChange: EventEmitter<Date>;
  /**
   * @internal
   */
  calciteInternalDatePickerMouseOut: EventEmitter<void>;
  keyDownHandler: (event: KeyboardEvent) => void;
  /**
   * Once user is not interacting via keyboard,
   * disable auto focusing of active date
   */
  disableActiveFocus: () => void;
  mouseoutHandler(): void;
  render(): VNode;
  private activeFocus;
  /**
   * Add n months to the current month
   *
   * @param step
   */
  private addMonths;
  /**
   * Add n days to the current date
   *
   * @param step
   */
  private addDays;
  /**
   * Get dates for last days of the previous month
   *
   * @param month
   * @param year
   * @param startOfWeek
   */
  private getPrevMonthdays;
  /**
   * Get dates for the current month
   *
   * @param month
   * @param year
   */
  private getCurrentMonthDays;
  /**
   * Get dates for first days of the next month
   *
   * @param month
   * @param year
   * @param startOfWeek
   */
  private getNextMonthDays;
  /**
   * Determine if the date is in between the start and end dates
   *
   * @param date
   */
  private betweenSelectedRange;
  /**
   * Determine if the date should be in selected state
   *
   * @param date
   */
  private isSelected;
  /**
   * Determine if the date is the start of the date range
   *
   * @param date
   */
  private isStartOfRange;
  private isEndOfRange;
  dayHover: (event: CustomEvent) => void;
  daySelect: (event: CustomEvent) => void;
  /**
   * Render calcite-date-picker-day
   *
   * @param active
   * @param day
   * @param date
   * @param currentMonth
   * @param ref
   */
  private renderDateDay;
  private isFocusedOnStart;
  private isHoverInRange;
  private isRangeHover;
}
