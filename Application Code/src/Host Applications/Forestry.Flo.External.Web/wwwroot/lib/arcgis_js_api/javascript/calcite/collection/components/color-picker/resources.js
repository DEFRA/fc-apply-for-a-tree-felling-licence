/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import Color from "color";
export const CSS = {
  container: "container",
  controlSection: "control-section",
  hexOptions: "color-hex-options",
  section: "section",
  header: "header",
  control: "control",
  splitSection: "section--split",
  colorModeContainer: "color-mode-container",
  colorMode: "color-mode",
  channels: "channels",
  channel: "channel",
  savedColors: "saved-colors",
  savedColorsSection: "saved-colors-section",
  saveColor: "save-color",
  deleteColor: "delete-color",
  savedColorsButtons: "saved-colors-buttons",
  headerHex: "header--hex",
  colorFieldAndSlider: "color-field-and-slider",
  colorFieldAndSliderInteractive: "color-field-and-slider--interactive",
  colorFieldAndSliderWrap: "color-field-and-slider-wrap",
  scope: "scope",
  hueScope: "scope--hue",
  colorFieldScope: "scope--color-field",
  savedColor: "saved-color"
};
export const DEFAULT_COLOR = Color("#007AC2");
export const DEFAULT_STORAGE_KEY_PREFIX = "calcite-color-";
export const RGB_LIMITS = {
  r: 255,
  g: 255,
  b: 255
};
export const HSV_LIMITS = {
  h: 360,
  s: 100,
  v: 100
};
export const TEXT = {
  b: "B",
  blue: "Blue",
  deleteColor: "Delete color",
  g: "G",
  green: "Green",
  h: "H",
  hsv: "HSV",
  hex: "Hex",
  hue: "Hue",
  noColor: "No color",
  r: "R",
  red: "Red",
  rgb: "RGB",
  s: "S",
  saturation: "Saturation",
  saveColor: "Save color",
  saved: "Saved",
  v: "V",
  value: "Value"
};
export const DIMENSIONS = {
  s: {
    slider: {
      height: 10,
      width: 160
    },
    colorField: {
      height: 80,
      width: 160
    },
    thumb: {
      radius: 8
    }
  },
  m: {
    slider: {
      height: 14,
      width: 272
    },
    colorField: {
      height: 150,
      width: 272
    },
    thumb: {
      radius: 10
    }
  },
  l: {
    slider: {
      height: 16,
      width: 464
    },
    colorField: {
      height: 200,
      width: 464
    },
    thumb: {
      radius: 12
    }
  }
};
