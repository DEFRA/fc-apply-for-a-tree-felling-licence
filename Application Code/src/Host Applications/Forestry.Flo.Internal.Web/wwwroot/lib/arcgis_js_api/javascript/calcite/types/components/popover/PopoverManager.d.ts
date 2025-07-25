import { ReferenceElement } from "../../utils/floating-ui";
export default class PopoverManager {
  private registeredElements;
  private registeredElementCount;
  registerElement(referenceEl: ReferenceElement, popover: HTMLCalcitePopoverElement): void;
  unregisterElement(referenceEl: ReferenceElement): void;
  private queryPopover;
  private togglePopovers;
  private keyHandler;
  private clickHandler;
  private addListeners;
  private removeListeners;
}
