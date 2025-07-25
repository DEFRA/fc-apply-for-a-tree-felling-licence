/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { createObserver } from "./observers";
const allowedGlobalAttributes = ["lang"];
const elementToComponentAndObserverOptionsMap = new Map();
let mutationObserver;
function updateGlobalAttributes(component, attributeFilter) {
  const { el } = component;
  const updatedAttributes = {};
  attributeFilter
    .filter((attr) => !!allowedGlobalAttributes.includes(attr) && !!el.hasAttribute(attr))
    .forEach((attr) => {
    const value = el.getAttribute(attr);
    if (value !== null) {
      updatedAttributes[attr] = value;
    }
  });
  component.globalAttributes = updatedAttributes;
}
function processMutations(mutations) {
  mutations.forEach(({ target }) => {
    const [component, options] = elementToComponentAndObserverOptionsMap.get(target);
    updateGlobalAttributes(component, options.attributeFilter);
  });
}
/**
 * Helper to set up listening for changes to global attributes.
 *
 * render(): VNode {
 *   const lang = this.inheritedAttributes['lang'] ?? 'en';
 *   return <div>My lang is {lang}</div>;
 * }
 *
 * @param component
 * @param attributeFilter
 */
export function watchGlobalAttributes(component, attributeFilter) {
  const { el } = component;
  const observerOptions = { attributeFilter };
  elementToComponentAndObserverOptionsMap.set(el, [component, observerOptions]);
  updateGlobalAttributes(component, attributeFilter);
  if (!mutationObserver) {
    mutationObserver = createObserver("mutation", processMutations);
  }
  mutationObserver.observe(el, observerOptions);
}
/**
 * Helper remove listening for changes to inherited attributes.
 *
 * @param component
 */
export function unwatchGlobalAttributes(component) {
  elementToComponentAndObserverOptionsMap.delete(component.el);
  // we explicitly process queued mutations and disconnect and reconnect
  // the observer until MutationObserver gets an `unobserve` method
  // see https://github.com/whatwg/dom/issues/126
  processMutations(mutationObserver.takeRecords());
  mutationObserver.disconnect();
  for (const [element, [, observerOptions]] of elementToComponentAndObserverOptionsMap.entries()) {
    mutationObserver.observe(element, observerOptions);
  }
}
