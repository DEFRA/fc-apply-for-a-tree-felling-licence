import { VNode } from "../../stencil-public-runtime";
import { FlipContext } from "../interfaces";
import { InteractiveComponent } from "../../utils/interactive";
/** Any attributes placed on <calcite-link> component will propagate to the rendered child */
/** Passing a 'href' will render an anchor link, instead of a span. Role will be set to link, or link, depending on this. */
/** It is the consumers responsibility to add aria information, rel, target, for links, and any link attributes for form submission */
/** @slot - A slot for adding text. */
export declare class Link implements InteractiveComponent {
  el: HTMLCalciteLinkElement;
  /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
  disabled: boolean;
  /**
   * Prompts the user to save the linked URL instead of navigating to it. Can be used with or without a value:
   * Without a value, the browser will suggest a filename/extension
   * See https://developer.mozilla.org/en-US/docs/Web/HTML/Element/a#attr-download.
   */
  download: string | boolean;
  /** Specifies the URL of the linked resource, which can be set as an absolute or relative path. */
  href?: string;
  /** Specifies an icon to display at the end of the component. */
  iconEnd?: string;
  /** When `true`, the icon will be flipped when the element direction is right-to-left (`"rtl"`). */
  iconFlipRtl?: FlipContext;
  /** Specifies an icon to display at the start of the component. */
  iconStart?: string;
  /** Specifies the relationship to the linked document defined in `href`. */
  rel?: string;
  /** Specifies the frame or window to open the linked document. */
  target?: string;
  componentDidRender(): void;
  render(): VNode;
  clickHandler(event: PointerEvent): void;
  /** Sets focus on the component. */
  setFocus(): Promise<void>;
  /** the rendered child element */
  private childEl;
  private childElClickHandler;
  private storeTagRef;
}
