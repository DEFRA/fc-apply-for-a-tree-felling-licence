/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { rectToClientRect } from "@floating-ui/core";
/**
 * This module provides utils to fix positioning across shadow DOM in non-Chromium browsers
 *
 * It is based on floating-ui's distributable
 */
/**
 * 👇 the following are needed to fix shadow DOM positioning 👇️
 *
 * @param element
 */
function getTrueOffsetParent(element) {
  if (!isHTMLElement(element) || getComputedStyle(element).position === "fixed") {
    return null;
  }
  return composedOffsetParent(element);
}
/**
 * Polyfills the old offsetParent behavior from before the spec was changed:
 * https://github.com/w3c/csswg-drafts/issues/159
 *
 * @param element
 */
function composedOffsetParent(element) {
  let { offsetParent } = element;
  let ancestor = element;
  let foundInsideSlot = false;
  while (ancestor && ancestor !== offsetParent) {
    const { assignedSlot } = ancestor;
    if (assignedSlot) {
      let newOffsetParent = assignedSlot.offsetParent;
      if (getComputedStyle(assignedSlot).display === "contents") {
        const hadStyleAttribute = assignedSlot.hasAttribute("style");
        const oldDisplay = assignedSlot.style.display;
        assignedSlot.style.display = getComputedStyle(ancestor).display;
        newOffsetParent = assignedSlot.offsetParent;
        assignedSlot.style.display = oldDisplay;
        if (!hadStyleAttribute) {
          assignedSlot.removeAttribute("style");
        }
      }
      ancestor = assignedSlot;
      if (offsetParent !== newOffsetParent) {
        offsetParent = newOffsetParent;
        foundInsideSlot = true;
      }
    }
    else if (isShadowRoot(ancestor) && ancestor.host && foundInsideSlot) {
      break;
    }
    ancestor = (isShadowRoot(ancestor) && ancestor.host) || ancestor.parentNode;
  }
  return offsetParent;
}
function getElementRects(_ref) {
  const { reference, floating, strategy } = _ref;
  return {
    reference: getRectRelativeToOffsetParent(reference, getOffsetParent(floating), strategy),
    floating: { ...getDimensions(floating), x: 0, y: 0 }
  };
}
export { getClippingRect, getElementRects, getOffsetParent };
/**
 * ☝️ the following are needed to fix shadow DOM positioning ☝️
 */
/**
 * 👇 the following are taken directly from floating-ui's ESM distributable to support the exports above 👇️
 *
 * **Notes**:
 * unused functions are removed
 * ESLint is disabled
 * TS-warnings are suppressed
 */
