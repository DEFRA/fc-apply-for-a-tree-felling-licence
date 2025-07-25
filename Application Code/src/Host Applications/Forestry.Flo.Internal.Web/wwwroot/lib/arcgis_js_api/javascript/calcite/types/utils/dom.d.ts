/**
 * This helper will guarantee an ID on the provided element.
 *
 * If it already has an ID, it will be preserved, otherwise a unique one will be generated and assigned.
 *
 * @param el
 * @returns {string} The element's ID.
 */
export declare function ensureId(el: Element): string;
export declare function nodeListToArray<T extends Element>(nodeList: HTMLCollectionOf<T> | NodeListOf<T> | T[]): T[];
export declare type Direction = "ltr" | "rtl";
export declare function getThemeName(el: HTMLElement): "light" | "dark";
export declare function getElementDir(el: HTMLElement): Direction;
export declare function getElementProp(el: Element, prop: string, fallbackValue: any): any;
export declare function getRootNode(el: Element): Document | ShadowRoot;
export declare function getHost(root: Document | ShadowRoot): Element | null;
/**
 * This helper queries an element's rootNode and any ancestor rootNodes.
 *
 * If both an 'id' and 'selector' are supplied, 'id' will take precedence over 'selector'.
 *
 * @param element
 * @param root0
 * @param root0.selector
 * @param root0.id
 * @returns {Element} The element.
 */
export declare function queryElementRoots<T extends Element = Element>(element: Element, { selector, id }: {
  selector?: string;
  id?: string;
}): T | null;
export declare function closestElementCrossShadowBoundary<T extends Element = Element>(element: Element, selector: string): T | null;
/**
 * This utility helps invoke a callback as it traverses a node and its ancestors until reaching the root document.
 *
 * Returning early or undefined in `onVisit` will continue traversing up the DOM tree. Otherwise, traversal will halt with the returned value as the result of the function
 *
 * @param element
 * @param onVisit
 */
export declare function walkUpAncestry<T = any>(element: Element, onVisit: (node: Node) => T): T;
export declare function containsCrossShadowBoundary(element: Element, maybeDescendant: Element): boolean;
export interface FocusableElement extends HTMLElement {
  setFocus?: () => Promise<void>;
}
export declare function isCalciteFocusable(el: FocusableElement): boolean;
export declare function focusElement(el: FocusableElement): Promise<void>;
interface GetSlottedOptions {
  all?: boolean;
  direct?: boolean;
  matches?: string;
  selector?: string;
}
export declare function getSlotted<T extends Element = Element>(element: Element, slotName: string | string[] | (GetSlottedOptions & {
  all: true;
}), options: GetSlottedOptions & {
  all: true;
}): T[];
export declare function getSlotted<T extends Element = Element>(element: Element, slotName?: string | string[] | GetSlottedOptions, options?: GetSlottedOptions): T | null;
export declare function filterDirectChildren<T extends Element>(el: Element, selector: string): T[];
export declare function setRequestedIcon(iconObject: Record<string, string>, iconValue: string | boolean, matchedValue: string): string;
export declare function intersects(rect1: DOMRect, rect2: DOMRect): boolean;
/**
 * This helper makes sure that boolean aria attributes are properly converted to a string.
 *
 * It should only be used for aria attributes that require a string value of "true" or "false".
 *
 * @param value
 * @returns {string} The string conversion of a boolean value ("true" | "false").
 */
export declare function toAriaBoolean(value: boolean): string;
/**
 * This helper returns true if the pointer event fired from the primary button of the device.
 *
 * See https://www.w3.org/TR/pointerevents/#the-button-property.
 *
 * @param event
 * @returns {boolean}
 */
export declare function isPrimaryPointerButton(event: PointerEvent): boolean;
export {};
