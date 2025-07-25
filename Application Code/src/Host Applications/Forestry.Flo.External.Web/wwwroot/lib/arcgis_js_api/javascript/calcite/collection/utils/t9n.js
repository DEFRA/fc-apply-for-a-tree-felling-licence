/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { getAssetPath } from "@stencil/core";
import { getSupportedLocale } from "./locale";
export const componentLangToMessageBundleCache = {};
async function getMessageBundle(lang, component) {
  const key = `${component}_${lang}`;
  if (componentLangToMessageBundleCache[key]) {
    return componentLangToMessageBundleCache[key];
  }
  componentLangToMessageBundleCache[key] = fetch(getAssetPath(`./assets/${component}/t9n/messages_${lang}.json`))
    .then((resp) => {
    if (!resp.ok) {
      throwMessageFetchError();
    }
    return resp.json();
  })
    .catch(() => throwMessageFetchError());
  return componentLangToMessageBundleCache[key];
}
function throwMessageFetchError() {
  throw new Error("could not fetch component message bundle");
}
/**
 * This util helps preserve existing intlProp usage when they have not been replaced by overrides.
 *
 * @param component
 */
export function overridesFromIntlProps(component) {
  const { el } = component;
  const overrides = {};
  Object.keys(el.constructor.prototype)
    .filter((prop) => prop.startsWith("intl"))
    .forEach((prop) => {
    const assignedValue = el[prop];
    if (assignedValue) {
      let mappedProp = prop.replace("intl", "");
      mappedProp = `${mappedProp[0].toLowerCase()}${mappedProp.slice(1)}`;
      overrides[mappedProp] = assignedValue;
    }
  });
  return overrides;
}
function mergeMessages(component) {
  component.messages = {
    ...component.defaultMessages,
    ...getEffectiveMessageOverrides(component)
  };
}
function getEffectiveMessageOverrides(component) {
  var _a;
  return (_a = component.messageOverrides) !== null && _a !== void 0 ? _a : overridesFromIntlProps(component);
}
/**
 * This utility sets up the messages used by the component. It should be awaited in the `componentWillLoad` lifecycle hook.
 *
 * @param component
 */
export async function setUpMessages(component) {
  component.defaultMessages = await fetchMessages(component, component.effectiveLocale);
  mergeMessages(component);
}
async function fetchMessages(component, lang) {
  const { el } = component;
  const tag = el.tagName.toLowerCase();
  const componentName = tag.replace("calcite-", "");
  return getMessageBundle(getSupportedLocale(lang), componentName);
}
/**
 * This utility must be set up for the component to update its default message bundle if the locale changes.
 *
 * It can be set up in **either** of the following ways:
 *
 * 1. called from `LocalizedComponent`'s `onLocaleChange` method or
 * 2. called from a watcher configured to watch `LocalizedComponent`'s `effectiveLocale` prop
 *
 * @param component
 * @param lang
 */
export async function updateMessages(component, lang) {
  component.defaultMessages = await fetchMessages(component, lang);
}
/**
 * This utility sets up internals for messages support.
 *
 * It needs to be called in `connectedCallback`
 *
 * **Note**: this must be called after `LocalizedComponent`'s `connectLocalized` method.
 *
 * @param component
 */
export function connectMessages(component) {
  component.onMessagesChange = defaultOnMessagesChange;
}
/**
 * This utility tears down internals for messages support.
 *
 * It needs to be called in `disconnectedCallback`
 *
 * @param component
 */
export function disconnectMessages(component) {
  component.onMessagesChange = undefined;
}
function defaultOnMessagesChange() {
  mergeMessages(this);
}
