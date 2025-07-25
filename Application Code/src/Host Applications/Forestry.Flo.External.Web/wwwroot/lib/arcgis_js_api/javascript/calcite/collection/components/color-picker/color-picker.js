/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import Color from "color";
import { CSS, DEFAULT_COLOR, DEFAULT_STORAGE_KEY_PREFIX, DIMENSIONS, HSV_LIMITS, RGB_LIMITS, TEXT } from "./resources";
import { focusElement, getElementDir, isPrimaryPointerButton } from "../../utils/dom";
import { colorEqual, CSSColorMode, normalizeHex, parseMode } from "./utils";
import { throttle } from "lodash-es";
import { clamp } from "../../utils/math";
import { updateHostInteraction } from "../../utils/interactive";
import { isActivationKey } from "../../utils/key";
const throttleFor60FpsInMs = 16;
const defaultValue = normalizeHex(DEFAULT_COLOR.hex());
const defaultFormat = "auto";
export class ColorPicker {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Public properties
    //
    //--------------------------------------------------------------------------
    /**
     * When `false`, an empty color (`null`) will be allowed as a `value`. Otherwise, a color value is enforced on the component.
     *
     * When `true`, a color value is enforced, and clearing the input or blurring will restore the last valid `value`. When `false`, an empty color (`null`) will be allowed as a `value`.
     */
    this.allowEmpty = false;
    /**
     * Specifies the appearance style of the component -
     *
     * `"solid"` (containing border) or `"minimal"` (no containing border).
     */
    this.appearance = "solid";
    /**
     * Internal prop for advanced use-cases.
     *
     * @internal
     */
    this.color = DEFAULT_COLOR;
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    /**
     * The format of `value`.
     *
     * When `"auto"`, the format will be inferred from `value` when set.
     *
     * @default "auto"
     */
    this.format = defaultFormat;
    /** When `true`, hides the Hex input. */
    this.hideHex = false;
    /** When `true`, hides the RGB/HSV channel inputs. */
    this.hideChannels = false;
    /** When `true`, hides the saved colors section. */
    this.hideSaved = false;
    /**
     * Accessible name for the RGB section's blue channel.
     *
     * @default "B"
     */
    this.intlB = TEXT.b;
    /**
     * Accessible name for the RGB section's blue channel description.
     *
     * @default "Blue"
     */
    this.intlBlue = TEXT.blue;
    /**
     * Accessible name for the delete color button.
     *
     * @default "Delete color"
     */
    this.intlDeleteColor = TEXT.deleteColor;
    /**
     * Accessible name for the RGB section's green channel.
     *
     * @default "G"
     */
    this.intlG = TEXT.g;
    /**
     * Accessible name for the RGB section's green channel description.
     *
     * @default "Green"
     */
    this.intlGreen = TEXT.green;
    /**
     * Accessible name for the HSV section's hue channel.
     *
     * @default "H"
     */
    this.intlH = TEXT.h;
    /**
     * Accessible name for the HSV mode.
     *
     * @default "HSV"
     */
    this.intlHsv = TEXT.hsv;
    /**
     * Accessible name for the Hex input.
     *
     * @default "Hex"
     */
    this.intlHex = TEXT.hex;
    /**
     * Accessible name for the HSV section's hue channel description.
     *
     * @default "Hue"
     */
    this.intlHue = TEXT.hue;
    /**
     * Accessible name for the Hex input when there is no color selected.
     *
     * @default "No color"
     */
    this.intlNoColor = TEXT.noColor;
    /**
     * Accessible name for the RGB section's red channel.
     *
     * @default "R"
     */
    this.intlR = TEXT.r;
    /**
     * Accessible name for the RGB section's red channel description.
     *
     * @default "Red"
     */
    this.intlRed = TEXT.red;
    /**
     * Accessible name for the RGB mode.
     *
     * @default "RGB"
     */
    this.intlRgb = TEXT.rgb;
    /**
     * Accessible name for the HSV section's saturation channel.
     *
     * @default "S"
     */
    this.intlS = TEXT.s;
    /**
     * Accessible name for the HSV section's saturation channel description.
     *
     * @default "Saturation"
     */
    this.intlSaturation = TEXT.saturation;
    /**
     * Accessible name for the save color button.
     *
     * @default "Save color"
     */
    this.intlSaveColor = TEXT.saveColor;
    /**
     * Accessible name for the saved colors section.
     *
     * @default "Saved"
     */
    this.intlSaved = TEXT.saved;
    /**
     * Accessible name for the HSV section's value channel.
     *
     * @default "V"
     */
    this.intlV = TEXT.v;
    /**
     * Accessible name for the HSV section's value channel description.
     *
     * @default "Value"
     */
    this.intlValue = TEXT.value;
    /** Specifies the size of the component. */
    this.scale = "m";
    /**
     * The component's value, where the value can be a CSS color string, or a RGB, HSL or HSV object.
     *
     * The type will be preserved as the color is updated.
     *
     * @default "#007ac2"
     * @see [CSS Color](https://developer.mozilla.org/en-US/docs/Web/CSS/color)
     * @see [ColorValue](https://github.com/Esri/calcite-components/blob/master/src/components/color-picker/interfaces.ts#L10)
     */
    this.value = defaultValue;
    this.colorFieldAndSliderHovered = false;
    this.hueThumbState = "idle";
    this.internalColorUpdateContext = null;
    this.mode = CSSColorMode.HEX;
    this.shiftKeyChannelAdjustment = 0;
    this.sliderThumbState = "idle";
    this.colorFieldAndSliderInteractive = false;
    this.channelMode = "rgb";
    this.channels = this.toChannels(DEFAULT_COLOR);
    this.dimensions = DIMENSIONS.m;
    this.savedColors = [];
    this.handleTabActivate = (event) => {
      this.channelMode = event.currentTarget.getAttribute("data-color-mode");
      this.updateChannelsFromColor(this.color);
    };
    this.handleColorFieldScopeKeyDown = (event) => {
      const { key } = event;
      const arrowKeyToXYOffset = {
        ArrowUp: { x: 0, y: -10 },
        ArrowRight: { x: 10, y: 0 },
        ArrowDown: { x: 0, y: 10 },
        ArrowLeft: { x: -10, y: 0 }
      };
      if (arrowKeyToXYOffset[key]) {
        event.preventDefault();
        this.scopeOrientation = key === "ArrowDown" || key === "ArrowUp" ? "vertical" : "horizontal";
        this.captureColorFieldColor(this.colorFieldScopeLeft + arrowKeyToXYOffset[key].x || 0, this.colorFieldScopeTop + arrowKeyToXYOffset[key].y || 0, false);
      }
    };
    this.handleHueScopeKeyDown = (event) => {
      const modifier = event.shiftKey ? 10 : 1;
      const { key } = event;
      const arrowKeyToXOffset = {
        ArrowUp: 1,
        ArrowRight: 1,
        ArrowDown: -1,
        ArrowLeft: -1
      };
      if (arrowKeyToXOffset[key]) {
        event.preventDefault();
        const delta = arrowKeyToXOffset[key] * modifier;
        const hue = this.baseColorFieldColor.hue();
        const color = this.baseColorFieldColor.hue(hue + delta);
        this.internalColorSet(color, false);
      }
    };
    this.handleHexInputChange = (event) => {
      event.stopPropagation();
      const { allowEmpty, color } = this;
      const input = event.target;
      const hex = input.value;
      if (allowEmpty && !hex) {
        this.internalColorSet(null);
        return;
      }
      const normalizedHex = color && normalizeHex(color.hex());
      if (hex !== normalizedHex) {
        this.internalColorSet(Color(hex));
      }
    };
    this.handleSavedColorSelect = (event) => {
      const swatch = event.currentTarget;
      this.internalColorSet(Color(swatch.color));
    };
    this.handleChannelInput = (event) => {
      const input = event.currentTarget;
      const internalInput = event.detail.nativeEvent.target;
      const channelIndex = Number(input.getAttribute("data-channel-index"));
      const limit = this.channelMode === "rgb"
        ? RGB_LIMITS[Object.keys(RGB_LIMITS)[channelIndex]]
        : HSV_LIMITS[Object.keys(HSV_LIMITS)[channelIndex]];
      let inputValue;
      if (this.allowEmpty && !input.value) {
        inputValue = "";
      }
      else {
        const value = Number(input.value) + this.shiftKeyChannelAdjustment;
        const clamped = clamp(value, 0, limit);
        inputValue = clamped.toString();
      }
      input.value = inputValue;
      internalInput.value = inputValue;
    };
    this.handleChannelChange = (event) => {
      const input = event.currentTarget;
      const channelIndex = Number(input.getAttribute("data-channel-index"));
      const channels = [...this.channels];
      const shouldClearChannels = this.allowEmpty && !input.value;
      if (shouldClearChannels) {
        this.channels = [null, null, null];
        this.internalColorSet(null);
        return;
      }
      channels[channelIndex] = Number(input.value);
      this.updateColorFromChannels(channels);
    };
    this.handleSavedColorKeyDown = (event) => {
      if (isActivationKey(event.key)) {
        event.preventDefault();
        this.handleSavedColorSelect(event);
      }
    };
    this.handleColorFieldAndSliderPointerLeave = () => {
      this.colorFieldAndSliderInteractive = false;
      this.colorFieldAndSliderHovered = false;
      if (this.sliderThumbState !== "drag" && this.hueThumbState !== "drag") {
        this.hueThumbState = "idle";
        this.sliderThumbState = "idle";
        this.drawColorFieldAndSlider();
      }
    };
    this.handleColorFieldAndSliderPointerDown = (event) => {
      var _a, _b;
      if (!isPrimaryPointerButton(event)) {
        return;
      }
      const { offsetX, offsetY } = event;
      const region = this.getCanvasRegion(offsetY);
      if (region === "color-field") {
        this.hueThumbState = "drag";
        this.captureColorFieldColor(offsetX, offsetY);
        (_a = this.colorFieldScopeNode) === null || _a === void 0 ? void 0 : _a.focus();
      }
      else if (region === "slider") {
        this.sliderThumbState = "drag";
        this.captureHueSliderColor(offsetX);
        (_b = this.hueScopeNode) === null || _b === void 0 ? void 0 : _b.focus();
      }
      // prevent text selection outside of color field & slider area
      event.preventDefault();
      document.addEventListener("pointermove", this.globalPointerMoveHandler);
      document.addEventListener("pointerup", this.globalPointerUpHandler, { once: true });
      this.activeColorFieldAndSliderRect =
        this.fieldAndSliderRenderingContext.canvas.getBoundingClientRect();
    };
    this.globalPointerUpHandler = (event) => {
      if (!isPrimaryPointerButton(event)) {
        return;
      }
      const previouslyDragging = this.sliderThumbState === "drag" || this.hueThumbState === "drag";
      this.hueThumbState = "idle";
      this.sliderThumbState = "idle";
      this.activeColorFieldAndSliderRect = null;
      this.drawColorFieldAndSlider();
      if (previouslyDragging) {
        this.calciteColorPickerChange.emit();
      }
    };
    this.globalPointerMoveHandler = (event) => {
      const { el, dimensions } = this;
      const sliderThumbDragging = this.sliderThumbState === "drag";
      const hueThumbDragging = this.hueThumbState === "drag";
      if (!el.isConnected || (!sliderThumbDragging && !hueThumbDragging)) {
        return;
      }
      let samplingX;
      let samplingY;
      const colorFieldAndSliderRect = this.activeColorFieldAndSliderRect;
      const { clientX, clientY } = event;
      if (this.colorFieldAndSliderHovered) {
        samplingX = clientX - colorFieldAndSliderRect.x;
        samplingY = clientY - colorFieldAndSliderRect.y;
      }
      else {
        const colorFieldWidth = dimensions.colorField.width;
        const colorFieldHeight = dimensions.colorField.height;
        const hueSliderHeight = dimensions.slider.height;
        if (clientX < colorFieldAndSliderRect.x + colorFieldWidth &&
          clientX > colorFieldAndSliderRect.x) {
          samplingX = clientX - colorFieldAndSliderRect.x;
        }
        else if (clientX < colorFieldAndSliderRect.x) {
          samplingX = 0;
        }
        else {
          samplingX = colorFieldWidth - 1;
        }
        if (clientY < colorFieldAndSliderRect.y + colorFieldHeight + hueSliderHeight &&
          clientY > colorFieldAndSliderRect.y) {
          samplingY = clientY - colorFieldAndSliderRect.y;
        }
        else if (clientY < colorFieldAndSliderRect.y) {
          samplingY = 0;
        }
        else {
          samplingY = colorFieldHeight + hueSliderHeight;
        }
      }
      if (hueThumbDragging) {
        this.captureColorFieldColor(samplingX, samplingY, false);
      }
      else {
        this.captureHueSliderColor(samplingX);
      }
    };
    this.handleColorFieldAndSliderPointerEnterOrMove = ({ offsetX, offsetY }) => {
      const { dimensions: { colorField, slider, thumb } } = this;
      this.colorFieldAndSliderInteractive = offsetY <= colorField.height + slider.height;
      this.colorFieldAndSliderHovered = true;
      const region = this.getCanvasRegion(offsetY);
      if (region === "color-field") {
        const prevHueThumbState = this.hueThumbState;
        const color = this.baseColorFieldColor.hsv();
        const centerX = Math.round(color.saturationv() / (HSV_LIMITS.s / colorField.width));
        const centerY = Math.round(colorField.height - color.value() / (HSV_LIMITS.v / colorField.height));
        const hoveringThumb = this.containsPoint(offsetX, offsetY, centerX, centerY, thumb.radius);
        let transitionedBetweenHoverAndIdle = false;
        if (prevHueThumbState === "idle" && hoveringThumb) {
          this.hueThumbState = "hover";
          transitionedBetweenHoverAndIdle = true;
        }
        else if (prevHueThumbState === "hover" && !hoveringThumb) {
          this.hueThumbState = "idle";
          transitionedBetweenHoverAndIdle = true;
        }
        if (this.hueThumbState !== "drag") {
          if (transitionedBetweenHoverAndIdle) {
            // refresh since we won't update color and thus no redraw
            this.drawColorFieldAndSlider();
          }
        }
      }
      else if (region === "slider") {
        const sliderThumbColor = this.baseColorFieldColor.hsv().saturationv(100).value(100);
        const prevSliderThumbState = this.sliderThumbState;
        const sliderThumbCenterX = Math.round(sliderThumbColor.hue() / (360 / slider.width));
        const sliderThumbCenterY = Math.round((slider.height + this.getSliderCapSpacing()) / 2) + colorField.height;
        const hoveringSliderThumb = this.containsPoint(offsetX, offsetY, sliderThumbCenterX, sliderThumbCenterY, thumb.radius);
        let sliderThumbTransitionedBetweenHoverAndIdle = false;
        if (prevSliderThumbState === "idle" && hoveringSliderThumb) {
          this.sliderThumbState = "hover";
          sliderThumbTransitionedBetweenHoverAndIdle = true;
        }
        else if (prevSliderThumbState === "hover" && !hoveringSliderThumb) {
          this.sliderThumbState = "idle";
          sliderThumbTransitionedBetweenHoverAndIdle = true;
        }
        if (this.sliderThumbState !== "drag") {
          if (sliderThumbTransitionedBetweenHoverAndIdle) {
            // refresh since we won't update color and thus no redraw
            this.drawColorFieldAndSlider();
          }
        }
      }
    };
    this.storeColorFieldScope = (node) => {
      this.colorFieldScopeNode = node;
    };
    this.storeHueScope = (node) => {
      this.hueScopeNode = node;
    };
    this.renderChannelsTabTitle = (channelMode) => {
      const { channelMode: activeChannelMode, intlRgb, intlHsv } = this;
      const active = channelMode === activeChannelMode;
      const label = channelMode === "rgb" ? intlRgb : intlHsv;
      return (h("calcite-tab-title", { active: active, class: CSS.colorMode, "data-color-mode": channelMode, key: channelMode, onCalciteTabsActivate: this.handleTabActivate }, label));
    };
    this.renderChannelsTab = (channelMode) => {
      const { channelMode: activeChannelMode, channels, intlB, intlBlue, intlG, intlGreen, intlH, intlHue, intlR, intlRed, intlS, intlSaturation, intlV, intlValue } = this;
      const active = channelMode === activeChannelMode;
      const isRgb = channelMode === "rgb";
      const channelLabels = isRgb ? [intlR, intlG, intlB] : [intlH, intlS, intlV];
      const channelAriaLabels = isRgb
        ? [intlRed, intlGreen, intlBlue]
        : [intlHue, intlSaturation, intlValue];
      const direction = getElementDir(this.el);
      return (h("calcite-tab", { active: active, class: CSS.control, key: channelMode }, h("div", { class: CSS.channels, dir: "ltr" }, channels.map((channel, index) => 
      /* the channel container is ltr, so we apply the host's direction */
      this.renderChannel(channel, index, channelLabels[index], channelAriaLabels[index], direction)))));
    };
    this.renderChannel = (value, index, label, ariaLabel, direction) => (h("calcite-input", { class: CSS.channel, "data-channel-index": index, dir: direction, label: ariaLabel, numberButtonType: "none", numberingSystem: this.numberingSystem, onCalciteInputChange: this.handleChannelChange, onCalciteInputInput: this.handleChannelInput, onKeyDown: this.handleKeyDown, prefixText: label, scale: this.scale === "l" ? "m" : "s", type: "number", value: value === null || value === void 0 ? void 0 : value.toString() }));
    this.deleteColor = () => {
      const colorToDelete = this.color.hex();
      const inStorage = this.savedColors.indexOf(colorToDelete) > -1;
      if (!inStorage) {
        return;
      }
      const savedColors = this.savedColors.filter((color) => color !== colorToDelete);
      this.savedColors = savedColors;
      const storageKey = `${DEFAULT_STORAGE_KEY_PREFIX}${this.storageId}`;
      if (this.storageId) {
        localStorage.setItem(storageKey, JSON.stringify(savedColors));
      }
    };
    this.saveColor = () => {
      const colorToSave = this.color.hex();
      const alreadySaved = this.savedColors.indexOf(colorToSave) > -1;
      if (alreadySaved) {
        return;
      }
      const savedColors = [...this.savedColors, colorToSave];
      this.savedColors = savedColors;
      const storageKey = `${DEFAULT_STORAGE_KEY_PREFIX}${this.storageId}`;
      if (this.storageId) {
        localStorage.setItem(storageKey, JSON.stringify(savedColors));
      }
    };
    this.drawColorFieldAndSlider = throttle(() => {
      if (!this.fieldAndSliderRenderingContext) {
        return;
      }
      this.drawColorField();
      this.drawHueSlider();
    }, throttleFor60FpsInMs);
    this.captureColorFieldColor = (x, y, skipEqual = true) => {
      const { dimensions: { colorField: { height, width } } } = this;
      const saturation = Math.round((HSV_LIMITS.s / width) * x);
      const value = Math.round((HSV_LIMITS.v / height) * (height - y));
      this.internalColorSet(this.baseColorFieldColor.hsv().saturationv(saturation).value(value), skipEqual);
    };
    this.initColorFieldAndSlider = (canvas) => {
      this.fieldAndSliderRenderingContext = canvas.getContext("2d");
      this.updateCanvasSize(canvas);
    };
  }
  handleColorChange(color, oldColor) {
    this.drawColorFieldAndSlider();
    this.updateChannelsFromColor(color);
    this.previousColor = oldColor;
  }
  handleFormatChange(format) {
    this.setMode(format);
    this.internalColorSet(this.color, false, "internal");
  }
  handleScaleChange(scale = "m") {
    var _a;
    this.updateDimensions(scale);
    this.updateCanvasSize((_a = this.fieldAndSliderRenderingContext) === null || _a === void 0 ? void 0 : _a.canvas);
  }
  handleValueChange(value, oldValue) {
    const { allowEmpty, format } = this;
    const checkMode = !allowEmpty || value;
    let modeChanged = false;
    if (checkMode) {
      const nextMode = parseMode(value);
      if (!nextMode || (format !== "auto" && nextMode !== format)) {
        this.showIncompatibleColorWarning(value, format);
        this.value = oldValue;
        return;
      }
      modeChanged = this.mode !== nextMode;
      this.setMode(nextMode);
    }
    const dragging = this.sliderThumbState === "drag" || this.hueThumbState === "drag";
    if (this.internalColorUpdateContext === "initial") {
      return;
    }
    if (this.internalColorUpdateContext === "user-interaction") {
      this.calciteColorPickerInput.emit();
      if (!dragging) {
        this.calciteColorPickerChange.emit();
      }
      return;
    }
    const color = allowEmpty && !value ? null : Color(value);
    const colorChanged = !colorEqual(color, this.color);
    if (modeChanged || colorChanged) {
      this.internalColorSet(color, true, "internal");
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Internal State/Props
  //
  //--------------------------------------------------------------------------
  get baseColorFieldColor() {
    return this.color || this.previousColor || DEFAULT_COLOR;
  }
  // using @Listen as a workaround for VDOM listener not firing
  handleChannelKeyUpOrDown(event) {
    this.shiftKeyChannelAdjustment = 0;
    const { key } = event;
    if ((key !== "ArrowUp" && key !== "ArrowDown") ||
      !event.composedPath().some((node) => { var _a; return (_a = node.classList) === null || _a === void 0 ? void 0 : _a.contains(CSS.channel); })) {
      return;
    }
    const { shiftKey } = event;
    event.preventDefault();
    if (!this.color) {
      this.internalColorSet(this.previousColor);
      event.stopPropagation();
      return;
    }
    // this gets applied to the input's up/down arrow increment/decrement
    const complementaryBump = 9;
    this.shiftKeyChannelAdjustment =
      key === "ArrowUp" && shiftKey
        ? complementaryBump
        : key === "ArrowDown" && shiftKey
          ? -complementaryBump
          : 0;
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    return focusElement(this.colorFieldScopeNode);
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentWillLoad() {
    const { allowEmpty, color, format, value } = this;
    const willSetNoColor = allowEmpty && !value;
    const parsedMode = parseMode(value);
    const valueIsCompatible = willSetNoColor || (format === "auto" && parsedMode) || format === parsedMode;
    const initialColor = willSetNoColor ? null : valueIsCompatible ? Color(value) : color;
    if (!valueIsCompatible) {
      this.showIncompatibleColorWarning(value, format);
    }
    this.setMode(format);
    this.internalColorSet(initialColor, false, "initial");
    this.updateDimensions(this.scale);
    const storageKey = `${DEFAULT_STORAGE_KEY_PREFIX}${this.storageId}`;
    if (this.storageId && localStorage.getItem(storageKey)) {
      this.savedColors = JSON.parse(localStorage.getItem(storageKey));
    }
  }
  disconnectedCallback() {
    document.removeEventListener("pointermove", this.globalPointerMoveHandler);
    document.removeEventListener("pointerup", this.globalPointerUpHandler);
  }
  componentDidRender() {
    updateHostInteraction(this);
  }
  //--------------------------------------------------------------------------
  //
  //  Render Methods
  //
  //--------------------------------------------------------------------------
  render() {
    const { allowEmpty, color, intlDeleteColor, hideHex, hideChannels, hideSaved, intlHex, intlSaved, intlSaveColor, savedColors, scale } = this;
    const selectedColorInHex = color ? color.hex() : null;
    const hexInputScale = scale === "l" ? "m" : "s";
    const { colorFieldAndSliderInteractive, colorFieldScopeTop, colorFieldScopeLeft, hueScopeLeft, hueScopeTop, scopeOrientation, dimensions: { colorField: { height: colorFieldHeight, width: colorFieldWidth }, slider: { height: sliderHeight } } } = this;
    const hueTop = hueScopeTop !== null && hueScopeTop !== void 0 ? hueScopeTop : sliderHeight / 2 + colorFieldHeight;
    const hueLeft = hueScopeLeft !== null && hueScopeLeft !== void 0 ? hueScopeLeft : (colorFieldWidth * DEFAULT_COLOR.hue()) / HSV_LIMITS.h;
    const noColor = color === null;
    const vertical = scopeOrientation === "vertical";
    return (h("div", { class: CSS.container }, h("div", { class: CSS.colorFieldAndSliderWrap }, h("canvas", { class: {
        [CSS.colorFieldAndSlider]: true,
        [CSS.colorFieldAndSliderInteractive]: colorFieldAndSliderInteractive
      }, onPointerDown: this.handleColorFieldAndSliderPointerDown, onPointerEnter: this.handleColorFieldAndSliderPointerEnterOrMove, onPointerLeave: this.handleColorFieldAndSliderPointerLeave, onPointerMove: this.handleColorFieldAndSliderPointerEnterOrMove, ref: this.initColorFieldAndSlider }), h("div", { "aria-label": vertical ? this.intlValue : this.intlSaturation, "aria-valuemax": vertical ? HSV_LIMITS.v : HSV_LIMITS.s, "aria-valuemin": "0", "aria-valuenow": (vertical ? color === null || color === void 0 ? void 0 : color.saturationv() : color === null || color === void 0 ? void 0 : color.value()) || "0", class: { [CSS.scope]: true, [CSS.colorFieldScope]: true }, onKeyDown: this.handleColorFieldScopeKeyDown, ref: this.storeColorFieldScope, role: "slider", style: { top: `${colorFieldScopeTop || 0}px`, left: `${colorFieldScopeLeft || 0}px` }, tabindex: "0" }), h("div", { "aria-label": this.intlHue, "aria-valuemax": HSV_LIMITS.h, "aria-valuemin": "0", "aria-valuenow": (color === null || color === void 0 ? void 0 : color.round().hue()) || DEFAULT_COLOR.round().hue(), class: { [CSS.scope]: true, [CSS.hueScope]: true }, onKeyDown: this.handleHueScopeKeyDown, ref: this.storeHueScope, role: "slider", style: { top: `${hueTop}px`, left: `${hueLeft}px` }, tabindex: "0" })), hideHex && hideChannels ? null : (h("div", { class: {
        [CSS.controlSection]: true,
        [CSS.section]: true
      } }, hideHex ? null : (h("div", { class: CSS.hexOptions }, h("span", { class: {
        [CSS.header]: true,
        [CSS.headerHex]: true
      } }, intlHex), h("calcite-color-picker-hex-input", { allowEmpty: allowEmpty, class: CSS.control, numberingSystem: this.numberingSystem, onCalciteColorPickerHexInputChange: this.handleHexInputChange, scale: hexInputScale, value: selectedColorInHex }))), hideChannels ? null : (h("calcite-tabs", { class: {
        [CSS.colorModeContainer]: true,
        [CSS.splitSection]: true
      }, scale: hexInputScale }, h("calcite-tab-nav", { slot: "tab-nav" }, this.renderChannelsTabTitle("rgb"), this.renderChannelsTabTitle("hsv")), this.renderChannelsTab("rgb"), this.renderChannelsTab("hsv"))))), hideSaved ? null : (h("div", { class: { [CSS.savedColorsSection]: true, [CSS.section]: true } }, h("div", { class: CSS.header }, h("label", null, intlSaved), h("div", { class: CSS.savedColorsButtons }, h("calcite-button", { appearance: "transparent", class: CSS.deleteColor, color: "neutral", disabled: noColor, iconStart: "minus", label: intlDeleteColor, onClick: this.deleteColor, scale: hexInputScale, type: "button" }), h("calcite-button", { appearance: "transparent", class: CSS.saveColor, color: "neutral", disabled: noColor, iconStart: "plus", label: intlSaveColor, onClick: this.saveColor, scale: hexInputScale, type: "button" }))), savedColors.length > 0 ? (h("div", { class: CSS.savedColors }, [
      ...savedColors.map((color) => (h("calcite-color-picker-swatch", { active: selectedColorInHex === color, class: CSS.savedColor, color: color, key: color, onClick: this.handleSavedColorSelect, onKeyDown: this.handleSavedColorKeyDown, scale: scale, tabIndex: 0 })))
    ])) : null))));
  }
  // --------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  handleKeyDown(event) {
    if (event.key === "Enter") {
      event.preventDefault();
    }
  }
  showIncompatibleColorWarning(value, format) {
    console.warn(`ignoring color value (${value}) as it is not compatible with the current format (${format})`);
  }
  setMode(format) {
    this.mode = format === "auto" ? this.mode : format;
  }
  captureHueSliderColor(x) {
    const { dimensions: { slider: { width } } } = this;
    const hue = (360 / width) * x;
    this.internalColorSet(this.baseColorFieldColor.hue(hue), false);
  }
  getCanvasRegion(y) {
    const { dimensions: { colorField: { height: colorFieldHeight }, slider: { height: sliderHeight } } } = this;
    if (y <= colorFieldHeight) {
      return "color-field";
    }
    if (y <= colorFieldHeight + sliderHeight) {
      return "slider";
    }
    return "none";
  }
  internalColorSet(color, skipEqual = true, context = "user-interaction") {
    if (skipEqual && colorEqual(color, this.color)) {
      return;
    }
    this.internalColorUpdateContext = context;
    this.color = color;
    this.value = this.toValue(color);
    this.internalColorUpdateContext = null;
  }
  toValue(color, format = this.mode) {
    if (!color) {
      return null;
    }
    const hexMode = "hex";
    if (format.includes(hexMode)) {
      return normalizeHex(color.round()[hexMode]());
    }
    if (format.includes("-css")) {
      return color[format.replace("-css", "").replace("a", "")]().round().string();
    }
    const colorObject = color[format]().round().object();
    if (format.endsWith("a")) {
      // normalize alpha prop
      colorObject.a = colorObject.alpha;
      delete colorObject.alpha;
    }
    return colorObject;
  }
  getSliderCapSpacing() {
    const { dimensions: { slider: { height }, thumb: { radius } } } = this;
    return radius * 2 - height;
  }
  updateDimensions(scale = "m") {
    this.dimensions = DIMENSIONS[scale];
  }
  drawColorField() {
    const context = this.fieldAndSliderRenderingContext;
    const { dimensions: { colorField: { height, width } } } = this;
    context.fillStyle = this.baseColorFieldColor.hsv().saturationv(100).value(100).string();
    context.fillRect(0, 0, width, height);
    const whiteGradient = context.createLinearGradient(0, 0, width, 0);
    whiteGradient.addColorStop(0, "rgba(255,255,255,1)");
    whiteGradient.addColorStop(1, "rgba(255,255,255,0)");
    context.fillStyle = whiteGradient;
    context.fillRect(0, 0, width, height);
    const blackGradient = context.createLinearGradient(0, 0, 0, height);
    blackGradient.addColorStop(0, "rgba(0,0,0,0)");
    blackGradient.addColorStop(1, "rgba(0,0,0,1)");
    context.fillStyle = blackGradient;
    context.fillRect(0, 0, width, height);
    this.drawActiveColorFieldColor();
  }
  setCanvasContextSize(canvas, { height, width }) {
    const devicePixelRatio = window.devicePixelRatio || 1;
    canvas.width = width * devicePixelRatio;
    canvas.height = height * devicePixelRatio;
    canvas.style.height = `${height}px`;
    canvas.style.width = `${width}px`;
    const context = canvas.getContext("2d");
    context.scale(devicePixelRatio, devicePixelRatio);
  }
  updateCanvasSize(canvas) {
    if (!canvas) {
      return;
    }
    this.setCanvasContextSize(canvas, {
      width: this.dimensions.colorField.width,
      height: this.dimensions.colorField.height +
        this.dimensions.slider.height +
        this.getSliderCapSpacing() * 2
    });
    this.drawColorFieldAndSlider();
  }
  containsPoint(testPointX, testPointY, boundsX, boundsY, boundsRadius) {
    return (Math.pow(testPointX - boundsX, 2) + Math.pow(testPointY - boundsY, 2) <=
      Math.pow(boundsRadius, 2));
  }
  drawActiveColorFieldColor() {
    const { color } = this;
    if (!color) {
      return;
    }
    const hsvColor = color.hsv();
    const { dimensions: { colorField: { height, width }, thumb: { radius } } } = this;
    const x = hsvColor.saturationv() / (HSV_LIMITS.s / width);
    const y = height - hsvColor.value() / (HSV_LIMITS.v / height);
    requestAnimationFrame(() => {
      this.colorFieldScopeLeft = x;
      this.colorFieldScopeTop = y;
    });
    this.drawThumb(this.fieldAndSliderRenderingContext, radius, x, y, hsvColor, this.hueThumbState);
  }
  drawThumb(context, radius, x, y, color, state) {
    const startAngle = 0;
    const endAngle = 2 * Math.PI;
    context.beginPath();
    context.arc(x, y, radius, startAngle, endAngle);
    context.shadowBlur = state === "hover" ? 32 : 16;
    context.shadowColor = `rgba(0, 0, 0, ${state === "drag" ? 0.32 : 0.16})`;
    context.fillStyle = "#fff";
    context.fill();
    context.beginPath();
    context.arc(x, y, radius - 3, startAngle, endAngle);
    context.shadowBlur = 0;
    context.shadowColor = "transparent";
    context.fillStyle = color.rgb().string();
    context.fill();
  }
  drawActiveHueSliderColor() {
    const { color } = this;
    if (!color) {
      return;
    }
    const hsvColor = color.hsv().saturationv(100).value(100);
    const { dimensions: { colorField: { height: colorFieldHeight }, slider: { height, width }, thumb: { radius } } } = this;
    const x = hsvColor.hue() / (360 / width);
    const y = height / 2 + colorFieldHeight;
    requestAnimationFrame(() => {
      this.hueScopeLeft = x;
      this.hueScopeTop = y;
    });
    this.drawThumb(this.fieldAndSliderRenderingContext, radius, x, y, hsvColor, this.sliderThumbState);
  }
  drawHueSlider() {
    const context = this.fieldAndSliderRenderingContext;
    const { dimensions: { colorField: { height: colorFieldHeight }, slider: { height, width } } } = this;
    const gradient = context.createLinearGradient(0, 0, width, 0);
    const hueSliderColorStopKeywords = ["red", "yellow", "lime", "cyan", "blue", "magenta", "red"];
    const offset = 1 / (hueSliderColorStopKeywords.length - 1);
    let currentOffset = 0;
    hueSliderColorStopKeywords.forEach((keyword) => {
      gradient.addColorStop(currentOffset, Color(keyword).string());
      currentOffset += offset;
    });
    context.fillStyle = gradient;
    context.clearRect(0, colorFieldHeight, width, height + this.getSliderCapSpacing() * 2);
    context.fillRect(0, colorFieldHeight, width, height);
    this.drawActiveHueSliderColor();
  }
  updateColorFromChannels(channels) {
    this.internalColorSet(Color(channels, this.channelMode));
  }
  updateChannelsFromColor(color) {
    this.channels = color ? this.toChannels(color) : [null, null, null];
  }
  toChannels(color) {
    const { channelMode } = this;
    return color[channelMode]()
      .array()
      .map((value) => Math.floor(value));
  }
  static get is() { return "calcite-color-picker"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["color-picker.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["color-picker.css"]
    };
  }
  static get properties() {
    return {
      "allowEmpty": {
        "type": "boolean",
        "mutable": false,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "When `false`, an empty color (`null`) will be allowed as a `value`. Otherwise, a color value is enforced on the component.\n\nWhen `true`, a color value is enforced, and clearing the input or blurring will restore the last valid `value`. When `false`, an empty color (`null`) will be allowed as a `value`."
        },
        "attribute": "allow-empty",
        "reflect": true,
        "defaultValue": "false"
      },
      "appearance": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "ColorAppearance",
          "resolved": "\"default\" | \"minimal\" | \"solid\"",
          "references": {
            "ColorAppearance": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the appearance style of the component -\n\n`\"solid\"` (containing border) or `\"minimal\"` (no containing border)."
        },
        "attribute": "appearance",
        "reflect": true,
        "defaultValue": "\"solid\""
      },
      "color": {
        "type": "unknown",
        "mutable": true,
        "complexType": {
          "original": "InternalColor | null",
          "resolved": "Color<ColorParam>",
          "references": {
            "InternalColor": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": "Internal prop for advanced use-cases."
        },
        "defaultValue": "DEFAULT_COLOR"
      },
      "disabled": {
        "type": "boolean",
        "mutable": false,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "When `true`, interaction is prevented and the component is displayed with lower opacity."
        },
        "attribute": "disabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "format": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Format",
          "resolved": "\"auto\" | \"hex\" | \"hexa\" | \"hsl\" | \"hsl-css\" | \"hsla\" | \"hsla-css\" | \"hsv\" | \"hsva\" | \"rgb\" | \"rgb-css\" | \"rgba\" | \"rgba-css\"",
          "references": {
            "Format": {
              "location": "import",
              "path": "./utils"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"auto\""
            }],
          "text": "The format of `value`.\n\nWhen `\"auto\"`, the format will be inferred from `value` when set."
        },
        "attribute": "format",
        "reflect": true,
        "defaultValue": "defaultFormat"
      },
      "hideHex": {
        "type": "boolean",
        "mutable": false,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "When `true`, hides the Hex input."
        },
        "attribute": "hide-hex",
        "reflect": true,
        "defaultValue": "false"
      },
      "hideChannels": {
        "type": "boolean",
        "mutable": false,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "When `true`, hides the RGB/HSV channel inputs."
        },
        "attribute": "hide-channels",
        "reflect": true,
        "defaultValue": "false"
      },
      "hideSaved": {
        "type": "boolean",
        "mutable": false,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "When `true`, hides the saved colors section."
        },
        "attribute": "hide-saved",
        "reflect": true,
        "defaultValue": "false"
      },
      "intlB": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"B\""
            }],
          "text": "Accessible name for the RGB section's blue channel."
        },
        "attribute": "intl-b",
        "reflect": false,
        "defaultValue": "TEXT.b"
      },
      "intlBlue": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"Blue\""
            }],
          "text": "Accessible name for the RGB section's blue channel description."
        },
        "attribute": "intl-blue",
        "reflect": false,
        "defaultValue": "TEXT.blue"
      },
      "intlDeleteColor": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"Delete color\""
            }],
          "text": "Accessible name for the delete color button."
        },
        "attribute": "intl-delete-color",
        "reflect": false,
        "defaultValue": "TEXT.deleteColor"
      },
      "intlG": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"G\""
            }],
          "text": "Accessible name for the RGB section's green channel."
        },
        "attribute": "intl-g",
        "reflect": false,
        "defaultValue": "TEXT.g"
      },
      "intlGreen": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"Green\""
            }],
          "text": "Accessible name for the RGB section's green channel description."
        },
        "attribute": "intl-green",
        "reflect": false,
        "defaultValue": "TEXT.green"
      },
      "intlH": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"H\""
            }],
          "text": "Accessible name for the HSV section's hue channel."
        },
        "attribute": "intl-h",
        "reflect": false,
        "defaultValue": "TEXT.h"
      },
      "intlHsv": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"HSV\""
            }],
          "text": "Accessible name for the HSV mode."
        },
        "attribute": "intl-hsv",
        "reflect": false,
        "defaultValue": "TEXT.hsv"
      },
      "intlHex": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"Hex\""
            }],
          "text": "Accessible name for the Hex input."
        },
        "attribute": "intl-hex",
        "reflect": false,
        "defaultValue": "TEXT.hex"
      },
      "intlHue": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"Hue\""
            }],
          "text": "Accessible name for the HSV section's hue channel description."
        },
        "attribute": "intl-hue",
        "reflect": false,
        "defaultValue": "TEXT.hue"
      },
      "intlNoColor": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"No color\""
            }],
          "text": "Accessible name for the Hex input when there is no color selected."
        },
        "attribute": "intl-no-color",
        "reflect": false,
        "defaultValue": "TEXT.noColor"
      },
      "intlR": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"R\""
            }],
          "text": "Accessible name for the RGB section's red channel."
        },
        "attribute": "intl-r",
        "reflect": false,
        "defaultValue": "TEXT.r"
      },
      "intlRed": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"Red\""
            }],
          "text": "Accessible name for the RGB section's red channel description."
        },
        "attribute": "intl-red",
        "reflect": false,
        "defaultValue": "TEXT.red"
      },
      "intlRgb": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"RGB\""
            }],
          "text": "Accessible name for the RGB mode."
        },
        "attribute": "intl-rgb",
        "reflect": false,
        "defaultValue": "TEXT.rgb"
      },
      "intlS": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"S\""
            }],
          "text": "Accessible name for the HSV section's saturation channel."
        },
        "attribute": "intl-s",
        "reflect": false,
        "defaultValue": "TEXT.s"
      },
      "intlSaturation": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"Saturation\""
            }],
          "text": "Accessible name for the HSV section's saturation channel description."
        },
        "attribute": "intl-saturation",
        "reflect": false,
        "defaultValue": "TEXT.saturation"
      },
      "intlSaveColor": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"Save color\""
            }],
          "text": "Accessible name for the save color button."
        },
        "attribute": "intl-save-color",
        "reflect": false,
        "defaultValue": "TEXT.saveColor"
      },
      "intlSaved": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"Saved\""
            }],
          "text": "Accessible name for the saved colors section."
        },
        "attribute": "intl-saved",
        "reflect": false,
        "defaultValue": "TEXT.saved"
      },
      "intlV": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"V\""
            }],
          "text": "Accessible name for the HSV section's value channel."
        },
        "attribute": "intl-v",
        "reflect": false,
        "defaultValue": "TEXT.v"
      },
      "intlValue": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"Value\""
            }],
          "text": "Accessible name for the HSV section's value channel description."
        },
        "attribute": "intl-value",
        "reflect": false,
        "defaultValue": "TEXT.value"
      },
      "scale": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Scale",
          "resolved": "\"l\" | \"m\" | \"s\"",
          "references": {
            "Scale": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the size of the component."
        },
        "attribute": "scale",
        "reflect": true,
        "defaultValue": "\"m\""
      },
      "storageId": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the storage ID for colors."
        },
        "attribute": "storage-id",
        "reflect": true
      },
      "numberingSystem": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "NumberingSystem",
          "resolved": "\"arab\" | \"arabext\" | \"bali\" | \"beng\" | \"deva\" | \"fullwide\" | \"gujr\" | \"guru\" | \"hanidec\" | \"khmr\" | \"knda\" | \"laoo\" | \"latn\" | \"limb\" | \"mlym\" | \"mong\" | \"mymr\" | \"orya\" | \"tamldec\" | \"telu\" | \"thai\" | \"tibt\"",
          "references": {
            "NumberingSystem": {
              "location": "import",
              "path": "../../utils/locale"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies the Unicode numeral system used by the component for localization."
        },
        "attribute": "numbering-system",
        "reflect": true
      },
      "value": {
        "type": "string",
        "mutable": true,
        "complexType": {
          "original": "ColorValue | null",
          "resolved": "HSL | HSL & ObjectWithAlpha | HSV | HSV & ObjectWithAlpha | RGB | RGB & ObjectWithAlpha | string",
          "references": {
            "ColorValue": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"#007ac2\""
            }, {
              "name": "see",
              "text": "[CSS Color](https://developer.mozilla.org/en-US/docs/Web/CSS/color)"
            }, {
              "name": "see",
              "text": "[ColorValue](https://github.com/Esri/calcite-components/blob/master/src/components/color-picker/interfaces.ts#L10)"
            }],
          "text": "The component's value, where the value can be a CSS color string, or a RGB, HSL or HSV object.\n\nThe type will be preserved as the color is updated."
        },
        "attribute": "value",
        "reflect": false,
        "defaultValue": "defaultValue"
      }
    };
  }
  static get states() {
    return {
      "colorFieldAndSliderInteractive": {},
      "channelMode": {},
      "channels": {},
      "dimensions": {},
      "savedColors": {},
      "colorFieldScopeTop": {},
      "colorFieldScopeLeft": {},
      "scopeOrientation": {},
      "hueScopeLeft": {},
      "hueScopeTop": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteColorPickerChange",
        "name": "calciteColorPickerChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the color value has changed."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteColorPickerInput",
        "name": "calciteColorPickerInput",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires as the color value changes.\n\nSimilar to the `calciteColorPickerChange` event with the exception of dragging. When dragging the color field or hue slider thumb, this event fires as the thumb is moved."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }];
  }
  static get methods() {
    return {
      "setFocus": {
        "complexType": {
          "signature": "() => Promise<void>",
          "parameters": [],
          "references": {
            "Promise": {
              "location": "global"
            }
          },
          "return": "Promise<void>"
        },
        "docs": {
          "text": "Sets focus on the component.",
          "tags": []
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "color",
        "methodName": "handleColorChange"
      }, {
        "propName": "format",
        "methodName": "handleFormatChange"
      }, {
        "propName": "scale",
        "methodName": "handleScaleChange"
      }, {
        "propName": "value",
        "methodName": "handleValueChange"
      }];
  }
  static get listeners() {
    return [{
        "name": "keydown",
        "method": "handleChannelKeyUpOrDown",
        "target": undefined,
        "capture": true,
        "passive": false
      }, {
        "name": "keyup",
        "method": "handleChannelKeyUpOrDown",
        "target": undefined,
        "capture": true,
        "passive": false
      }];
  }
}
