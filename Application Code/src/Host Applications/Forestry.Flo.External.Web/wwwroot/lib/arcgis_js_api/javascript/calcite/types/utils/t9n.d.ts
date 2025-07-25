import { LocalizedComponent } from "./locale";
export declare type MessageBundle = Record<string, string>;
export declare const componentLangToMessageBundleCache: Record<string, Promise<MessageBundle>>;
/**
 * This util helps preserve existing intlProp usage when they have not been replaced by overrides.
 *
 * @param component
 */
export declare function overridesFromIntlProps(component: T9nComponent): MessageBundle;
/**
 * This utility sets up the messages used by the component. It should be awaited in the `componentWillLoad` lifecycle hook.
 *
 * @param component
 */
export declare function setUpMessages(component: T9nComponent): Promise<void>;
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
export declare function updateMessages(component: T9nComponent, lang: string): Promise<void>;
/**
 * This utility sets up internals for messages support.
 *
 * It needs to be called in `connectedCallback`
 *
 * **Note**: this must be called after `LocalizedComponent`'s `connectLocalized` method.
 *
 * @param component
 */
export declare function connectMessages(component: T9nComponent): void;
/**
 * This utility tears down internals for messages support.
 *
 * It needs to be called in `disconnectedCallback`
 *
 * @param component
 */
export declare function disconnectMessages(component: T9nComponent): void;
/**
 * This interface enables components to support built-in translation strings.
 *
 * **Notes**:
 *
 * This requires `LocalizedComponent` to be implemented.
 * To avoid unnecessary lookups, composite components should set `lang` on internal t9n components.
 */
export interface T9nComponent extends LocalizedComponent {
  el: HTMLElement;
  /**
   * This property holds all messages used by the component's rendering.
   *
   * This prop should use the `@State` decorator.
   */
  messages: MessageBundle;
  /**
   * This property holds the component's default messages.
   *
   * This prop should use the `@State` decorator.
   */
  defaultMessages: MessageBundle;
  /**
   * This property holds all user message overrides.
   *
   * This prop should use the `@Prop` decorator.
   */
  messageOverrides: Partial<MessageBundle>;
  /**
   * This private method ensures messages are kept in sync.
   *
   * This method should be empty and configured to watch for changes on `defaultMessages`, `messageOverrides` and any associated Intl prop.
   *
   * @Watch("intlMyPropA")
   * @Watch("intlMyPropZ")
   * @Watch("defaultMessages")
   * @Watch("messageOverrides")
   * onMessagesChange(): void {
   *  \/* wired up by t9n util *\/
   * }
   */
  onMessagesChange(): void;
}
