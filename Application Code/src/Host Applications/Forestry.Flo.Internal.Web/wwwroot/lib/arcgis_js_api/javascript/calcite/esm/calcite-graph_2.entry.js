/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { r as registerInstance, h, g as getElement, f as forceUpdate, c as createEvent, H as Host } from './index-7b536c47.js';
import { g as guid } from './guid-3c14f5cd.js';
import { c as createObserver } from './observers-513bffd3.js';
import { i as isPrimaryPointerButton, k as intersects } from './dom-bbdd8cab.js';
import { d as decimalPlaces, c as clamp } from './math-fc031ff1.js';
import { c as connectLabel, d as disconnectLabel } from './label-5ccbdccb.js';
import { c as connectForm, d as disconnectForm, a as afterConnectDefaultValueSet, H as HiddenFormInputSlot } from './form-993bce3a.js';
import { u as updateHostInteraction } from './interactive-fc7d4437.js';
import { i as isActivationKey } from './key-41222930.js';
import { n as numberStringFormatter, c as connectLocalized, d as disconnectLocalized } from './locale-4001e60f.js';
import './resources-c8c07116.js';

/**
 * Calculate slope of the tangents
 * uses Steffen interpolation as it's monotonic
 * http://jrwalsh1.github.io/posts/interpolations/
 *
 * @param p0
 * @param p1
 * @param p2
 */
function slope(p0, p1, p2) {
  const dx = p1[0] - p0[0];
  const dx1 = p2[0] - p1[0];
  const dy = p1[1] - p0[1];
  const dy1 = p2[1] - p1[1];
  const m = dy / (dx || (dx1 < 0 && 0));
  const m1 = dy1 / (dx1 || (dx < 0 && 0));
  const p = (m * dx1 + m1 * dx) / (dx + dx1);
  return (Math.sign(m) + Math.sign(m1)) * Math.min(Math.abs(m), Math.abs(m1), 0.5 * Math.abs(p)) || 0;
}
/**
 * Calculate slope for just one tangent (single-sided)
 *
 * @param p0
 * @param p1
 * @param m
 */
function slopeSingle(p0, p1, m) {
  const dx = p1[0] - p0[0];
  const dy = p1[1] - p0[1];
  return dx ? ((3 * dy) / dx - m) / 2 : m;
}
/**
 * Given two points and their tangent slopes,
 * calculate the bezier handle coordinates and return draw command.
 *
 * Translates Hermite Spline to Beziér curve:
 * stackoverflow.com/questions/42574940/
 *
 * @param p0
 * @param p1
 * @param m0
 * @param m1
 * @param t
 */
function bezier(p0, p1, m0, m1, t) {
  const [x0, y0] = p0;
  const [x1, y1] = p1;
  const dx = (x1 - x0) / 3;
  const h1 = t([x0 + dx, y0 + dx * m0]).join(",");
  const h2 = t([x1 - dx, y1 - dx * m1]).join(",");
  const p = t([x1, y1]).join(",");
  return `C ${h1} ${h2} ${p}`;
}
/**
 * Generate a function which will translate a point
 * from the data coordinate space to svg viewbox oriented pixels
 *
 * @param root0
 * @param root0.width
 * @param root0.height
 * @param root0.min
 * @param root0.max
 */
function translate({ width, height, min, max }) {
  const rangeX = max[0] - min[0];
  const rangeY = max[1] - min[1];
  return (point) => {
    const x = ((point[0] - min[0]) / rangeX) * width;
    const y = height - (point[1] / rangeY) * height;
    return [x, y];
  };
}
/**
 * Get the min and max values from the dataset
 *
 * @param data
 */
function range(data) {
  const [startX, startY] = data[0];
  const min = [startX, startY];
  const max = [startX, startY];
  return data.reduce(({ min, max }, [x, y]) => ({
    min: [Math.min(min[0], x), Math.min(min[1], y)],
    max: [Math.max(max[0], x), Math.max(max[1], y)]
  }), { min, max });
}
/**
 * Generate drawing commands for an area graph
 * returns a string can can be passed directly to a path element's `d` attribute
 *
 * @param root0
 * @param root0.data
 * @param root0.min
 * @param root0.max
 * @param root0.t
 */
function area({ data, min, max, t }) {
  if (data.length === 0) {
    return "";
  }
  // important points for beginning and ending the path
  const [startX, startY] = t(data[0]);
  const [minX, minY] = t(min);
  const [maxX] = t(max);
  // keep track of previous slope/points
  let m;
  let p0;
  let p1;
  // iterate over data points, calculating command for each
  const commands = data.reduce((acc, point, i) => {
    p0 = data[i - 2];
    p1 = data[i - 1];
    if (i > 1) {
      const m1 = slope(p0, p1, point);
      const m0 = m === undefined ? slopeSingle(p0, p1, m1) : m;
      const command = bezier(p0, p1, m0, m1, t);
      m = m1;
      return `${acc} ${command}`;
    }
    return acc;
  }, `M ${minX},${minY} L ${minX},${startY} L ${startX},${startY}`);
  // close the path
  const last = data[data.length - 1];
  const end = bezier(p1, last, m, slopeSingle(p1, last, m), t);
  return `${commands} ${end} L ${maxX},${minY} Z`;
}

const graphCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host{display:block;block-size:100%}.svg{fill:currentColor;stroke:transparent;margin:0px;display:block;block-size:100%;inline-size:100%;padding:0px}.svg .graph-path--highlight{fill:var(--calcite-ui-brand);opacity:0.5}";

