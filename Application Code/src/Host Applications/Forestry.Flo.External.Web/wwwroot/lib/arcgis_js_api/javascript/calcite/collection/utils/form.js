/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { closestElementCrossShadowBoundary } from "./dom";
import { h } from "@stencil/core";
/**
 * Exported for testing purposes.
 */
export const hiddenFormInputSlotName = "hidden-form-input";
function isCheckable(component) {
  return "checked" in component;
}
const onFormResetMap = new WeakMap();
const formComponentSet = new WeakSet();
function hasRegisteredFormComponentParent(form, formComponentEl) {
  // we use events as a way to test for nested form-associated components across shadow bounds
  const formComponentRegisterEventName = "calciteInternalFormComponentRegister";
  let hasRegisteredFormComponentParent = false;
  form.addEventListener(formComponentRegisterEventName, (event) => {
    hasRegisteredFormComponentParent = event
      .composedPath()
      .some((element) => formComponentSet.has(element));
    event.stopPropagation();
  }, { once: true });
  formComponentEl.dispatchEvent(new CustomEvent(formComponentRegisterEventName, {
    bubbles: true,
    composed: true
  }));
  return hasRegisteredFormComponentParent;
}
/**
 * Helper to submit a form.
 *
 * @param component
 * @returns true if its associated form was submitted, false otherwise.
 */
export function submitForm(component) {
  const { formEl } = component;
  if (!formEl) {
    return false;
  }
  "requestSubmit" in formEl ? formEl.requestSubmit() : formEl.submit();
  return true;
}
/**
 * Helper to reset a form.
 *
 * @param component
 */
export function resetForm(component) {
  var _a;
  (_a = component.formEl) === null || _a === void 0 ? void 0 : _a.reset();
}
/**
 * Helper to set up form interactions on connectedCallback.
 *
 * @param component
 */
export function connectForm(component) {
  const { el, value } = component;
  const form = closestElementCrossShadowBoundary(el, "form");
  if (!form || hasRegisteredFormComponentParent(form, el)) {
    return;
  }
  component.formEl = form;
  component.defaultValue = value;
  if (isCheckable(component)) {
    component.defaultChecked = component.checked;
  }
  const boundOnFormReset = (component.onFormReset || onFormReset).bind(component);
  form.addEventListener("reset", boundOnFormReset);
  onFormResetMap.set(component.el, boundOnFormReset);
  formComponentSet.add(el);
}
function onFormReset() {
  if (isCheckable(this)) {
    this.checked = this.defaultChecked;
    return;
  }
  this.value = this.defaultValue;
}
/**
 * Helper to tear down form interactions on disconnectedCallback.
 *
 * @param component
 */
export function disconnectForm(component) {
  const { el, formEl } = component;
  if (!formEl) {
    return;
  }
  const boundOnFormReset = onFormResetMap.get(el);
  formEl.removeEventListener("reset", boundOnFormReset);
  onFormResetMap.delete(el);
  component.formEl = null;
  formComponentSet.delete(el);
}
/**
 * Helper for setting the default value on initialization after connectedCallback.
 *
 * Note that this is only needed if the default value cannot be determined on connectedCallback.
 *
 * @param component
 * @param value
 */
export function afterConnectDefaultValueSet(component, value) {
  component.defaultValue = value;
}
const hiddenInputChangeHandler = (event) => {
  event.target.dispatchEvent(new CustomEvent("calciteInternalHiddenInputChange", { bubbles: true }));
};
const removeHiddenInputChangeEventListener = (input) => input.removeEventListener("change", hiddenInputChangeHandler);
/**
 * Helper for maintaining a form-associated's hidden input in sync with the component.
 *
 * Based on Ionic's approach: https://github.com/ionic-team/ionic-framework/blob/e4bf052794af9aac07f887013b9250d2a045eba3/core/src/utils/helpers.ts#L198
 *
 * @param component
 */
function syncHiddenFormInput(component) {
  const { el, formEl, name, value } = component;
  const { ownerDocument } = el;
  const inputs = el.querySelectorAll(`input[slot="${hiddenFormInputSlotName}"]`);
  if (!formEl || !name) {
    inputs.forEach((input) => {
      removeHiddenInputChangeEventListener(input);
      input.remove();
    });
    return;
  }
  const values = Array.isArray(value) ? value : [value];
  const extra = [];
  const seen = new Set();
  inputs.forEach((input) => {
    const valueMatch = values.find((val) => 
    /* intentional non-strict equality check */
    val == input.value);
    if (valueMatch != null) {
      seen.add(valueMatch);
      defaultSyncHiddenFormInput(component, input, valueMatch);
    }
    else {
      extra.push(input);
    }
  });
  let docFrag;
  values.forEach((value) => {
    if (seen.has(value)) {
      return;
    }
    let input = extra.pop();
    if (!input) {
      input = ownerDocument.createElement("input");
      input.slot = hiddenFormInputSlotName;
    }
    if (!docFrag) {
      docFrag = ownerDocument.createDocumentFragment();
    }
    docFrag.append(input);
    // emits when hidden input is autofilled
    input.addEventListener("change", hiddenInputChangeHandler);
    defaultSyncHiddenFormInput(component, input, value);
  });
  if (docFrag) {
    el.append(docFrag);
  }
  extra.forEach((input) => {
    removeHiddenInputChangeEventListener(input);
    input.remove();
  });
}
function defaultSyncHiddenFormInput(component, input, value) {
  var _a;
  const { defaultValue, disabled, name, required } = component;
  // keep in sync to prevent losing reset value
  input.defaultValue = defaultValue;
  input.disabled = disabled;
  input.name = name;
  input.required = required;
  input.tabIndex = -1;
  if (isCheckable(component)) {
    // keep in sync to prevent losing reset value
    input.defaultChecked = component.defaultChecked;
    // heuristic to support default/on mode from https://html.spec.whatwg.org/multipage/input.html#dom-input-value-default-on
    input.value = component.checked ? value || "on" : "";
    // we disable the component when not checked to avoid having its value submitted
    if (!disabled && !component.checked) {
      input.disabled = true;
    }
  }
  else {
    input.value = value || "";
  }
  (_a = component.syncHiddenFormInput) === null || _a === void 0 ? void 0 : _a.call(component, input);
}
/**
 * Helper to render the slot for form-associated component's hidden input.
 *
 * If the component has a default slot, this must be placed at the bottom of the component's root container to ensure it is the last child.
 *
 * render(): VNode {
 *   <Host>
 *     <div class={CSS.container}>
 *     // ...
 *     <HiddenFormInputSlot component={this} />
 *     </div>
 *   </Host>
 * }
 *
 * Note that the hidden-form-input Sass mixin must be added to the component's style to apply specific styles.
 *
 * @param root0
 * @param root0.component
 */
export const HiddenFormInputSlot = ({ component }) => {
  syncHiddenFormInput(component);
  return h("slot", { name: hiddenFormInputSlotName });
};
