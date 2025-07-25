/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { arrow, autoPlacement, autoUpdate, computePosition, flip, hide, offset, platform, shift } from "@floating-ui/dom";
import { closestElementCrossShadowBoundary, getElementDir } from "./dom";
import { debounce } from "lodash-es";
import { Build } from "@stencil/core";
import { config } from "./config";
const floatingUIBrowserCheck = patchFloatingUiForNonChromiumBrowsers();
async function patchFloatingUiForNonChromiumBrowsers() {
  function getUAString() {
    const uaData = navigator.userAgentData;
    if (uaData === null || uaData === void 0 ? void 0 : uaData.brands) {
      return uaData.brands.map((item) => `${item.brand}/${item.version}`).join(" ");
    }
    return navigator.userAgent;
  }
  if (Build.isBrowser &&
    config.floatingUINonChromiumPositioningFix &&
    // ⚠️ browser-sniffing is not a best practice and should be avoided ⚠️
    /firefox|safari/i.test(getUAString())) {
    const { getClippingRect, getElementRects, getOffsetParent } = await import("./floating-ui/nonChromiumPlatformUtils");
    platform.getClippingRect = getClippingRect;
    platform.getOffsetParent = getOffsetParent;
    platform.getElementRects = getElementRects;
  }
}
/**
 * Exported for testing purposes only
 */
export const placementDataAttribute = "data-placement";
/**
 * Exported for testing purposes only
 */
export const repositionDebounceTimeout = 100;
export const placements = [
  "auto",
  "auto-start",
  "auto-end",
  "top",
  "top-start",
  "top-end",
  "bottom",
  "bottom-start",
  "bottom-end",
  "right",
  "right-start",
  "right-end",
  "left",
  "left-start",
  "left-end",
  "leading-start",
  "leading",
  "leading-end",
  "trailing-end",
  "trailing",
  "trailing-start"
];
export const effectivePlacements = [
  "top",
  "bottom",
  "right",
  "left",
  "top-start",
  "top-end",
  "bottom-start",
  "bottom-end",
  "right-start",
  "right-end",
  "left-start",
  "left-end"
];
export const menuPlacements = ["top-start", "top", "top-end", "bottom-start", "bottom", "bottom-end"];
export const menuEffectivePlacements = [
  "top-start",
  "top",
  "top-end",
  "bottom-start",
  "bottom",
  "bottom-end"
];
export const flipPlacements = [
  "top",
  "bottom",
  "right",
  "left",
  "top-start",
  "top-end",
  "bottom-start",
  "bottom-end",
  "right-start",
  "right-end",
  "left-start",
  "left-end"
];
export const defaultMenuPlacement = "bottom-start";
export const FloatingCSS = {
  animation: "calcite-floating-ui-anim",
  animationActive: "calcite-floating-ui-anim--active"
};
function getMiddleware({ placement, disableFlip, flipPlacements, offsetDistance, offsetSkidding, arrowEl, type }) {
  const defaultMiddleware = [shift(), hide()];
  if (type === "menu") {
    return [
      ...defaultMiddleware,
      flip({
        fallbackPlacements: flipPlacements || ["top-start", "top", "top-end", "bottom-start", "bottom", "bottom-end"]
      })
    ];
  }
  if (type === "popover" || type === "tooltip") {
    const middleware = [
      ...defaultMiddleware,
      offset({
        mainAxis: typeof offsetDistance === "number" ? offsetDistance : 0,
        crossAxis: typeof offsetSkidding === "number" ? offsetSkidding : 0
      })
    ];
    if (placement === "auto" || placement === "auto-start" || placement === "auto-end") {
      middleware.push(autoPlacement({ alignment: placement === "auto-start" ? "start" : placement === "auto-end" ? "end" : null }));
    }
    else if (!disableFlip) {
      middleware.push(flip(flipPlacements ? { fallbackPlacements: flipPlacements } : {}));
    }
    if (arrowEl) {
      middleware.push(arrow({
        element: arrowEl
      }));
    }
    return middleware;
  }
  return [];
}
export function filterComputedPlacements(placements, el) {
  const filteredPlacements = placements.filter((placement) => effectivePlacements.includes(placement));
  if (filteredPlacements.length !== placements.length) {
    console.warn(`${el.tagName}: Invalid value found in: flipPlacements. Try any of these: ${effectivePlacements
      .map((placement) => `"${placement}"`)
      .join(", ")
      .trim()}`, { el });
  }
  return filteredPlacements;
}
/*
In floating-ui, "*-start" and "*-end" are already flipped in RTL.
There is no need for our "*-leading" and "*-trailing" values anymore.
https://github.com/floating-ui/floating-ui/issues/1530
https://github.com/floating-ui/floating-ui/issues/1563
*/
export function getEffectivePlacement(floatingEl, placement) {
  const placements = ["left", "right"];
  if (getElementDir(floatingEl) === "rtl") {
    placements.reverse();
  }
  return placement
    .replace(/-leading/gi, "-start")
    .replace(/-trailing/gi, "-end")
    .replace(/leading/gi, placements[0])
    .replace(/trailing/gi, placements[1]);
}
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
export async function reposition(component, options, delayed = false) {
  if (!component.open) {
    return;
  }
  return delayed ? debouncedReposition(options) : positionFloatingUI(options);
}
const debouncedReposition = debounce(positionFloatingUI, repositionDebounceTimeout, {
  leading: true,
  maxWait: repositionDebounceTimeout
});
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
export async function positionFloatingUI({ referenceEl, floatingEl, overlayPositioning = "absolute", placement, disableFlip, flipPlacements, offsetDistance, offsetSkidding, includeArrow = false, arrowEl, type }) {
  var _a;
  if (!referenceEl || !floatingEl || (includeArrow && !arrowEl)) {
    return null;
  }
  await floatingUIBrowserCheck;
  const { x, y, placement: effectivePlacement, strategy: position, middlewareData } = await computePosition(referenceEl, floatingEl, {
    strategy: overlayPositioning,
    placement: placement === "auto" || placement === "auto-start" || placement === "auto-end"
      ? undefined
      : getEffectivePlacement(floatingEl, placement),
    middleware: getMiddleware({
      placement,
      disableFlip,
      flipPlacements,
      offsetDistance,
      offsetSkidding,
      arrowEl,
      type
    })
  });
  if (middlewareData === null || middlewareData === void 0 ? void 0 : middlewareData.arrow) {
    const { x: arrowX, y: arrowY } = middlewareData.arrow;
    Object.assign(arrowEl.style, {
      left: arrowX != null ? `${arrowX}px` : "",
      top: arrowY != null ? `${arrowY}px` : ""
    });
  }
  const referenceHidden = (_a = middlewareData === null || middlewareData === void 0 ? void 0 : middlewareData.hide) === null || _a === void 0 ? void 0 : _a.referenceHidden;
  const visibility = referenceHidden ? "hidden" : null;
  const pointerEvents = visibility ? "none" : null;
  floatingEl.setAttribute(placementDataAttribute, effectivePlacement);
  const transform = `translate(${Math.round(x)}px,${Math.round(y)}px)`;
  Object.assign(floatingEl.style, {
    visibility,
    pointerEvents,
    position,
    top: "0",
    left: "0",
    transform
  });
}
/**
 * Exported for testing purposes only
 *
 * @internal
 */