const Graph = class {
  constructor(hostRef) {
    registerInstance(this, hostRef);
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /**
     * Array of tuples describing a single data point ([x, y])
     * These data points should be sorted by x-axis value
     */
    this.data = [];
    //--------------------------------------------------------------------------
    //
    //  Private State/Props
    //
    //--------------------------------------------------------------------------
    this.graphId = `calcite-graph-${guid()}`;
    this.resizeObserver = createObserver("resize", () => forceUpdate(this));
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    var _a;
    (_a = this.resizeObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el);
  }
  disconnectedCallback() {
    var _a;
    (_a = this.resizeObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
  }
  render() {
    const { data, colorStops, el, highlightMax, highlightMin, min, max } = this;
    const id = this.graphId;
    const { clientHeight: height, clientWidth: width } = el;
    // if we have no data, return empty svg
    if (!data || data.length === 0) {
      return (h("svg", { class: "svg", height: height, preserveAspectRatio: "none", viewBox: `0 0 ${width} ${height}`, width: width }));
    }
    const { min: rangeMin, max: rangeMax } = range(data);
    let currentMin = rangeMin;
    let currentMax = rangeMax;
    if (min < rangeMin[0] || min > rangeMin[0]) {
      currentMin = [min, 0];
    }
    if (max > rangeMax[0] || max < rangeMax[0]) {
      currentMax = [max, rangeMax[1]];
    }
    const t = translate({ min: currentMin, max: currentMax, width, height });
    const [hMinX] = t([highlightMin, currentMax[1]]);
    const [hMaxX] = t([highlightMax, currentMax[1]]);
    const areaPath = area({ data, min: rangeMin, max: rangeMax, t });
    const fill = colorStops ? `url(#linear-gradient-${id})` : undefined;
    return (h("svg", { class: "svg", height: height, preserveAspectRatio: "none", viewBox: `0 0 ${width} ${height}`, width: width }, colorStops ? (h("defs", null, h("linearGradient", { id: `linear-gradient-${id}`, x1: "0", x2: "1", y1: "0", y2: "0" }, colorStops.map(({ offset, color, opacity }) => (h("stop", { offset: `${offset * 100}%`, "stop-color": color, "stop-opacity": opacity })))))) : null, highlightMin !== undefined ? ([
      h("mask", { height: "100%", id: `${id}1`, width: "100%", x: "0%", y: "0%" }, h("path", { d: `
            M 0,0
            L ${hMinX - 1},0
            L ${hMinX - 1},${height}
            L 0,${height}
            Z
          `, fill: "white" })),
      h("mask", { height: "100%", id: `${id}2`, width: "100%", x: "0%", y: "0%" }, h("path", { d: `
            M ${hMinX + 1},0
            L ${hMaxX - 1},0
            L ${hMaxX - 1},${height}
            L ${hMinX + 1}, ${height}
            Z
          `, fill: "white" })),
      h("mask", { height: "100%", id: `${id}3`, width: "100%", x: "0%", y: "0%" }, h("path", { d: `
                M ${hMaxX + 1},0
                L ${width},0
                L ${width},${height}
                L ${hMaxX + 1}, ${height}
                Z
              `, fill: "white" })),
      h("path", { class: "graph-path", d: areaPath, fill: fill, mask: `url(#${id}1)` }),
      h("path", { class: "graph-path--highlight", d: areaPath, fill: fill, mask: `url(#${id}2)` }),
      h("path", { class: "graph-path", d: areaPath, fill: fill, mask: `url(#${id}3)` })
    ]) : (h("path", { class: "graph-path", d: areaPath, fill: fill }))));
  }
  get el() { return getElement(this); }
};
Graph.style = graphCss;

const CSS = {
  handleLabel: "handle__label",
  handleLabelMinValue: "handle__label--minValue",
  handleLabelValue: "handle__label--value",
  tickMin: "tick__label--min",
  tickMax: "tick__label--max"
};

const sliderCss = "@charset \"UTF-8\";@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}.scale--s{--calcite-slider-handle-size:10px;--calcite-slider-handle-extension-height:6.5px;--calcite-slider-container-font-size:var(--calcite-font-size--3)}.scale--s .handle__label,.scale--s .tick__label{line-height:.75rem}.scale--m{--calcite-slider-handle-size:14px;--calcite-slider-handle-extension-height:8px;--calcite-slider-container-font-size:var(--calcite-font-size--2)}.scale--m .handle__label,.scale--m .tick__label{line-height:1rem}.scale--l{--calcite-slider-handle-size:16px;--calcite-slider-handle-extension-height:10.5px;--calcite-slider-container-font-size:var(--calcite-font-size--1)}.scale--l .handle__label,.scale--l .tick__label{line-height:1rem}.handle__label,.tick__label{font-weight:var(--calcite-font-weight-medium);color:var(--calcite-ui-text-2);font-size:var(--calcite-slider-container-font-size)}:host{display:block}.container{position:relative;display:block;overflow-wrap:normal;word-break:normal;padding-inline:calc(var(--calcite-slider-handle-size) * 0.5);padding-block:calc(var(--calcite-slider-handle-size) * 0.5);margin-block:calc(var(--calcite-slider-handle-size) * 0.5);margin-inline:0;--calcite-slider-full-handle-height:calc(\n    var(--calcite-slider-handle-size) + var(--calcite-slider-handle-extension-height)\n  )}:host([disabled]) .track__range,:host([disabled]) .tick--active{background-color:var(--calcite-ui-text-3)}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}.scale--s .thumb:not(.thumb--precise){--calcite-slider-thumb-y-offset:-6px}.scale--m .thumb:not(.thumb--precise){--calcite-slider-thumb-y-offset:-8px}.scale--l .thumb:not(.thumb--precise){--calcite-slider-thumb-y-offset:-9px}:host([precise]:not([has-histogram])) .container .thumb--value{--calcite-slider-thumb-y-offset:calc(var(--calcite-slider-full-handle-height) * -1)}.thumb-container{position:relative;max-inline-size:100%}.thumb{--calcite-slider-thumb-x-offset:calc(var(--calcite-slider-handle-size) * 0.5);position:absolute;margin:0px;display:flex;cursor:pointer;flex-direction:column;align-items:center;border-style:none;background-color:transparent;padding:0px;font-family:inherit;outline:2px solid transparent;outline-offset:2px;transform:translate(var(--calcite-slider-thumb-x-offset), var(--calcite-slider-thumb-y-offset))}.thumb .handle__label.static,.thumb .handle__label.transformed{position:absolute;inset-block:0px;opacity:0}.thumb .handle__label.hyphen::after{content:\"—\";display:inline-block;inline-size:1em}.thumb .handle__label.hyphen--wrap{display:flex}.thumb .handle{box-sizing:border-box;border-radius:9999px;background-color:var(--calcite-ui-foreground-1);outline-color:transparent;block-size:var(--calcite-slider-handle-size);inline-size:var(--calcite-slider-handle-size);box-shadow:0 0 0 2px var(--calcite-ui-text-3) inset;transition:border var(--calcite-internal-animation-timing-medium) ease, background-color var(--calcite-internal-animation-timing-medium) ease, box-shadow var(--calcite-animation-timing) ease}.thumb .handle-extension{inline-size:0.125rem;block-size:var(--calcite-slider-handle-extension-height);background-color:var(--calcite-ui-text-3)}.thumb:hover .handle{box-shadow:0 0 0 3px var(--calcite-ui-brand) inset}.thumb:hover .handle-extension{background-color:var(--calcite-ui-brand)}.thumb:focus .handle{outline:2px solid var(--calcite-ui-brand);outline-offset:2px}.thumb:focus .handle-extension{background-color:var(--calcite-ui-brand)}.thumb.thumb--minValue{transform:translate(calc(var(--calcite-slider-thumb-x-offset) * -1), var(--calcite-slider-thumb-y-offset))}.thumb.thumb--precise{--calcite-slider-thumb-y-offset:-2px}:host([label-handles]) .thumb{--calcite-slider-thumb-x-offset:50%}:host([label-handles]):host(:not([has-histogram])) .scale--s .thumb:not(.thumb--precise){--calcite-slider-thumb-y-offset:-23px}:host([label-handles]):host(:not([has-histogram])) .scale--m .thumb:not(.thumb--precise){--calcite-slider-thumb-y-offset:-30px}:host([label-handles]):host(:not([has-histogram])) .scale--l .thumb:not(.thumb--precise){--calcite-slider-thumb-y-offset:-32px}:host([has-histogram][label-handles]) .handle__label,:host([label-handles]:not([has-histogram])) .thumb--minValue.thumb--precise .handle__label{margin-block-start:0.5em}:host(:not([has-histogram]):not([precise])) .handle__label,:host([label-handles]:not([has-histogram])) .thumb--value .handle__label{margin-block-end:0.5em}:host([label-handles][precise]):host(:not([has-histogram])) .scale--s .thumb--value{--calcite-slider-thumb-y-offset:-33px}:host([label-handles][precise]):host(:not([has-histogram])) .scale--m .thumb--value{--calcite-slider-thumb-y-offset:-44px}:host([label-handles][precise]):host(:not([has-histogram])) .scale--l .thumb--value{--calcite-slider-thumb-y-offset:-49px}.thumb:focus .handle,.thumb--active .handle{background-color:var(--calcite-ui-brand);box-shadow:0 0 8px 0 rgba(0, 0, 0, 0.16)}.thumb:hover.thumb--precise:after,.thumb:focus.thumb--precise:after,.thumb--active.thumb--precise:after{background-color:var(--calcite-ui-brand)}.track{position:relative;block-size:0.125rem;border-radius:0px;background-color:var(--calcite-ui-border-2);transition:all var(--calcite-internal-animation-timing-medium) ease-in}.track__range{position:absolute;inset-block-start:0px;block-size:0.125rem;background-color:var(--calcite-ui-brand)}.container--range .track__range:hover{cursor:ew-resize}.container--range .track__range:after{position:absolute;inline-size:100%;content:\"\";inset-block-start:calc(var(--calcite-slider-full-handle-height) * 0.5 * -1);block-size:calc(var(--calcite-slider-handle-size) + var(--calcite-slider-handle-extension-height))}@media (forced-colors: active){.thumb{outline-width:0;outline-offset:0}.handle{outline:2px solid transparent;outline-offset:2px}.thumb:focus .handle,.thumb .handle-extension,.thumb:hover .handle-extension,.thumb:focus .handle-extension,.thumb:active .handle-extension{background-color:canvasText}.track{background-color:canvasText}.track__range{background-color:highlight}}.tick{position:absolute;block-size:0.25rem;inline-size:0.125rem;border-width:1px;border-style:solid;background-color:var(--calcite-ui-border-input);border-color:var(--calcite-ui-foreground-1);inset-block-start:-2px;pointer-events:none;margin-inline-start:calc(-1 * 0.125rem)}.tick--active{background-color:var(--calcite-ui-brand)}.tick__label{pointer-events:none;margin-block-start:0.875rem;display:flex;justify-content:center}.tick__label--min{transition:opacity var(--calcite-animation-timing)}.tick__label--max{transition:opacity var(--calcite-internal-animation-timing-fast)}:host([has-histogram][label-handles]) .tick__label--min,:host([has-histogram][label-handles]) .tick__label--max,:host([has-histogram][precise]) .tick__label--min,:host([has-histogram][precise]) .tick__label--max{font-weight:var(--calcite-font-weight-normal);color:var(--calcite-ui-text-3)}.graph{color:var(--calcite-ui-foreground-3);block-size:48px}:host([label-ticks][ticks]) .container{padding-block-end:calc(0.875rem + var(--calcite-slider-container-font-size))}:host([has-histogram]):host([precise][label-handles]) .container{padding-block-end:calc(var(--calcite-slider-full-handle-height) + 1em)}:host([has-histogram]):host([label-handles]:not([precise])) .container{padding-block-end:calc(var(--calcite-slider-handle-size) * 0.5 + 1em)}:host([has-histogram]):host([precise]:not([label-handles])) .container{padding-block-end:var(--calcite-slider-full-handle-height)}:host(:not([has-histogram])):host([precise]:not([label-handles])) .container{padding-block-start:var(--calcite-slider-full-handle-height)}:host(:not([has-histogram])):host([precise]:not([label-handles])) .container--range{padding-block-end:var(--calcite-slider-full-handle-height)}:host(:not([has-histogram])):host([label-handles]:not([precise])) .container{padding-block-start:calc(var(--calcite-slider-full-handle-height) + 4px)}:host(:not([has-histogram])):host([label-handles][precise]) .container{padding-block-start:calc(var(--calcite-slider-full-handle-height) + var(--calcite-slider-container-font-size) + 4px)}:host(:not([has-histogram])):host([label-handles][precise]) .container--range{padding-block-end:calc(var(--calcite-slider-full-handle-height) + var(--calcite-slider-container-font-size) + 4px)}::slotted(input[slot=hidden-form-input]){margin:0 !important;opacity:0 !important;outline:none !important;padding:0 !important;position:absolute !important;inset:0 !important;transform:none !important;-webkit-appearance:none !important;z-index:-1 !important}";

function isRange(value) {
  return Array.isArray(value);
}
const Slider = class {
  constructor(hostRef) {
    registerInstance(this, hostRef);
    this.calciteSliderInput = createEvent(this, "calciteSliderInput", 6);
    this.calciteSliderChange = createEvent(this, "calciteSliderChange", 6);
    this.calciteSliderUpdate = createEvent(this, "calciteSliderUpdate", 6);
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
    this.disabled = false;
    /**
     * When `true`, number values are displayed with a group separator corresponding to the language and country format.
     */
    this.groupSeparator = false;
    /** When `true`, indicates a histogram is present. */
    this.hasHistogram = false;
    /** When `true`, displays label handles with their numeric value. */
    this.labelHandles = false;
    /** When `true` and `ticks` is specified, displays label tick marks with their numeric value. */
    this.labelTicks = false;
    /** The component's maximum selectable value. */
    this.max = 100;
    /** The component's minimum selectable value. */
    this.min = 0;
    /**
     * When `true`, the slider will display values from high to low.
     *
     * Note that this value will be ignored if the slider has an associated histogram.
     */
    this.mirrored = false;
    /** When `true`, sets a finer point for handles. */
    this.precise = false;
    /**
     * When `true`, the component must have a value in order for the form to submit.
     */
    this.required = false;
    /** When `true`, enables snap selection in coordination with `step` via a mouse. */
    this.snap = false;
    /** Specifies the interval to move with the up, or down keys. */
    this.step = 1;
    /** The component's value. */
    this.value = 0;
    /**
     *  Specifies the size of the component.
     */
    this.scale = "m";
    this.activeProp = "value";
    this.guid = `calcite-slider-${guid()}`;
    this.effectiveLocale = "";
    this.minMaxValueRange = null;
    this.minValueDragRange = null;
    this.maxValueDragRange = null;
    this.tickValues = [];
    this.dragUpdate = (event) => {
      event.preventDefault();
      if (this.dragProp) {
        const value = this.translate(event.clientX || event.pageX);
        if (isRange(this.value) && this.dragProp === "minMaxValue") {
          if (this.minValueDragRange && this.maxValueDragRange && this.minMaxValueRange) {
            const newMinValue = value - this.minValueDragRange;
            const newMaxValue = value + this.maxValueDragRange;
            if (newMaxValue <= this.max &&
              newMinValue >= this.min &&
              newMaxValue - newMinValue === this.minMaxValueRange) {
              this.setValue({
                minValue: this.clamp(newMinValue, "minValue"),
                maxValue: this.clamp(newMaxValue, "maxValue")
              });
            }
          }
          else {
            this.minValueDragRange = value - this.minValue;
            this.maxValueDragRange = this.maxValue - value;
            this.minMaxValueRange = this.maxValue - this.minValue;
          }
        }
        else {
          this.setValue({ [this.dragProp]: this.clamp(value, this.dragProp) });
        }
      }
    };
    this.pointerUpDragEnd = (event) => {
      if (!isPrimaryPointerButton(event)) {
        return;
      }
      this.dragEnd(event);
    };
    this.dragEnd = (event) => {
      this.removeDragListeners();
      this.focusActiveHandle(event.clientX);
      if (this.lastDragPropValue != this[this.dragProp]) {
        this.emitChange();
      }
      this.dragProp = null;
      this.lastDragPropValue = null;
      this.minValueDragRange = null;
      this.maxValueDragRange = null;
      this.minMaxValueRange = null;
    };
    /**
     * Set the reference of the track Element
     *
     * @internal
     * @param node
     */
    this.storeTrackRef = (node) => {
      this.trackEl = node;
    };
    /**
     * Returns a string representing the localized label value based if the groupSeparator prop is parsed.
     *
     * @param value
     */
    this.determineGroupSeparator = (value) => {
      if (typeof value === "number") {
        numberStringFormatter.numberFormatOptions = {
          locale: this.effectiveLocale,
          numberingSystem: this.numberingSystem,
          useGrouping: this.groupSeparator
        };
        return numberStringFormatter.localize(value.toString());
      }
    };
  }
  histogramWatcher(newHistogram) {
    this.hasHistogram = !!newHistogram;
  }
  valueHandler() {
    this.setMinMaxFromValue();
  }
  minMaxValueHandler() {
    this.setValueFromMinMax();
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    connectLocalized(this);
    this.setMinMaxFromValue();
    this.setValueFromMinMax();
    connectLabel(this);
    connectForm(this);
  }
  disconnectedCallback() {
    disconnectLabel(this);
    disconnectForm(this);
    disconnectLocalized(this);
    this.removeDragListeners();
  }
  componentWillLoad() {
    this.tickValues = this.generateTickValues();
    if (!isRange(this.value)) {
      this.value = this.clamp(this.value);
    }
    afterConnectDefaultValueSet(this, this.value);
    if (this.snap && !isRange(this.value)) {
      this.value = this.getClosestStep(this.value);
    }
    if (this.histogram) {
      this.hasHistogram = true;
    }
  }
  componentDidRender() {
    if (this.labelHandles) {
      this.adjustHostObscuredHandleLabel("value");
      if (isRange(this.value)) {
        this.adjustHostObscuredHandleLabel("minValue");
        if (!(this.precise && !this.hasHistogram)) {
          this.hyphenateCollidingRangeHandleLabels();
        }
      }
    }
    this.hideObscuredBoundingTickLabels();
    updateHostInteraction(this);
  }
  render() {
    const id = this.el.id || this.guid;
    const maxProp = isRange(this.value) ? "maxValue" : "value";
    const value = isRange(this.value) ? this.maxValue : this.value;
    const displayedValue = this.determineGroupSeparator(value);
    const displayedMinValue = this.determineGroupSeparator(this.minValue);
    const min = this.minValue || this.min;
    const useMinValue = this.shouldUseMinValue();
    const minInterval = this.getUnitInterval(useMinValue ? this.minValue : min) * 100;
    const maxInterval = this.getUnitInterval(value) * 100;
    const mirror = this.shouldMirror();
    const leftThumbOffset = `${mirror ? 100 - minInterval : minInterval}%`;
    const rightThumbOffset = `${mirror ? maxInterval : 100 - maxInterval}%`;
    const valueIsRange = isRange(this.value);
    const handleLabelMinValueClasses = `${CSS.handleLabel} ${CSS.handleLabelMinValue}`;
    const handleLabelValueClasses = `${CSS.handleLabel} ${CSS.handleLabelValue}`;
    const handle = (h("div", { "aria-disabled": this.disabled, "aria-label": valueIsRange ? this.maxLabel : this.minLabel, "aria-orientation": "horizontal", "aria-valuemax": this.max, "aria-valuemin": this.min, "aria-valuenow": value, class: {
        thumb: true,
        "thumb--value": true,
        "thumb--active": this.lastDragProp !== "minMaxValue" && this.dragProp === maxProp
      }, onBlur: () => (this.activeProp = null), onFocus: () => (this.activeProp = maxProp), onPointerDown: (event) => this.pointerDownDragStart(event, maxProp), ref: (el) => (this.maxHandle = el), role: "slider", style: { right: rightThumbOffset }, tabIndex: 0 }, h("div", { class: "handle" })));
    const labeledHandle = (h("div", { "aria-disabled": this.disabled, "aria-label": valueIsRange ? this.maxLabel : this.minLabel, "aria-orientation": "horizontal", "aria-valuemax": this.max, "aria-valuemin": this.min, "aria-valuenow": value, class: {
        thumb: true,
        "thumb--value": true,
        "thumb--active": this.lastDragProp !== "minMaxValue" && this.dragProp === maxProp
      }, onBlur: () => (this.activeProp = null), onFocus: () => (this.activeProp = maxProp), onPointerDown: (event) => this.pointerDownDragStart(event, maxProp), ref: (el) => (this.maxHandle = el), role: "slider", style: { right: rightThumbOffset }, tabIndex: 0 }, h("span", { "aria-hidden": "true", class: handleLabelValueClasses }, displayedValue), h("span", { "aria-hidden": "true", class: `${handleLabelValueClasses} static` }, displayedValue), h("span", { "aria-hidden": "true", class: `${handleLabelValueClasses} transformed` }, displayedValue), h("div", { class: "handle" })));
    const histogramLabeledHandle = (h("div", { "aria-disabled": this.disabled, "aria-label": valueIsRange ? this.maxLabel : this.minLabel, "aria-orientation": "horizontal", "aria-valuemax": this.max, "aria-valuemin": this.min, "aria-valuenow": value, class: {
        thumb: true,
        "thumb--value": true,
        "thumb--active": this.lastDragProp !== "minMaxValue" && this.dragProp === maxProp
      }, onBlur: () => (this.activeProp = null), onFocus: () => (this.activeProp = maxProp), onPointerDown: (event) => this.pointerDownDragStart(event, maxProp), ref: (el) => (this.maxHandle = el), role: "slider", style: { right: rightThumbOffset }, tabIndex: 0 }, h("div", { class: "handle" }), h("span", { "aria-hidden": "true", class: handleLabelValueClasses }, displayedValue), h("span", { "aria-hidden": "true", class: `${handleLabelValueClasses} static` }, displayedValue), h("span", { "aria-hidden": "true", class: `${handleLabelValueClasses} transformed` }, displayedValue)));
    const preciseHandle = (h("div", { "aria-disabled": this.disabled, "aria-label": valueIsRange ? this.maxLabel : this.minLabel, "aria-orientation": "horizontal", "aria-valuemax": this.max, "aria-valuemin": this.min, "aria-valuenow": value, class: {
        thumb: true,
        "thumb--value": true,
        "thumb--active": this.lastDragProp !== "minMaxValue" && this.dragProp === maxProp,
        "thumb--precise": true
      }, onBlur: () => (this.activeProp = null), onFocus: () => (this.activeProp = maxProp), onPointerDown: (event) => this.pointerDownDragStart(event, maxProp), ref: (el) => (this.maxHandle = el), role: "slider", style: { right: rightThumbOffset }, tabIndex: 0 }, h("div", { class: "handle" }), h("div", { class: "handle-extension" })));
    const histogramPreciseHandle = (h("div", { "aria-disabled": this.disabled, "aria-label": valueIsRange ? this.maxLabel : this.minLabel, "aria-orientation": "horizontal", "aria-valuemax": this.max, "aria-valuemin": this.min, "aria-valuenow": value, class: {
        thumb: true,
        "thumb--value": true,
        "thumb--active": this.lastDragProp !== "minMaxValue" && this.dragProp === maxProp,
        "thumb--precise": true
      }, onBlur: () => (this.activeProp = null), onFocus: () => (this.activeProp = maxProp), onPointerDown: (event) => this.pointerDownDragStart(event, maxProp), ref: (el) => (this.maxHandle = el), role: "slider", style: { right: rightThumbOffset }, tabIndex: 0 }, h("div", { class: "handle-extension" }), h("div", { class: "handle" })));
    const labeledPreciseHandle = (h("div", { "aria-disabled": this.disabled, "aria-label": valueIsRange ? this.maxLabel : this.minLabel, "aria-orientation": "horizontal", "aria-valuemax": this.max, "aria-valuemin": this.min, "aria-valuenow": value, class: {
        thumb: true,
        "thumb--value": true,
        "thumb--active": this.lastDragProp !== "minMaxValue" && this.dragProp === maxProp,
        "thumb--precise": true
      }, onBlur: () => (this.activeProp = null), onFocus: () => (this.activeProp = maxProp), onPointerDown: (event) => this.pointerDownDragStart(event, maxProp), ref: (el) => (this.maxHandle = el), role: "slider", style: { right: rightThumbOffset }, tabIndex: 0 }, h("span", { "aria-hidden": "true", class: handleLabelValueClasses }, displayedValue), h("span", { "aria-hidden": "true", class: `${handleLabelValueClasses} static` }, displayedValue), h("span", { "aria-hidden": "true", class: `${handleLabelValueClasses} transformed` }, displayedValue), h("div", { class: "handle" }), h("div", { class: "handle-extension" })));
    const histogramLabeledPreciseHandle = (h("div", { "aria-disabled": this.disabled, "aria-label": valueIsRange ? this.maxLabel : this.minLabel, "aria-orientation": "horizontal", "aria-valuemax": this.max, "aria-valuemin": this.min, "aria-valuenow": value, class: {
        thumb: true,
        "thumb--value": true,
        "thumb--active": this.lastDragProp !== "minMaxValue" && this.dragProp === maxProp,
        "thumb--precise": true
      }, onBlur: () => (this.activeProp = null), onFocus: () => (this.activeProp = maxProp), onPointerDown: (event) => this.pointerDownDragStart(event, maxProp), ref: (el) => (this.maxHandle = el), role: "slider", style: { right: rightThumbOffset }, tabIndex: 0 }, h("div", { class: "handle-extension" }), h("div", { class: "handle" }), h("span", { "aria-hidden": "true", class: handleLabelValueClasses }, displayedValue), h("span", { "aria-hidden": "true", class: `${handleLabelValueClasses} static` }, displayedValue), h("span", { "aria-hidden": "true", class: `${handleLabelValueClasses} transformed` }, displayedValue)));
    const minHandle = (h("div", { "aria-disabled": this.disabled, "aria-label": this.minLabel, "aria-orientation": "horizontal", "aria-valuemax": this.max, "aria-valuemin": this.min, "aria-valuenow": this.minValue, class: {
        thumb: true,
        "thumb--minValue": true,
        "thumb--active": this.dragProp === "minValue"
      }, onBlur: () => (this.activeProp = null), onFocus: () => (this.activeProp = "minValue"), onPointerDown: (event) => this.pointerDownDragStart(event, "minValue"), ref: (el) => (this.minHandle = el), role: "slider", style: { left: leftThumbOffset }, tabIndex: 0 }, h("div", { class: "handle" })));
    const minLabeledHandle = (h("div", { "aria-disabled": this.disabled, "aria-label": this.minLabel, "aria-orientation": "horizontal", "aria-valuemax": this.max, "aria-valuemin": this.min, "aria-valuenow": this.minValue, class: {
        thumb: true,
        "thumb--minValue": true,
        "thumb--active": this.dragProp === "minValue"
      }, onBlur: () => (this.activeProp = null), onFocus: () => (this.activeProp = "minValue"), onPointerDown: (event) => this.pointerDownDragStart(event, "minValue"), ref: (el) => (this.minHandle = el), role: "slider", style: { left: leftThumbOffset }, tabIndex: 0 }, h("span", { "aria-hidden": "true", class: handleLabelMinValueClasses }, displayedMinValue), h("span", { "aria-hidden": "true", class: `${handleLabelMinValueClasses} static` }, displayedMinValue), h("span", { "aria-hidden": "true", class: `${handleLabelMinValueClasses} transformed` }, displayedMinValue), h("div", { class: "handle" })));
    const minHistogramLabeledHandle = (h("div", { "aria-disabled": this.disabled, "aria-label": this.minLabel, "aria-orientation": "horizontal", "aria-valuemax": this.max, "aria-valuemin": this.min, "aria-valuenow": this.minValue, class: {
        thumb: true,
        "thumb--minValue": true,
        "thumb--active": this.dragProp === "minValue"
      }, onBlur: () => (this.activeProp = null), onFocus: () => (this.activeProp = "minValue"), onPointerDown: (event) => this.pointerDownDragStart(event, "minValue"), ref: (el) => (this.minHandle = el), role: "slider", style: { left: leftThumbOffset }, tabIndex: 0 }, h("div", { class: "handle" }), h("span", { "aria-hidden": "true", class: handleLabelMinValueClasses }, displayedMinValue), h("span", { "aria-hidden": "true", class: `${handleLabelMinValueClasses} static` }, displayedMinValue), h("span", { "aria-hidden": "true", class: `${handleLabelMinValueClasses} transformed` }, displayedMinValue)));
    const minPreciseHandle = (h("div", { "aria-disabled": this.disabled, "aria-label": this.minLabel, "aria-orientation": "horizontal", "aria-valuemax": this.max, "aria-valuemin": this.min, "aria-valuenow": this.minValue, class: {
        thumb: true,
        "thumb--minValue": true,
        "thumb--active": this.dragProp === "minValue",
        "thumb--precise": true
      }, onBlur: () => (this.activeProp = null), onFocus: () => (this.activeProp = "minValue"), onPointerDown: (event) => this.pointerDownDragStart(event, "minValue"), ref: (el) => (this.minHandle = el), role: "slider", style: { left: leftThumbOffset }, tabIndex: 0 }, h("div", { class: "handle-extension" }), h("div", { class: "handle" })));
    const minLabeledPreciseHandle = (h("div", { "aria-disabled": this.disabled, "aria-label": this.minLabel, "aria-orientation": "horizontal", "aria-valuemax": this.max, "aria-valuemin": this.min, "aria-valuenow": this.minValue, class: {
        thumb: true,
        "thumb--minValue": true,
        "thumb--active": this.dragProp === "minValue",
        "thumb--precise": true
      }, onBlur: () => (this.activeProp = null), onFocus: () => (this.activeProp = "minValue"), onPointerDown: (event) => this.pointerDownDragStart(event, "minValue"), ref: (el) => (this.minHandle = el), role: "slider", style: { left: leftThumbOffset }, tabIndex: 0 }, h("div", { class: "handle-extension" }), h("div", { class: "handle" }), h("span", { "aria-hidden": "true", class: handleLabelMinValueClasses }, displayedMinValue), h("span", { "aria-hidden": "true", class: `${handleLabelMinValueClasses} static` }, displayedMinValue), h("span", { "aria-hidden": "true", class: `${handleLabelMinValueClasses} transformed` }, displayedMinValue)));
    return (h(Host, { id: id, onTouchStart: this.handleTouchStart }, h("div", { class: {
        ["container"]: true,
        ["container--range"]: valueIsRange,
        [`scale--${this.scale}`]: true
      } }, this.renderGraph(), h("div", { class: "track", ref: this.storeTrackRef }, h("div", { class: "track__range", onPointerDown: (event) => this.pointerDownDragStart(event, "minMaxValue"), style: {
        left: `${mirror ? 100 - maxInterval : minInterval}%`,
        right: `${mirror ? minInterval : 100 - maxInterval}%`
      } }), h("div", { class: "ticks" }, this.tickValues.map((tick) => {
      const tickOffset = `${this.getUnitInterval(tick) * 100}%`;
      let activeTicks = tick >= min && tick <= value;
      if (useMinValue) {
        activeTicks = tick >= this.minValue && tick <= this.maxValue;
      }
      return (h("span", { class: {
          tick: true,
          "tick--active": activeTicks
        }, style: {
          left: mirror ? "" : tickOffset,
          right: mirror ? tickOffset : ""
        } }, this.renderTickLabel(tick)));
    }))), h("div", { class: "thumb-container" }, !this.precise && !this.labelHandles && valueIsRange && minHandle, !this.hasHistogram &&
      !this.precise &&
      this.labelHandles &&
      valueIsRange &&
      minLabeledHandle, this.precise && !this.labelHandles && valueIsRange && minPreciseHandle, this.precise && this.labelHandles && valueIsRange && minLabeledPreciseHandle, this.hasHistogram &&
      !this.precise &&
      this.labelHandles &&
      valueIsRange &&
      minHistogramLabeledHandle, !this.precise && !this.labelHandles && handle, !this.hasHistogram && !this.precise && this.labelHandles && labeledHandle, !this.hasHistogram && this.precise && !this.labelHandles && preciseHandle, this.hasHistogram && this.precise && !this.labelHandles && histogramPreciseHandle, !this.hasHistogram && this.precise && this.labelHandles && labeledPreciseHandle, this.hasHistogram && !this.precise && this.labelHandles && histogramLabeledHandle, this.hasHistogram &&
      this.precise &&
      this.labelHandles &&
      histogramLabeledPreciseHandle, h(HiddenFormInputSlot, { component: this })))));
  }
  renderGraph() {
    return this.histogram ? (h("calcite-graph", { class: "graph", colorStops: this.histogramStops, data: this.histogram, highlightMax: isRange(this.value) ? this.maxValue : this.value, highlightMin: isRange(this.value) ? this.minValue : this.min, max: this.max, min: this.min })) : null;
  }
  renderTickLabel(tick) {
    const valueIsRange = isRange(this.value);
    const isMinTickLabel = tick === this.min;
    const isMaxTickLabel = tick === this.max;
    const displayedTickValue = this.determineGroupSeparator(tick);
    const tickLabel = (h("span", { class: {
        tick__label: true,
        [CSS.tickMin]: isMinTickLabel,
        [CSS.tickMax]: isMaxTickLabel
      } }, displayedTickValue));
    if (this.labelTicks && !this.hasHistogram && !valueIsRange) {
      return tickLabel;
    }
    if (this.labelTicks &&
      !this.hasHistogram &&
      valueIsRange &&
      !this.precise &&
      !this.labelHandles) {
      return tickLabel;
    }
    if (this.labelTicks &&
      !this.hasHistogram &&
      valueIsRange &&
      !this.precise &&
      this.labelHandles) {
      return tickLabel;
    }
    if (this.labelTicks &&
      !this.hasHistogram &&
      valueIsRange &&
      this.precise &&
      (isMinTickLabel || isMaxTickLabel)) {
      return tickLabel;
    }
    if (this.labelTicks && this.hasHistogram && !this.precise && !this.labelHandles) {
      return tickLabel;
    }
    if (this.labelTicks &&
      this.hasHistogram &&
      this.precise &&
      !this.labelHandles &&
      (isMinTickLabel || isMaxTickLabel)) {
      return tickLabel;
    }
    if (this.labelTicks &&
      this.hasHistogram &&
      !this.precise &&
      this.labelHandles &&
      (isMinTickLabel || isMaxTickLabel)) {
      return tickLabel;
    }
    if (this.labelTicks &&
      this.hasHistogram &&
      this.precise &&
      this.labelHandles &&
      (isMinTickLabel || isMaxTickLabel)) {
      return tickLabel;
    }
    return null;
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  keyDownHandler(event) {
    const mirror = this.shouldMirror();
    const { activeProp, max, min, pageStep, step } = this;
    const value = this[activeProp];
    const { key } = event;
    if (isActivationKey(key)) {
      event.preventDefault();
      return;
    }
    let adjustment;
    if (key === "ArrowUp" || key === "ArrowRight") {
      const directionFactor = mirror && key === "ArrowRight" ? -1 : 1;
      adjustment = value + step * directionFactor;
    }
    else if (key === "ArrowDown" || key === "ArrowLeft") {
      const directionFactor = mirror && key === "ArrowLeft" ? -1 : 1;
      adjustment = value - step * directionFactor;
    }
    else if (key === "PageUp") {
      if (pageStep) {
        adjustment = value + pageStep;
      }
    }
    else if (key === "PageDown") {
      if (pageStep) {
        adjustment = value - pageStep;
      }
    }
    else if (key === "Home") {
      adjustment = min;
    }
    else if (key === "End") {
      adjustment = max;
    }
    if (isNaN(adjustment)) {
      return;
    }
    event.preventDefault();
    const fixedDecimalAdjustment = Number(adjustment.toFixed(decimalPlaces(step)));
    this.setValue({
      [activeProp]: this.clamp(fixedDecimalAdjustment, activeProp)
    });
  }
  pointerDownHandler(event) {
    if (!isPrimaryPointerButton(event)) {
      return;
    }
    const x = event.clientX || event.pageX;
    const position = this.translate(x);
    let prop = "value";
    if (isRange(this.value)) {
      const inRange = position >= this.minValue && position <= this.maxValue;
      if (inRange && this.lastDragProp === "minMaxValue") {
        prop = "minMaxValue";
      }
      else {
        const closerToMax = Math.abs(this.maxValue - position) < Math.abs(this.minValue - position);
        prop = closerToMax || position > this.maxValue ? "maxValue" : "minValue";
      }
    }
    this.lastDragPropValue = this[prop];
    this.dragStart(prop);
    const isThumbActive = this.el.shadowRoot.querySelector(".thumb:active");
    if (!isThumbActive) {
      this.setValue({ [prop]: this.clamp(position, prop) });
    }
    this.focusActiveHandle(x);
  }
  handleTouchStart(event) {
    // needed to prevent extra click at the end of a handle drag
    event.preventDefault();
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    const handle = this.minHandle ? this.minHandle : this.maxHandle;
    handle === null || handle === void 0 ? void 0 : handle.focus();
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  setValueFromMinMax() {
    const { minValue, maxValue } = this;
    if (typeof minValue === "number" && typeof maxValue === "number") {
      this.value = [minValue, maxValue];
    }
  }
  setMinMaxFromValue() {
    const { value } = this;
    if (isRange(value)) {
      this.minValue = value[0];
      this.maxValue = value[1];
    }
  }
  onLabelClick() {
    this.setFocus();
  }
  shouldMirror() {
    return this.mirrored && !this.hasHistogram;
  }
  shouldUseMinValue() {
    if (!isRange(this.value)) {
      return false;
    }
    return ((this.hasHistogram && this.maxValue === 0) || (!this.hasHistogram && this.minValue === 0));
  }
  generateTickValues() {
    const ticks = [];
    let current = this.min;
    while (this.ticks && current < this.max + this.ticks) {
      ticks.push(Math.min(current, this.max));
      current = current + this.ticks;
    }
    return ticks;
  }
  pointerDownDragStart(event, prop) {
    if (!isPrimaryPointerButton(event)) {
      return;
    }
    this.dragStart(prop);
  }
  dragStart(prop) {
    this.dragProp = prop;
    this.lastDragProp = this.dragProp;
    this.activeProp = prop;
    document.addEventListener("pointermove", this.dragUpdate);
    document.addEventListener("pointerup", this.pointerUpDragEnd);
    document.addEventListener("pointercancel", this.dragEnd);
  }
  focusActiveHandle(valueX) {
    switch (this.dragProp) {
      case "minValue":
        this.minHandle.focus();
        break;
      case "maxValue":
      case "value":
        this.maxHandle.focus();
        break;
      case "minMaxValue":
        this.getClosestHandle(valueX).focus();
        break;
    }
  }
  emitInput() {
    this.calciteSliderInput.emit();
    this.calciteSliderUpdate.emit();
  }
  emitChange() {
    this.calciteSliderChange.emit();
  }
  removeDragListeners() {
    document.removeEventListener("pointermove", this.dragUpdate);
    document.removeEventListener("pointerup", this.pointerUpDragEnd);
    document.removeEventListener("pointercancel", this.dragEnd);
  }
  /**
   * Set prop value(s) if changed at the component level
   *
   * @param {object} values - a set of key/value pairs delineating what properties in the component to update
   */
  setValue(values) {
    let valueChanged;
    Object.keys(values).forEach((propName) => {
      const newValue = values[propName];
      if (!valueChanged) {
        const oldValue = this[propName];
        valueChanged = oldValue !== newValue;
      }
      this[propName] = newValue;
    });
    if (!valueChanged) {
      return;
    }
    const dragging = this.dragProp;
    if (!dragging) {
      this.emitChange();
    }
    this.emitInput();
  }
  /**
   * If number is outside range, constrain to min or max
   *
   * @param value
   * @param prop
   * @internal
   */
  clamp(value, prop) {
    value = clamp(value, this.min, this.max);
    // ensure that maxValue and minValue don't swap positions
    if (prop === "maxValue") {
      value = Math.max(value, this.minValue);
    }
    if (prop === "minValue") {
      value = Math.min(value, this.maxValue);
    }
    return value;
  }
  /**
   * Translate a pixel position to value along the range
   *
   * @param x
   * @internal
   */
  translate(x) {
    const range = this.max - this.min;
    const { left, width } = this.trackEl.getBoundingClientRect();
    const percent = (x - left) / width;
    const mirror = this.shouldMirror();
    const clampedValue = this.clamp(this.min + range * (mirror ? 1 - percent : percent));
    let value = Number(clampedValue.toFixed(decimalPlaces(this.step)));
    if (this.snap && this.step) {
      value = this.getClosestStep(value);
    }
    return value;
  }
  /**
   * Get closest allowed value along stepped values
   *
   * @param num
   * @internal
   */
  getClosestStep(num) {
    num = Number(this.clamp(num).toFixed(decimalPlaces(this.step)));
    if (this.step) {
      const step = Math.round(num / this.step) * this.step;
      num = Number(this.clamp(step).toFixed(decimalPlaces(this.step)));
    }
    return num;
  }
  getClosestHandle(valueX) {
    return this.getDistanceX(this.maxHandle, valueX) > this.getDistanceX(this.minHandle, valueX)
      ? this.minHandle
      : this.maxHandle;
  }
  getDistanceX(el, valueX) {
    return Math.abs(el.getBoundingClientRect().left - valueX);
  }
  getFontSizeForElement(element) {
    return Number(window.getComputedStyle(element).getPropertyValue("font-size").match(/\d+/)[0]);
  }
  /**
   * Get position of value along range as fractional value
   *
   * @param num
   * @return {number} number in the unit interval [0,1]
   * @internal
   */
  getUnitInterval(num) {
    num = this.clamp(num);
    const range = this.max - this.min;
    return (num - this.min) / range;
  }
  adjustHostObscuredHandleLabel(name) {
    const label = this.el.shadowRoot.querySelector(`.handle__label--${name}`);
    const labelStatic = this.el.shadowRoot.querySelector(`.handle__label--${name}.static`);
    const labelTransformed = this.el.shadowRoot.querySelector(`.handle__label--${name}.transformed`);
    const labelStaticBounds = labelStatic.getBoundingClientRect();
    const labelStaticOffset = this.getHostOffset(labelStaticBounds.left, labelStaticBounds.right);
    label.style.transform = `translateX(${labelStaticOffset}px)`;
    labelTransformed.style.transform = `translateX(${labelStaticOffset}px)`;
  }
  hyphenateCollidingRangeHandleLabels() {
    const { shadowRoot } = this.el;
    const mirror = this.shouldMirror();
    const leftModifier = mirror ? "value" : "minValue";
    const rightModifier = mirror ? "minValue" : "value";
    const leftValueLabel = shadowRoot.querySelector(`.handle__label--${leftModifier}`);
    const leftValueLabelStatic = shadowRoot.querySelector(`.handle__label--${leftModifier}.static`);
    const leftValueLabelTransformed = shadowRoot.querySelector(`.handle__label--${leftModifier}.transformed`);
    const leftValueLabelStaticHostOffset = this.getHostOffset(leftValueLabelStatic.getBoundingClientRect().left, leftValueLabelStatic.getBoundingClientRect().right);
    const rightValueLabel = shadowRoot.querySelector(`.handle__label--${rightModifier}`);
    const rightValueLabelStatic = shadowRoot.querySelector(`.handle__label--${rightModifier}.static`);
    const rightValueLabelTransformed = shadowRoot.querySelector(`.handle__label--${rightModifier}.transformed`);
    const rightValueLabelStaticHostOffset = this.getHostOffset(rightValueLabelStatic.getBoundingClientRect().left, rightValueLabelStatic.getBoundingClientRect().right);
    const labelFontSize = this.getFontSizeForElement(leftValueLabel);
    const labelTransformedOverlap = this.getRangeLabelOverlap(leftValueLabelTransformed, rightValueLabelTransformed);
    const hyphenLabel = leftValueLabel;
    const labelOffset = labelFontSize / 2;
    if (labelTransformedOverlap > 0) {
      hyphenLabel.classList.add("hyphen", "hyphen--wrap");
      if (rightValueLabelStaticHostOffset === 0 && leftValueLabelStaticHostOffset === 0) {
        // Neither handle overlaps the host boundary
        let leftValueLabelTranslate = labelTransformedOverlap / 2 - labelOffset;
        leftValueLabelTranslate =
          Math.sign(leftValueLabelTranslate) === -1
            ? Math.abs(leftValueLabelTranslate)
            : -leftValueLabelTranslate;
        const leftValueLabelTransformedHostOffset = this.getHostOffset(leftValueLabelTransformed.getBoundingClientRect().left +
          leftValueLabelTranslate -
          labelOffset, leftValueLabelTransformed.getBoundingClientRect().right +
          leftValueLabelTranslate -
          labelOffset);
        let rightValueLabelTranslate = labelTransformedOverlap / 2;
        const rightValueLabelTransformedHostOffset = this.getHostOffset(rightValueLabelTransformed.getBoundingClientRect().left + rightValueLabelTranslate, rightValueLabelTransformed.getBoundingClientRect().right + rightValueLabelTranslate);
        if (leftValueLabelTransformedHostOffset !== 0) {
          leftValueLabelTranslate += leftValueLabelTransformedHostOffset;
          rightValueLabelTranslate += leftValueLabelTransformedHostOffset;
        }
        if (rightValueLabelTransformedHostOffset !== 0) {
          leftValueLabelTranslate += rightValueLabelTransformedHostOffset;
          rightValueLabelTranslate += rightValueLabelTransformedHostOffset;
        }
        leftValueLabel.style.transform = `translateX(${leftValueLabelTranslate}px)`;
        leftValueLabelTransformed.style.transform = `translateX(${leftValueLabelTranslate - labelOffset}px)`;
        rightValueLabel.style.transform = `translateX(${rightValueLabelTranslate}px)`;
        rightValueLabelTransformed.style.transform = `translateX(${rightValueLabelTranslate}px)`;
      }
      else if (leftValueLabelStaticHostOffset > 0 || rightValueLabelStaticHostOffset > 0) {
        // labels overlap host boundary on the left side
        leftValueLabel.style.transform = `translateX(${leftValueLabelStaticHostOffset + labelOffset}px)`;
        rightValueLabel.style.transform = `translateX(${labelTransformedOverlap + rightValueLabelStaticHostOffset}px)`;
        rightValueLabelTransformed.style.transform = `translateX(${labelTransformedOverlap + rightValueLabelStaticHostOffset}px)`;
      }
      else if (leftValueLabelStaticHostOffset < 0 || rightValueLabelStaticHostOffset < 0) {
        // labels overlap host boundary on the right side
        let leftValueLabelTranslate = Math.abs(leftValueLabelStaticHostOffset) + labelTransformedOverlap - labelOffset;
        leftValueLabelTranslate =
          Math.sign(leftValueLabelTranslate) === -1
            ? Math.abs(leftValueLabelTranslate)
            : -leftValueLabelTranslate;
        leftValueLabel.style.transform = `translateX(${leftValueLabelTranslate}px)`;
        leftValueLabelTransformed.style.transform = `translateX(${leftValueLabelTranslate - labelOffset}px)`;
      }
    }
    else {
      hyphenLabel.classList.remove("hyphen", "hyphen--wrap");
      leftValueLabel.style.transform = `translateX(${leftValueLabelStaticHostOffset}px)`;
      leftValueLabelTransformed.style.transform = `translateX(${leftValueLabelStaticHostOffset}px)`;
      rightValueLabel.style.transform = `translateX(${rightValueLabelStaticHostOffset}px)`;
      rightValueLabelTransformed.style.transform = `translateX(${rightValueLabelStaticHostOffset}px)`;
    }
  }
  /**
   * Hides bounding tick labels that are obscured by either handle.
   */
  hideObscuredBoundingTickLabels() {
    const valueIsRange = isRange(this.value);
    if (!this.hasHistogram && !valueIsRange && !this.labelHandles && !this.precise) {
      return;
    }
    if (!this.hasHistogram && !valueIsRange && this.labelHandles && !this.precise) {
      return;
    }
    if (!this.hasHistogram && !valueIsRange && !this.labelHandles && this.precise) {
      return;
    }
    if (!this.hasHistogram && !valueIsRange && this.labelHandles && this.precise) {
      return;
    }
    if (!this.hasHistogram && valueIsRange && !this.precise) {
      return;
    }
    if (this.hasHistogram && !this.precise && !this.labelHandles) {
      return;
    }
    const minHandle = this.el.shadowRoot.querySelector(".thumb--minValue");
    const maxHandle = this.el.shadowRoot.querySelector(".thumb--value");
    const minTickLabel = this.el.shadowRoot.querySelector(".tick__label--min");
    const maxTickLabel = this.el.shadowRoot.querySelector(".tick__label--max");
    if (!minHandle && maxHandle && minTickLabel && maxTickLabel) {
      minTickLabel.style.opacity = this.isMinTickLabelObscured(minTickLabel, maxHandle) ? "0" : "1";
      maxTickLabel.style.opacity = this.isMaxTickLabelObscured(maxTickLabel, maxHandle) ? "0" : "1";
    }
    if (minHandle && maxHandle && minTickLabel && maxTickLabel) {
      minTickLabel.style.opacity =
        this.isMinTickLabelObscured(minTickLabel, minHandle) ||
          this.isMinTickLabelObscured(minTickLabel, maxHandle)
          ? "0"
          : "1";
      maxTickLabel.style.opacity =
        this.isMaxTickLabelObscured(maxTickLabel, minHandle) ||
          (this.isMaxTickLabelObscured(maxTickLabel, maxHandle) && this.hasHistogram)
          ? "0"
          : "1";
    }
  }
  /**
   * Returns an integer representing the number of pixels to offset on the left or right side based on desired position behavior.
   *
   * @param leftBounds
   * @param rightBounds
   * @internal
   */
  getHostOffset(leftBounds, rightBounds) {
    const hostBounds = this.el.getBoundingClientRect();
    const buffer = 7;
    if (leftBounds + buffer < hostBounds.left) {
      return hostBounds.left - leftBounds - buffer;
    }
    if (rightBounds - buffer > hostBounds.right) {
      return -(rightBounds - hostBounds.right) + buffer;
    }
    return 0;
  }
  /**
   * Returns an integer representing the number of pixels that the two given span elements are overlapping, taking into account
   * a space in between the two spans equal to the font-size set on them to account for the space needed to render a hyphen.
   *
   * @param leftLabel
   * @param rightLabel
   */
  getRangeLabelOverlap(leftLabel, rightLabel) {
    const leftLabelBounds = leftLabel.getBoundingClientRect();
    const rightLabelBounds = rightLabel.getBoundingClientRect();
    const leftLabelFontSize = this.getFontSizeForElement(leftLabel);
    const rangeLabelOverlap = leftLabelBounds.right + leftLabelFontSize - rightLabelBounds.left;
    return Math.max(rangeLabelOverlap, 0);
  }
  /**
   * Returns a boolean value representing if the minLabel span element is obscured (being overlapped) by the given handle div element.
   *
   * @param minLabel
   * @param handle
   */
  isMinTickLabelObscured(minLabel, handle) {
    const minLabelBounds = minLabel.getBoundingClientRect();
    const handleBounds = handle.getBoundingClientRect();
    return intersects(minLabelBounds, handleBounds);
  }
  /**
   * Returns a boolean value representing if the maxLabel span element is obscured (being overlapped) by the given handle div element.
   *
   * @param maxLabel
   * @param handle
   */
  isMaxTickLabelObscured(maxLabel, handle) {
    const maxLabelBounds = maxLabel.getBoundingClientRect();
    const handleBounds = handle.getBoundingClientRect();
    return intersects(maxLabelBounds, handleBounds);
  }
  get el() { return getElement(this); }
  static get watchers() { return {
    "histogram": ["histogramWatcher"],
    "value": ["valueHandler"],
    "minValue": ["minMaxValueHandler"],
    "maxValue": ["minMaxValueHandler"]
  }; }
};
Slider.style = sliderCss;

export { Graph as calcite_graph, Slider as calcite_slider };