/* eslint-disable */
function isWindow(value) {
  return value && value.document && value.location && value.alert && value.setInterval;
}
function getWindow(node) {
  if (node == null) {
    return window;
  }
  if (!isWindow(node)) {
    const ownerDocument = node.ownerDocument;
    return ownerDocument ? ownerDocument.defaultView || window : window;
  }
  return node;
}
function getComputedStyle(element) {
  return getWindow(element).getComputedStyle(element);
}
function getNodeName(node) {
  return isWindow(node) ? "" : node ? (node.nodeName || "").toLowerCase() : "";
}
function getUAString() {
  // @ts-ignore
  const uaData = navigator.userAgentData;
  if (uaData != null && uaData.brands) {
    return uaData.brands.map((item) => item.brand + "/" + item.version).join(" ");
  }
  return navigator.userAgent;
}
function isHTMLElement(value) {
  return value instanceof getWindow(value).HTMLElement;
}
function isElement(value) {
  return value instanceof getWindow(value).Element;
}
function isNode(value) {
  return value instanceof getWindow(value).Node;
}
function isShadowRoot(node) {
  // Browsers without `ShadowRoot` support
  if (typeof ShadowRoot === "undefined") {
    return false;
  }
  const OwnElement = getWindow(node).ShadowRoot;
  return node instanceof OwnElement || node instanceof ShadowRoot;
}
function isOverflowElement(element) {
  // Firefox wants us to check `-x` and `-y` variations as well
  const { overflow, overflowX, overflowY, display } = getComputedStyle(element);
  return (/auto|scroll|overlay|hidden/.test(overflow + overflowY + overflowX) && !["inline", "contents"].includes(display));
}
function isTableElement(element) {
  return ["table", "td", "th"].includes(getNodeName(element));
}
function isContainingBlock(element) {
  // TODO: Try and use feature detection here instead
  const isFirefox = /firefox/i.test(getUAString());
  const css = getComputedStyle(element); // This is non-exhaustive but covers the most common CSS properties that
  // create a containing block.
  // https://developer.mozilla.org/en-US/docs/Web/CSS/Containing_block#identifying_the_containing_block
  return (css.transform !== "none" ||
    css.perspective !== "none" ||
    (isFirefox && css.willChange === "filter") ||
    (isFirefox && (css.filter ? css.filter !== "none" : false)) ||
    ["transform", "perspective"].some((value) => css.willChange.includes(value)) ||
    ["paint", "layout", "strict", "content"].some(
    // TS 4.1 compat
    (value) => {
      const contain = css.contain;
      return contain != null ? contain.includes(value) : false;
    }));
}
function isLayoutViewport() {
  // Not Safari
  return !/^((?!chrome|android).)*safari/i.test(getUAString()); // Feature detection for this fails in various ways
  // • Always-visible scrollbar or not
  // • Width of <html>, etc.
  // const vV = win.visualViewport;
  // return vV ? Math.abs(win.innerWidth / vV.scale - vV.width) < 0.5 : true;
}
function isLastTraversableNode(node) {
  return ["html", "body", "#document"].includes(getNodeName(node));
}
const min = Math.min;
const max = Math.max;
const round = Math.round;
function getBoundingClientRect(element, includeScale, isFixedStrategy) {
  var _win$visualViewport$o, _win$visualViewport, _win$visualViewport$o2, _win$visualViewport2;
  if (includeScale === void 0) {
    includeScale = false;
  }
  if (isFixedStrategy === void 0) {
    isFixedStrategy = false;
  }
  const clientRect = element.getBoundingClientRect();
  let scaleX = 1;
  let scaleY = 1;
  if (includeScale && isHTMLElement(element)) {
    scaleX = element.offsetWidth > 0 ? round(clientRect.width) / element.offsetWidth || 1 : 1;
    scaleY = element.offsetHeight > 0 ? round(clientRect.height) / element.offsetHeight || 1 : 1;
  }
  const win = isElement(element) ? getWindow(element) : window;
  const addVisualOffsets = !isLayoutViewport() && isFixedStrategy;
  const x = (clientRect.left +
    (addVisualOffsets
      ? (_win$visualViewport$o =
        (_win$visualViewport = win.visualViewport) == null ? void 0 : _win$visualViewport.offsetLeft) != null
        ? _win$visualViewport$o
        : 0
      : 0)) /
    scaleX;
  const y = (clientRect.top +
    (addVisualOffsets
      ? (_win$visualViewport$o2 =
        (_win$visualViewport2 = win.visualViewport) == null ? void 0 : _win$visualViewport2.offsetTop) != null
        ? _win$visualViewport$o2
        : 0
      : 0)) /
    scaleY;
  const width = clientRect.width / scaleX;
  const height = clientRect.height / scaleY;
  return {
    width,
    height,
    top: y,
    right: x + width,
    bottom: y + height,
    left: x,
    x,
    y
  };
}
function getDocumentElement(node) {
  return ((isNode(node) ? node.ownerDocument : node.document) || window.document).documentElement;
}
function getNodeScroll(element) {
  if (isElement(element)) {
    return {
      scrollLeft: element.scrollLeft,
      scrollTop: element.scrollTop
    };
  }
  return {
    scrollLeft: element.pageXOffset,
    scrollTop: element.pageYOffset
  };
}
function getWindowScrollBarX(element) {
  // If <html> has a CSS width greater than the viewport, then this will be
  // incorrect for RTL.
  // @ts-ignore
  return getBoundingClientRect(getDocumentElement(element)).left + getNodeScroll(element).scrollLeft;
}
function isScaled(element) {
  // @ts-ignore
  const rect = getBoundingClientRect(element);
  return round(rect.width) !== element.offsetWidth || round(rect.height) !== element.offsetHeight;
}
function getRectRelativeToOffsetParent(element, offsetParent, strategy) {
  const isOffsetParentAnElement = isHTMLElement(offsetParent);
  const documentElement = getDocumentElement(offsetParent);
  const rect = getBoundingClientRect(element, // @ts-ignore - checked above (TS 4.1 compat)
  isOffsetParentAnElement && isScaled(offsetParent), strategy === "fixed");
  let scroll = {
    scrollLeft: 0,
    scrollTop: 0
  };
  const offsets = {
    x: 0,
    y: 0
  };
  if (isOffsetParentAnElement || (!isOffsetParentAnElement && strategy !== "fixed")) {
    if (getNodeName(offsetParent) !== "body" || isOverflowElement(documentElement)) {
      scroll = getNodeScroll(offsetParent);
    }
    if (isHTMLElement(offsetParent)) {
      // @ts-ignore
      const offsetRect = getBoundingClientRect(offsetParent, true);
      offsets.x = offsetRect.x + offsetParent.clientLeft;
      offsets.y = offsetRect.y + offsetParent.clientTop;
    }
    else if (documentElement) {
      offsets.x = getWindowScrollBarX(documentElement);
    }
  }
  return {
    x: rect.left + scroll.scrollLeft - offsets.x,
    y: rect.top + scroll.scrollTop - offsets.y,
    width: rect.width,
    height: rect.height
  };
}
function getParentNode(node) {
  if (getNodeName(node) === "html") {
    return node;
  }
  return (
  // this is a quicker (but less type safe) way to save quite some bytes from the bundle
  // @ts-ignore
  node.assignedSlot || // step into the shadow DOM of the parent of a slotted node
    node.parentNode || // DOM Element detected
    (isShadowRoot(node) ? node.host : null) || // ShadowRoot detected
    getDocumentElement(node) // fallback
  );
}
function getContainingBlock(element) {
  let currentNode = getParentNode(element);
  if (isShadowRoot(currentNode)) {
    currentNode = currentNode.host;
  }
  while (isHTMLElement(currentNode) && !isLastTraversableNode(currentNode)) {
    if (isContainingBlock(currentNode)) {
      return currentNode;
    }
    else {
      const parent = currentNode.parentNode;
      currentNode = isShadowRoot(parent) ? parent.host : parent;
    }
  }
  return null;
} // Gets the closest ancestor positioned element. Handles some edge cases,
// such as table ancestors and cross browser bugs.
function getOffsetParent(element) {
  const window = getWindow(element);
  let offsetParent = getTrueOffsetParent(element);
  while (offsetParent && isTableElement(offsetParent) && getComputedStyle(offsetParent).position === "static") {
    offsetParent = getTrueOffsetParent(offsetParent);
  }
  if (offsetParent &&
    (getNodeName(offsetParent) === "html" ||
      (getNodeName(offsetParent) === "body" &&
        getComputedStyle(offsetParent).position === "static" &&
        !isContainingBlock(offsetParent)))) {
    return window;
  }
  return offsetParent || getContainingBlock(element) || window;
}
function getDimensions(element) {
  if (isHTMLElement(element)) {
    return {
      width: element.offsetWidth,
      height: element.offsetHeight
    };
  }
  // @ts-ignore
  const rect = getBoundingClientRect(element);
  return {
    width: rect.width,
    height: rect.height
  };
}
function getViewportRect(element, strategy) {
  const win = getWindow(element);
  const html = getDocumentElement(element);
  const visualViewport = win.visualViewport;
  let width = html.clientWidth;
  let height = html.clientHeight;
  let x = 0;
  let y = 0;
  if (visualViewport) {
    width = visualViewport.width;
    height = visualViewport.height;
    const layoutViewport = isLayoutViewport();
    if (layoutViewport || (!layoutViewport && strategy === "fixed")) {
      x = visualViewport.offsetLeft;
      y = visualViewport.offsetTop;
    }
  }
  return {
    width,
    height,
    x,
    y
  };
}
// of the `<html>` and `<body>` rect bounds if horizontally scrollable
function getDocumentRect(element) {
  var _element$ownerDocumen;
  const html = getDocumentElement(element);
  const scroll = getNodeScroll(element);
  const body = (_element$ownerDocumen = element.ownerDocument) == null ? void 0 : _element$ownerDocumen.body;
  const width = max(html.scrollWidth, html.clientWidth, body ? body.scrollWidth : 0, body ? body.clientWidth : 0);
  const height = max(html.scrollHeight, html.clientHeight, body ? body.scrollHeight : 0, body ? body.clientHeight : 0);
  let x = -scroll.scrollLeft + getWindowScrollBarX(element);
  const y = -scroll.scrollTop;
  if (getComputedStyle(body || html).direction === "rtl") {
    x += max(html.clientWidth, body ? body.clientWidth : 0) - width;
  }
  return {
    width,
    height,
    x,
    y
  };
}
function getNearestOverflowAncestor(node) {
  const parentNode = getParentNode(node);
  if (isLastTraversableNode(parentNode)) {
    // @ts-ignore assume body is always available
    return node.ownerDocument.body;
  }
  if (isHTMLElement(parentNode) && isOverflowElement(parentNode)) {
    return parentNode;
  }
  return getNearestOverflowAncestor(parentNode);
}
function getOverflowAncestors(node, list) {
  var _node$ownerDocument;
  if (list === void 0) {
    list = [];
  }
  const scrollableAncestor = getNearestOverflowAncestor(node);
  const isBody = scrollableAncestor === ((_node$ownerDocument = node.ownerDocument) == null ? void 0 : _node$ownerDocument.body);
  const win = getWindow(scrollableAncestor);
  const target = isBody
    ? [win].concat(win.visualViewport || [], isOverflowElement(scrollableAncestor) ? scrollableAncestor : [])
    : scrollableAncestor;
  const updatedList = list.concat(target);
  return isBody
    ? updatedList // @ts-ignore: isBody tells us target will be an HTMLElement here
    : updatedList.concat(getOverflowAncestors(target));
}
function contains(parent, child) {
  const rootNode = child.getRootNode == null ? void 0 : child.getRootNode(); // First, attempt with faster native method
  if (parent.contains(child)) {
    return true;
  } // then fallback to custom implementation with Shadow DOM support
  else if (rootNode && isShadowRoot(rootNode)) {
    let next = child;
    do {
      // use `===` replace node.isSameNode()
      if (next && parent === next) {
        return true;
      } // @ts-ignore: need a better way to handle this...
      next = next.parentNode || next.host;
    } while (next);
  }
  return false;
}
function getNearestParentCapableOfEscapingClipping(element, clippingAncestors) {
  let currentNode = element;
  while (currentNode && !isLastTraversableNode(currentNode) && !clippingAncestors.includes(currentNode)) {
    if (isElement(currentNode) && ["absolute", "fixed"].includes(getComputedStyle(currentNode).position)) {
      break;
    }
    const parentNode = getParentNode(currentNode);
    currentNode = isShadowRoot(parentNode) ? parentNode.host : parentNode;
  }
  return currentNode;
}
function getInnerBoundingClientRect(element, strategy) {
  const clientRect = getBoundingClientRect(element, false, strategy === "fixed");
  const top = clientRect.top + element.clientTop;
  const left = clientRect.left + element.clientLeft;
  return {
    top,
    left,
    x: left,
    y: top,
    right: left + element.clientWidth,
    bottom: top + element.clientHeight,
    width: element.clientWidth,
    height: element.clientHeight
  };
}
function getClientRectFromClippingAncestor(element, clippingParent, strategy) {
  if (clippingParent === "viewport") {
    return rectToClientRect(getViewportRect(element, strategy));
  }
  if (isElement(clippingParent)) {
    return getInnerBoundingClientRect(clippingParent, strategy);
  }
  return rectToClientRect(getDocumentRect(getDocumentElement(element)));
} // A "clipping ancestor" is an overflowable container with the characteristic of
// clipping (or hiding) overflowing elements with a position different from
// `initial`
function getClippingAncestors(element) {
  // @ts-ignore
  const clippingAncestors = getOverflowAncestors(element);
  const nearestEscapableParent = getNearestParentCapableOfEscapingClipping(element, clippingAncestors);
  let clipperElement = null;
  if (nearestEscapableParent && isHTMLElement(nearestEscapableParent)) {
    const offsetParent = getOffsetParent(nearestEscapableParent);
    if (isOverflowElement(nearestEscapableParent)) {
      clipperElement = nearestEscapableParent;
    }
    else if (isHTMLElement(offsetParent)) {
      clipperElement = offsetParent;
    }
  }
  if (!isElement(clipperElement)) {
    return [];
  } // @ts-ignore isElement check ensures we return Array<Element>
  return clippingAncestors.filter((clippingAncestors) => clipperElement &&
    isElement(clippingAncestors) &&
    contains(clippingAncestors, clipperElement) &&
    getNodeName(clippingAncestors) !== "body");
} // Gets the maximum area that the element is visible in due to any number of
// clipping ancestors
function getClippingRect(_ref) {
  let { element, boundary, rootBoundary, strategy } = _ref;
  const mainClippingAncestors = boundary === "clippingAncestors" ? getClippingAncestors(element) : [].concat(boundary);
  const clippingAncestors = [...mainClippingAncestors, rootBoundary];
  const firstClippingAncestor = clippingAncestors[0];
  const clippingRect = clippingAncestors.reduce((accRect, clippingAncestor) => {
    const rect = getClientRectFromClippingAncestor(element, clippingAncestor, strategy);
    accRect.top = max(rect.top, accRect.top);
    accRect.right = min(rect.right, accRect.right);
    accRect.bottom = min(rect.bottom, accRect.bottom);
    accRect.left = max(rect.left, accRect.left);
    return accRect;
  }, getClientRectFromClippingAncestor(element, firstClippingAncestor, strategy));
  return {
    width: clippingRect.right - clippingRect.left,
    height: clippingRect.bottom - clippingRect.top,
    x: clippingRect.left,
    y: clippingRect.top
  };
}
