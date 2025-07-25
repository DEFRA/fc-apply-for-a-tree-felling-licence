/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { numberKeys } from "./key";
import { numberStringFormatter } from "./locale";
// adopted from https://stackoverflow.com/a/66939244
export class BigDecimal {
  constructor(input) {
    if (input instanceof BigDecimal) {
      return input;
    }
    const [integers, decimals] = String(input).split(".").concat("");
    this.value =
      BigInt(integers + decimals.padEnd(BigDecimal.DECIMALS, "0").slice(0, BigDecimal.DECIMALS)) +
        BigInt(BigDecimal.ROUNDED && decimals[BigDecimal.DECIMALS] >= "5");
    this.isNegative = input.charAt(0) === "-";
  }
  static _divRound(dividend, divisor) {
    return BigDecimal.fromBigInt(dividend / divisor + (BigDecimal.ROUNDED ? ((dividend * BigInt(2)) / divisor) % BigInt(2) : BigInt(0)));
  }
  static fromBigInt(bigint) {
    return Object.assign(Object.create(BigDecimal.prototype), { value: bigint });
  }
  toString() {
    const s = this.value
      .toString()
      .replace(new RegExp("-", "g"), "")
      .padStart(BigDecimal.DECIMALS + 1, "0");
    const i = s.slice(0, -BigDecimal.DECIMALS);
    const d = s.slice(-BigDecimal.DECIMALS).replace(/\.?0+$/, "");
    const value = i.concat(d.length ? "." + d : "");
    return `${this.isNegative ? "-" : ""}${value}`;
  }
  formatToParts(formatter) {
    const s = this.value
      .toString()
      .replace(new RegExp("-", "g"), "")
      .padStart(BigDecimal.DECIMALS + 1, "0");
    const i = s.slice(0, -BigDecimal.DECIMALS);
    const d = s.slice(-BigDecimal.DECIMALS).replace(/\.?0+$/, "");
    const parts = formatter.formatToParts(BigInt(i));
    this.isNegative && parts.unshift({ type: "minusSign", value: numberStringFormatter.minusSign });
    if (d.length) {
      parts.push({ type: "decimal", value: numberStringFormatter.decimal });
      d.split("").forEach((char) => parts.push({ type: "fraction", value: char }));
    }
    return parts;
  }
  format(formatter) {
    const s = this.value
      .toString()
      .replace(new RegExp("-", "g"), "")
      .padStart(BigDecimal.DECIMALS + 1, "0");
    const i = s.slice(0, -BigDecimal.DECIMALS);
    const d = s.slice(-BigDecimal.DECIMALS).replace(/\.?0+$/, "");
    const iFormatted = `${this.isNegative ? numberStringFormatter.minusSign : ""}${formatter.format(BigInt(i))}`;
    const dFormatted = d.length ? `${numberStringFormatter.decimal}${formatter.format(BigInt(d))}` : "";
    return `${iFormatted}${dFormatted}`;
  }
  add(num) {
    return BigDecimal.fromBigInt(this.value + new BigDecimal(num).value);
  }
  subtract(num) {
    return BigDecimal.fromBigInt(this.value - new BigDecimal(num).value);
  }
  multiply(num) {
    return BigDecimal._divRound(this.value * new BigDecimal(num).value, BigDecimal.SHIFT);
  }
  divide(num) {
    return BigDecimal._divRound(this.value * BigDecimal.SHIFT, new BigDecimal(num).value);
  }
}
// Configuration: constants
BigDecimal.DECIMALS = 100; // number of decimals on all instances
BigDecimal.ROUNDED = true; // numbers are truncated (false) or rounded (true)
BigDecimal.SHIFT = BigInt("1" + "0".repeat(BigDecimal.DECIMALS)); // derived constant
export function isValidNumber(numberString) {
  return !(!numberString || isNaN(Number(numberString)));
}
export function parseNumberString(numberString) {
  if (!numberString || !stringContainsNumbers(numberString)) {
    return "";
  }
  return sanitizeExponentialNumberString(numberString, (nonExpoNumString) => {
    let containsDecimal = false;
    const result = nonExpoNumString
      .split("")
      .filter((value, i) => {
      if (value.match(/\./g) && !containsDecimal) {
        containsDecimal = true;
        return true;
      }
      if (value.match(/\-/g) && i === 0) {
        return true;
      }
      return numberKeys.includes(value);
    })
      .reduce((string, part) => string + part);
    return isValidNumber(result) ? new BigDecimal(result).toString() : "";
  });
}
// regex for number sanitization
const allLeadingZerosOptionallyNegative = /^([-0])0+(?=\d)/;
const decimalOnlyAtEndOfString = /(?!^\.)\.$/;
const allHyphensExceptTheStart = /(?!^-)-/g;
const isNegativeDecimalOnlyZeros = /^-\b0\b\.?0*$/;
export const sanitizeNumberString = (numberString) => sanitizeExponentialNumberString(numberString, (nonExpoNumString) => {
  const sanitizedValue = nonExpoNumString
    .replace(allHyphensExceptTheStart, "")
    .replace(decimalOnlyAtEndOfString, "")
    .replace(allLeadingZerosOptionallyNegative, "$1");
  return isValidNumber(sanitizedValue)
    ? isNegativeDecimalOnlyZeros.test(sanitizedValue)
      ? sanitizedValue
      : new BigDecimal(sanitizedValue).toString()
    : nonExpoNumString;
});
export function sanitizeExponentialNumberString(numberString, func) {
  if (!numberString) {
    return numberString;
  }
  const firstE = numberString.toLowerCase().indexOf("e") + 1;
  if (!firstE) {
    return func(numberString);
  }
  return numberString
    .replace(/[eE]*$/g, "")
    .substring(0, firstE)
    .concat(numberString.slice(firstE).replace(/[eE]/g, ""))
    .split(/[eE]/)
    .map((section, i) => (i === 1 ? func(section.replace(/\./g, "")) : func(section)))
    .join("e")
    .replace(/^e/, "1e");
}
function stringContainsNumbers(string) {
  return numberKeys.some((number) => string.includes(number));
}
