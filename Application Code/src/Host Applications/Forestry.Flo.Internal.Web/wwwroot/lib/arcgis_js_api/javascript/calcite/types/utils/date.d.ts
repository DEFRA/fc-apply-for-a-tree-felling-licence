import { DateLocaleData } from "../components/date-picker/utils";
export interface HoverRange {
  focused: "end" | "start";
  start: Date;
  end: Date;
}
/**
 * Check if date is within a min and max
 *
 * @param date
 * @param min
 * @param max
 */
export declare function inRange(date: Date, min?: Date | string, max?: Date | string): boolean;
/**
 * Ensures date is within range,
 * returns min or max if out of bounds
 *
 * @param date
 * @param min
 * @param max
 */
export declare function dateFromRange(date?: any, min?: Date | string, max?: Date | string): Date | null;
/**
 * Parse an iso8601 string (YYYY-mm-dd) into a valid date.
 * TODO: handle time when time of day UI is added
 *
 * @param iso8601
 * @param isEndDate
 */
export declare function dateFromISO(iso8601: string | Date, isEndDate?: boolean): Date | null;
/**
 * Parse a localized date string into a valid Date.
 * return false if date is invalid, or out of range
 *
 * @param value
 * @param localeData
 */
export declare function dateFromLocalizedString(value: string, localeData: DateLocaleData): Date;
/**
 * Retrieve day, month, and year strings from a localized string
 *
 * @param string
 * @param localeData
 */
export declare function datePartsFromLocalizedString(string: string, localeData: DateLocaleData): {
  day: string;
  month: string;
  year: string;
};
/**
 * Return first portion of ISO string (YYYY-mm-dd)
 *
 * @param date
 */
export declare function dateToISO(date?: Date | string): string;
/**
 * Check if two dates are the same day, month, year
 *
 * @param d1
 * @param d2
 */
export declare function sameDate(d1: Date, d2: Date): boolean;
/**
 * Get a date one month in the past
 *
 * @param date
 */
export declare function prevMonth(date: Date): Date;
/**
 * Get a date one month in the future
 *
 * @param date
 */
export declare function nextMonth(date: Date): Date;
/**
 * Parse numeric units for day, month, and year from a localized string
 * month starts at 0 (can pass to date constructor)
 * can return values as number or string
 *
 * @param string
 * @param localeData
 */
export declare function parseDateString(string: string, localeData: DateLocaleData): {
  day: number;
  month: number;
  year: number;
};
declare type unitOrderSignifier = "m" | "d" | "y";
/**
 * Based on the unitOrder string, find order of month, day, and year for locale
 *
 * @param unitOrder
 */
export declare function getOrder(unitOrder: string): unitOrderSignifier[];
/**
 * Get number of days between two dates
 *
 * @param date1
 * @param date2
 */
export declare function getDaysDiff(date1: Date, date2: Date): number;
/**
 * Set time of the day to the end.
 *
 * @param {Date} date Date.
 * @returns {Date} Date with time set to end of day .
 */
export declare function setEndOfDay(date: Date): Date;
export {};
