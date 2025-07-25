import { EventEmitter, VNode } from "../../stencil-public-runtime";
import { Scale } from "../interfaces";
import { LabelableComponent } from "../../utils/label";
import { InteractiveComponent } from "../../utils/interactive";
/**
 * @slot - A slot for adding a `calcite-input`.
 */
export declare class InlineEditable implements InteractiveComponent, LabelableComponent {
  el: HTMLCalciteInlineEditableElement;
  /** specify whether editing can be enabled */
  disabled: boolean;
  disabledWatcher(disabled: boolean): void;
  /** specify whether the wrapped input element is editable, defaults to false */
  editingEnabled: boolean;
  editingEnabledWatcher(newValue: boolean, oldValue: boolean): void;
  /** specify whether the confirm button should display a loading state, defaults to false */
  loading: boolean;
  /** specify whether save/cancel controls should be displayed when editingEnabled is true, defaults to false */
  controls: boolean;
  /**
   * specify text to be user for the enable editing button's aria-label, defaults to `Click to edit`
   *
   * @default "Click to edit"
   */
  intlEnableEditing: string;
  /**
   * specify text to be user for the cancel editing button's aria-label, defaults to `Cancel`
   *
   * @default "Cancel"
   */
  intlCancelEditing: string;
  /**
   * specify text to be user for the confirm changes button's aria-label, defaults to `Save`
   *
   * @default "Save"
   */
  intlConfirmChanges: string;
  /** specify the scale of the inline-editable component, defaults to the scale of the wrapped calcite-input or the scale of the closest wrapping component with a set scale */
  scale?: Scale;
  /** when controls, specify a callback to be executed prior to disabling editing. when provided, loading state will be handled automatically. */
  afterConfirm?: () => Promise<void>;
  connectedCallback(): void;
  disconnectedCallback(): void;
  componentDidRender(): void;
  render(): VNode;
  /**
   * Emitted when the cancel button gets clicked.
   */
  calciteInlineEditableEditCancel: EventEmitter<void>;
  /**
   * Emitted when the check button gets clicked.
   */
  calciteInlineEditableEditConfirm: EventEmitter<void>;
  /**
   * @internal
   */
  calciteInternalInlineEditableEnableEditingChange: EventEmitter<void>;
  blurHandler(): void;
  private inputElement;
  private valuePriorToEditing;
  private shouldEmitCancel;
  private enableEditingButton;
  private cancelEditingButton;
  private confirmEditingButton;
  labelEl: HTMLCalciteLabelElement;
  mutationObserver: import("../../utils/observers").ExtendedMutationObserver;
  setFocus(): Promise<void>;
  mutationObserverCallback(): void;
  onLabelClick(): void;
  updateSlottedInput(): void;
  private get shouldShowControls();
  private enableEditing;
  private disableEditing;
  private cancelEditing;
  private escapeKeyHandler;
  private cancelEditingHandler;
  private enableEditingHandler;
  private confirmChangesHandler;
}