export const cleanupMap = new WeakMap();
/**
 * Helper to set up floating element interactions on connectedCallback.
 *
 * @param component
 * @param referenceEl
 * @param floatingEl
 */
export function connectFloatingUI(component, referenceEl, floatingEl) {
  if (!floatingEl || !referenceEl) {
    return;
  }
  disconnectFloatingUI(component, referenceEl, floatingEl);
  const position = component.overlayPositioning;
  // ensure position matches for initial positioning
  Object.assign(floatingEl.style, {
    visibility: "hidden",
    pointerEvents: "none",
    position
  });
  if (position === "absolute") {
    resetPosition(floatingEl);
  }
  const runAutoUpdate = Build.isBrowser
    ? autoUpdate
    : (_refEl, _floatingEl, updateCallback) => {
      updateCallback();
      return () => {
        /* noop */
      };
    };
  cleanupMap.set(component, runAutoUpdate(referenceEl, floatingEl, () => component.reposition()));
}
/**
 * Helper to tear down floating element interactions on disconnectedCallback.
 *
 * @param component
 * @param referenceEl
 * @param floatingEl
 */
export function disconnectFloatingUI(component, referenceEl, floatingEl) {
  if (!floatingEl || !referenceEl) {
    return;
  }
  getTransitionTarget(floatingEl).removeEventListener("transitionend", handleTransitionElTransitionEnd);
  const cleanup = cleanupMap.get(component);
  if (cleanup) {
    cleanup();
  }
  cleanupMap.delete(component);
}
const visiblePointerSize = 4;
/**
 * Default offset the position of the floating element away from the reference element.
 *
 * @default 6
 */
export const defaultOffsetDistance = Math.ceil(Math.hypot(visiblePointerSize, visiblePointerSize));
/**
 * This utils applies floating element styles to avoid affecting layout when closed.
 *
 * This should be called when the closing transition will start.
 *
 * @param floatingEl
 */
export function updateAfterClose(floatingEl) {
  if (!floatingEl || floatingEl.style.position !== "absolute") {
    return;
  }
  getTransitionTarget(floatingEl).addEventListener("transitionend", handleTransitionElTransitionEnd);
}
function getTransitionTarget(floatingEl) {
  // assumes floatingEl w/ shadowRoot is a FloatingUIComponent
  return floatingEl.shadowRoot || floatingEl;
}
function handleTransitionElTransitionEnd(event) {
  const floatingTransitionEl = event.target;
  if (
  // using any prop from floating-ui transition
  event.propertyName === "opacity" &&
    floatingTransitionEl.classList.contains(FloatingCSS.animation)) {
    const floatingEl = getFloatingElFromTransitionTarget(floatingTransitionEl);
    resetPosition(floatingEl);
    getTransitionTarget(floatingEl).removeEventListener("transitionend", handleTransitionElTransitionEnd);
  }
}
function resetPosition(floatingEl) {
  // resets position to better match https://floating-ui.com/docs/computePosition#initial-layout
  floatingEl.style.transform = "";
  floatingEl.style.top = "0";
  floatingEl.style.left = "0";
}
function getFloatingElFromTransitionTarget(floatingTransitionEl) {
  return closestElementCrossShadowBoundary(floatingTransitionEl, `[${placementDataAttribute}]`);
}
