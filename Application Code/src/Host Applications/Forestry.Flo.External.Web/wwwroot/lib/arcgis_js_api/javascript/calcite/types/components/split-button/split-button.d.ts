import { EventEmitter, VNode } from "../../stencil-public-runtime";
import { ButtonAppearance, ButtonColor, DropdownIconType } from "../button/interfaces";
import { DeprecatedEventPayload, FlipContext, Scale, Width } from "../interfaces";
import { OverlayPositioning } from "../../utils/floating-ui";
import { InteractiveComponent } from "../../utils/interactive";
/**
 * @slot - A slot for adding `calcite-dropdown` content.
 */
export declare class SplitButton implements InteractiveComponent {
  el: HTMLCalciteSplitButtonElement;
  /** Specifies the appearance style of the component. */
  appearance: ButtonAppearance;
  /** Specifies the color of the component. */
  color: ButtonColor;
  /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
  disabled: boolean;
  handleDisabledChange(value: boolean): void;
  /**
   * When `true`, the component is active.
   *
   * @internal
   */
  active: boolean;
  activeHandler(): void;
  /** Specifies the icon used for the dropdown menu. */
  dropdownIconType: DropdownIconType;
  /** Accessible name for the dropdown menu. */
  dropdownLabel?: string;
  /**
    When `true`, a busy indicator is displayed on the primary button.
   */
  loading: boolean;
  /**
   * Determines the type of positioning to use for the overlaid content.
   *
   * Using `"absolute"` will work for most cases. The component will be positioned inside of overflowing parent containers and will affect the container's layout.
   *
   * `"fixed"` should be used to escape an overflowing parent container, or when the reference element's `position` CSS property is `"fixed"`.
   *
   */
  overlayPositioning: OverlayPositioning;
  /** Specifies an icon to display at the end of the primary button. */
  primaryIconEnd?: string;
  /**  When `true`, the primary button icon will be flipped when the element direction is right-to-left (`"rtl"`). */
  primaryIconFlipRtl?: FlipContext;
  /** Specifies an icon to display at the start of the primary button. */
  primaryIconStart?: string;
  /** Accessible name for the primary button. */
  primaryLabel?: string;
  /** Text displayed in the primary button. */
  primaryText: string;
  /** Specifies the size of the component. */
  scale: Scale;
  /** Specifies the width of the component. */
  width: Width;
  /**
   * Fires when the primary button is clicked.
   *
   * **Note:** The event payload is deprecated, use separate mouse event listeners to get info about click.
   */
  calciteSplitButtonPrimaryClick: EventEmitter<DeprecatedEventPayload>;
  /**
   * Fires when the dropdown menu is clicked.
   *
   * **Note:** The event payload is deprecated, use separate mouse event listeners to get info about click.
   */
  calciteSplitButtonSecondaryClick: EventEmitter<DeprecatedEventPayload>;
  componentDidRender(): void;
  render(): VNode;
  private calciteSplitButtonPrimaryClickHandler;
  private calciteSplitButtonSecondaryClickHandler;
  private get dropdownIcon();
}
