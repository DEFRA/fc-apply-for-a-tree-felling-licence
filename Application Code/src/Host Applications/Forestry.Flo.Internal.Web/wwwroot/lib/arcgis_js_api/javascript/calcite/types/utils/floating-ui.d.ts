import { Placement, Strategy, VirtualElement } from "@floating-ui/dom";
/**
 * Exported for testing purposes only
 */
export declare const placementDataAttribute = "data-placement";
/**
 * Exported for testing purposes only
 */
export declare const repositionDebounceTimeout = 100;
export declare type ReferenceElement = VirtualElement | Element;
declare type UIType = "menu" | "tooltip" | "popover";
export declare type OverlayPositioning = Strategy;
/**
 * Placements that change based on element direction.
 *
 * These variation placements will automatically flip "left"/"right" depending on LTR/RTL direction.
 *
 * Floating-ui has no plans to offer this functionality out of the box at this time.
 *
 * see: https://github.com/floating-ui/floating-ui/issues/1563 and https://github.com/floating-ui/floating-ui/discussions/1549
 */
declare type VariationPlacement = "leading-start" | "leading" | "leading-end" | "trailing-end" | "trailing" | "trailing-start";
declare type AutoPlacement = "auto" | "auto-start" | "auto-end";
/**
 * Use "*-start" and "*-end" instead.
 *
 * There is no need for our "*-leading" and "*-trailing" values anymore since "*-start" and "*-end" are already flipped in RTL.
 *
 * @deprecated
 */
declare type DeprecatedPlacement = "leading-leading" | "leading-trailing" | "trailing-leading" | "trailing-trailing" | "top-leading" | "top-trailing" | "bottom-leading" | "bottom-trailing" | "right-leading" | "right-trailing" | "left-leading" | "left-trailing";
export declare type LogicalPlacement = AutoPlacement | Placement | VariationPlacement | DeprecatedPlacement;
export declare type EffectivePlacement = Placement;
export declare const placements: LogicalPlacement[];
export declare const effectivePlacements: EffectivePlacement[];
export declare const menuPlacements: MenuPlacement[];
export declare const menuEffectivePlacements: EffectivePlacement[];
export declare const flipPlacements: EffectivePlacement[];
/**
 * Use "*-start" and "*-end" instead.
 *
 * There is no need for our "*-leading" and "*-trailing" values anymore since "*-start" and "*-end" are already flipped in RTL.
 *
 * @deprecated
 */
declare type DeprecatedMenuPlacement = Extract<DeprecatedPlacement, "top-leading" | "top-trailing" | "bottom-leading" | "bottom-trailing">;
export declare type MenuPlacement = DeprecatedMenuPlacement | Extract<LogicalPlacement, "top-start" | "top" | "top-end" | "bottom-start" | "bottom" | "bottom-end">;
export declare const defaultMenuPlacement: MenuPlacement;
export interface FloatingUIComponent {
  /**
   * Whether the component is opened.
   */
  open: boolean;
  /**
   * Describes the type of positioning to use for the overlaid content. If your element is in a fixed container, use the 'fixed' value.
   */
  overlayPositioning: OverlayPositioning;
  /**
   * Determines where the component will be positioned relative to the referenceElement.
   *
   * Possible values: "auto", "auto-start", "auto-end", "top", "right", "bottom", "left", "top-start", "top-end", "right-start", "right-end", "bottom-start", "bottom-end", "left-start", "left-end", "leading-start", "leading", "leading-end", "trailing-end", "trailing",  or "trailing-start".
   *
   */
  placement: LogicalPlacement;
  /**
   * Updates the position of the component.
   *
   * @param delayed – (internal) when true, it will reposition the component after a delay. the default is false. This is useful for components that have multiple watched properties that schedule repositioning.
   */
  reposition(delayed?: boolean): Promise<void>;
}
export declare const FloatingCSS: {
  animation: string;
  animationActive: string;
};
export declare function filterComputedPlacements(placements: string[], el: HTMLElement): EffectivePlacement[];
export declare function getEffectivePlacement(floatingEl: HTMLElement, placement: LogicalPlacement): EffectivePlacement;
/**
 * Convenience function to manage `reposition` calls for FloatingUIComponents that use `positionFloatingUI.
 *
 * Note: this is not needed for components that use `calcite-popover`.
 *
 * @param component
 * @param options
 * @param options.referenceEl
 * @param options.floatingEl
 * @param options.overlayPositioning
 * @param options.placement
 * @param options.disableFlip
 * @param options.flipPlacements
 * @param options.offsetDistance
 * @param options.offsetSkidding
 * @param options.arrowEl
 * @param options.type
 * @param delayed
 */
export declare function reposition(component: FloatingUIComponent, options: Parameters<typeof positionFloatingUI>[0], delayed?: boolean): Promise<void>;
/**
 * Positions the floating element relative to the reference element.
 *
 * **Note:** exported for testing purposes only
 *
 * @param root0
 * @param root0.referenceEl
 * @param root0.floatingEl
 * @param root0.overlayPositioning
 * @param root0.placement
 * @param root0.disableFlip
 * @param root0.flipPlacements
 * @param root0.offsetDistance
 * @param root0.offsetSkidding
 * @param root0.arrowEl
 * @param root0.type
 * @param root0.includeArrow
 */
export declare function positionFloatingUI({ referenceEl, floatingEl, overlayPositioning, placement, disableFlip, flipPlacements, offsetDistance, offsetSkidding, includeArrow, arrowEl, type }: {
  referenceEl: ReferenceElement;
  floatingEl: HTMLElement;
  overlayPositioning: Strategy;
  placement: LogicalPlacement;
  disableFlip?: boolean;
  flipPlacements?: EffectivePlacement[];
  offsetDistance?: number;
  offsetSkidding?: number;
  arrowEl?: HTMLElement;
  includeArrow?: boolean;
  type: UIType;
}): Promise<void>;
/**
 * Exported for testing purposes only
 *
 * @internal
 */
export declare const cleanupMap: WeakMap<FloatingUIComponent, () => void>;
/**
 * Helper to set up floating element interactions on connectedCallback.
 *
 * @param component
 * @param referenceEl
 * @param floatingEl
 */
export declare function connectFloatingUI(component: FloatingUIComponent, referenceEl: ReferenceElement, floatingEl: HTMLElement): void;
/**
 * Helper to tear down floating element interactions on disconnectedCallback.
 *
 * @param component
 * @param referenceEl
 * @param floatingEl
 */
export declare function disconnectFloatingUI(component: FloatingUIComponent, referenceEl: ReferenceElement, floatingEl: HTMLElement): void;
/**
 * Default offset the position of the floating element away from the reference element.
 *
 * @default 6
 */
export declare const defaultOffsetDistance: number;
/**
 * This utils applies floating element styles to avoid affecting layout when closed.
 *
 * This should be called when the closing transition will start.
 *
 * @param floatingEl
 */
export declare function updateAfterClose(floatingEl: HTMLElement): void;
export {};
