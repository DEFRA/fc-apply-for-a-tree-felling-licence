/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
'use strict';

function rgbToHex(color) {
  const { r, g, b } = color;
  return `#${r.toString(16).padStart(2, "0")}${g.toString(16).padStart(2, "0")}${b
    .toString(16)
    .padStart(2, "0")}`.toLowerCase();
}
const hexChar = /^[0-9A-F]$/i;
const shortHandHex = /^#[0-9A-F]{3}$/i;
const longhandHex = /^#[0-9A-F]{6}$/i;
function isValidHex(hex) {
  return isShorthandHex(hex) || isLonghandHex(hex);
}
function isShorthandHex(hex) {
  return hex && hex.length === 4 && shortHandHex.test(hex);
}
function isLonghandHex(hex) {
  return hex && hex.length === 7 && longhandHex.test(hex);
}
function normalizeHex(hex) {
  hex = hex.toLowerCase();
  if (!hex.startsWith("#")) {
    hex = `#${hex}`;
  }
  if (isShorthandHex(hex)) {
    return rgbToHex(hexToRGB(hex));
  }
  return hex;
}
function hexToRGB(hex) {
  if (!isValidHex(hex)) {
    return null;
  }
  hex = hex.replace("#", "");
  if (hex.length === 3) {
    const [first, second, third] = hex.split("");
    const r = parseInt(`${first}${first}`, 16);
    const g = parseInt(`${second}${second}`, 16);
    const b = parseInt(`${third}${third}`, 16);
    return { r, g, b };
  }
  const r = parseInt(hex.slice(0, 2), 16);
  const g = parseInt(hex.slice(2, 4), 16);
  const b = parseInt(hex.slice(4, 6), 16);
  return { r, g, b };
}
// these utils allow users to pass enum values as strings without having to access the enum
// based on the approach suggested by https://github.com/microsoft/TypeScript/issues/17690#issuecomment-321365759,
const enumify = (x) => x;
const CSSColorMode = enumify({
  HEX: "hex",
  HEXA: "hexa",
  RGB_CSS: "rgb-css",
  RGBA_CSS: "rgba-css",
  HSL_CSS: "hsl-css",
  HSLA_CSS: "hsla-css"
});
const ObjectColorMode = enumify({
  RGB: "rgb",
  RGBA: "rgba",
  HSL: "hsl",
  HSLA: "hsla",
  HSV: "hsv",
  HSVA: "hsva"
});
function parseMode(colorValue) {
  if (typeof colorValue === "string") {
    if (colorValue.startsWith("#")) {
      const { length } = colorValue;
      if (length === 4 || length === 7) {
        return CSSColorMode.HEX;
      }
      if (length === 5 || length === 9) {
        return CSSColorMode.HEXA;
      }
    }
    if (colorValue.startsWith("rgba(")) {
      return CSSColorMode.RGBA_CSS;
    }
    if (colorValue.startsWith("rgb(")) {
      return CSSColorMode.RGB_CSS;
    }
    if (colorValue.startsWith("hsl(")) {
      return CSSColorMode.HSL_CSS;
    }
    if (colorValue.startsWith("hsla(")) {
      return CSSColorMode.HSLA_CSS;
    }
  }
  if (typeof colorValue === "object") {
    if (hasChannels(colorValue, "r", "g", "b")) {
      return hasChannels(colorValue, "a") ? ObjectColorMode.RGBA : ObjectColorMode.RGB;
    }
    if (hasChannels(colorValue, "h", "s", "l")) {
      return hasChannels(colorValue, "a") ? ObjectColorMode.HSLA : ObjectColorMode.HSL;
    }
    if (hasChannels(colorValue, "h", "s", "v")) {
      return hasChannels(colorValue, "a") ? ObjectColorMode.HSVA : ObjectColorMode.HSV;
    }
  }
  return null;
}
function hasChannels(colorObject, ...channels) {
  return channels.every((channel) => channel && colorObject && `${channel}` in colorObject);
}
function colorEqual(value1, value2) {
  return (value1 === null || value1 === void 0 ? void 0 : value1.rgbNumber()) === (value2 === null || value2 === void 0 ? void 0 : value2.rgbNumber());
}

exports.CSSColorMode = CSSColorMode;
exports.colorEqual = colorEqual;
exports.hexChar = hexChar;
exports.hexToRGB = hexToRGB;
exports.isLonghandHex = isLonghandHex;
exports.isValidHex = isValidHex;
exports.normalizeHex = normalizeHex;
exports.parseMode = parseMode;
exports.rgbToHex = rgbToHex;
