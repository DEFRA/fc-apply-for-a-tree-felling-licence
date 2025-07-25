import { EventEmitter, VNode } from "../../stencil-public-runtime";
import { ColorStop, DataSeries } from "../graph/interfaces";
import { Scale } from "../interfaces";
import { LabelableComponent } from "../../utils/label";
import { FormComponent } from "../../utils/form";
import { InteractiveComponent } from "../../utils/interactive";
import { LocalizedComponent, NumberingSystem } from "../../utils/locale";
export declare class Slider implements LabelableComponent, FormComponent, InteractiveComponent, LocalizedComponent {
  el: HTMLCalciteSliderElement;
  /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
  disabled: boolean;
  /**
   * When `true`, number values are displayed with a group separator corresponding to the language and country format.
   */
  groupSeparator: boolean;
  /** When `true`, indicates a histogram is present. */
  hasHistogram: boolean;
  /**
   * A list of the histogram's x,y coordinates within the component's `min` and `max`. Displays above the component's track.
   *
   * @see [DataSeries](https://github.com/Esri/calcite-components/blob/master/src/components/graph/interfaces.ts#L5)
   */
  histogram?: DataSeries;
  histogramWatcher(newHistogram: DataSeries): void;
  /**
   * A set of single color stops for a histogram, sorted by offset ascending.
   */
  histogramStops: ColorStop[];
  /** When `true`, displays label handles with their numeric value. */
  labelHandles: boolean;
  /** When `true` and `ticks` is specified, displays label tick marks with their numeric value. */
  labelTicks: boolean;
  /** The component's maximum selectable value. */
  max: number;
  /** For multiple selections, the accessible name for the second handle, such as `"Temperature, upper bound"`. */
  maxLabel?: string;
  /** For multiple selections, the component's upper value. */
  maxValue?: number;
  /** The component's minimum selectable value. */
  min: number;
  /** Accessible name for first (or only) handle, such as `"Temperature, lower bound"`. */
  minLabel: string;
  /** For multiple selections, the component's lower value. */
  minValue?: number;
  /**
   * When `true`, the slider will display values from high to low.
   *
   * Note that this value will be ignored if the slider has an associated histogram.
   */
  mirrored: boolean;
  /** Specifies the name of the component on form submission. */
  name: string;
  /**
   * Specifies the Unicode numeral system used by the component for localization.
   */
  numberingSystem?: NumberingSystem;
  /** Specifies the interval to move with the page up, or page down keys. */
  pageStep?: number;
  /** When `true`, sets a finer point for handles. */
  precise: boolean;
  /**
   * When `true`, the component must have a value in order for the form to submit.
   */
  required: boolean;
  /** When `true`, enables snap selection in coordination with `step` via a mouse. */
  snap: boolean;
  /** Specifies the interval to move with the up, or down keys. */
  step?: number;
  /** Displays tick marks on the number line at a specified interval. */
  ticks?: number;
  /** The component's value. */
  value: null | number | number[];
  valueHandler(): void;
  minMaxValueHandler(): void;
  /**
   *  Specifies the size of the component.
   */
  scale: Scale;
  connectedCallback(): void;
  disconnectedCallback(): void;
  componentWillLoad(): void;
  componentDidRender(): void;
  render(): VNode;
  private renderGraph;
  private renderTickLabel;
  keyDownHandler(event: KeyboardEvent): void;
  pointerDownHandler(event: PointerEvent): void;
  handleTouchStart(event: TouchEvent): void;
  /**
   * Fires on all updates to the component.
   *
   * **Note:** Will be fired frequently during drag. If you are performing any
   * expensive operations consider using a debounce or throttle to avoid
   * locking up the main thread.
   */
  calciteSliderInput: EventEmitter<void>;
  /**
   * Fires when the thumb is released on the component.
   *
   * **Note:** If you need to constantly listen to the drag event,
   * use `calciteSliderInput` instead.
   */
  calciteSliderChange: EventEmitter<void>;
  /**
   * Fires on all updates to the component.
   *
   * **Note:** Will be fired frequently during drag. If you are performing any
   * expensive operations consider using a debounce or throttle to avoid
   * locking up the main thread.
   *
   * @deprecated use `calciteSliderInput` instead.
   */
  calciteSliderUpdate: EventEmitter<void>;
  /** Sets focus on the component. */
  setFocus(): Promise<void>;
  labelEl: HTMLCalciteLabelElement;
  formEl: HTMLFormElement;
  defaultValue: Slider["value"];
  private activeProp;
  private guid;
  private dragProp;
  private lastDragProp;
  private lastDragPropValue;
  private minHandle;
  private maxHandle;
  private trackEl;
  effectiveLocale: string;
  private minMaxValueRange;
  private minValueDragRange;
  private maxValueDragRange;
  private tickValues;
  setValueFromMinMax(): void;
  setMinMaxFromValue(): void;
  onLabelClick(): void;
  private shouldMirror;
  private shouldUseMinValue;
  private generateTickValues;
  private pointerDownDragStart;
  private dragStart;
  private focusActiveHandle;
  private dragUpdate;
  private emitInput;
  private emitChange;
  private pointerUpDragEnd;
  private dragEnd;
  private removeDragListeners;
  /**
   * Set prop value(s) if changed at the component level
   *
   * @param {object} values - a set of key/value pairs delineating what properties in the component to update
   */
  private setValue;
  /**
   * Set the reference of the track Element
   *
   * @internal
   * @param node
   */
  private storeTrackRef;
  /**
   * If number is outside range, constrain to min or max
   *
   * @param value
   * @param prop
   * @internal
   */
  private clamp;
  /**
   * Translate a pixel position to value along the range
   *
   * @param x
   * @internal
   */
  private translate;
  /**
   * Get closest allowed value along stepped values
   *
   * @param num
   * @internal
   */
  private getClosestStep;
  private getClosestHandle;
  private getDistanceX;
  private getFontSizeForElement;
  /**
   * Get position of value along range as fractional value
   *
   * @param num
   * @return {number} number in the unit interval [0,1]
   * @internal
   */
  private getUnitInterval;
  private adjustHostObscuredHandleLabel;
  private hyphenateCollidingRangeHandleLabels;
  /**
   * Hides bounding tick labels that are obscured by either handle.
   */
  private hideObscuredBoundingTickLabels;
  /**
   * Returns an integer representing the number of pixels to offset on the left or right side based on desired position behavior.
   *
   * @param leftBounds
   * @param rightBounds
   * @internal
   */
  private getHostOffset;
  /**
   * Returns an integer representing the number of pixels that the two given span elements are overlapping, taking into account
   * a space in between the two spans equal to the font-size set on them to account for the space needed to render a hyphen.
   *
   * @param leftLabel
   * @param rightLabel
   */
  private getRangeLabelOverlap;
  /**
   * Returns a boolean value representing if the minLabel span element is obscured (being overlapped) by the given handle div element.
   *
   * @param minLabel
   * @param handle
   */
  private isMinTickLabelObscured;
  /**
   * Returns a boolean value representing if the maxLabel span element is obscured (being overlapped) by the given handle div element.
   *
   * @param maxLabel
   * @param handle
   */
  private isMaxTickLabelObscured;
  /**
   * Returns a string representing the localized label value based if the groupSeparator prop is parsed.
   *
   * @param value
   */
  private determineGroupSeparator;
}
