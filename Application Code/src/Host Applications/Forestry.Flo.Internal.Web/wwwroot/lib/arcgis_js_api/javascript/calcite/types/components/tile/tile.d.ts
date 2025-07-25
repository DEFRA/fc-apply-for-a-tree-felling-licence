import { VNode } from "../../stencil-public-runtime";
import { ConditionalSlotComponent } from "../../utils/conditionalSlot";
import { InteractiveComponent } from "../../utils/interactive";
/**
 * @slot content-start - A slot for adding non-actionable elements before the component's content.
 * @slot content-end - A slot for adding non-actionable elements after the component's content.
 */
export declare class Tile implements ConditionalSlotComponent, InteractiveComponent {
  el: HTMLCalciteTileElement;
  /**
   * When `true`, the component is active.
   */
  active: boolean;
  /**
   * A description for the component, which displays below the heading.
   */
  description?: string;
  /**
   * When `true`, interaction is prevented and the component is displayed with lower opacity.
   */
  disabled: boolean;
  /**
   * The component's embed mode.
   *
   * When `true`, renders without a border and padding for use by other components.
   */
  embed: boolean;
  /**
   * The focused state of the component.
   *
   * @internal
   */
  focused: boolean;
  /** The component header text, which displays between the icon and description. */
  heading?: string;
  /** When `true`, the component is not displayed and is not focusable.  */
  hidden: boolean;
  /** When embed is `"false"`, the URL for the component. */
  href?: string;
  /** Specifies an icon to display. */
  icon?: string;
  connectedCallback(): void;
  disconnectedCallback(): void;
  componentDidRender(): void;
  renderTile(): VNode;
  render(): VNode;
}
