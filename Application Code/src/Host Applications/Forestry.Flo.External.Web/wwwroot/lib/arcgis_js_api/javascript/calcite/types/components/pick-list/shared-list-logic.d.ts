import { PickList } from "./pick-list";
import { ValueList } from "../value-list/value-list";
declare type Lists = PickList | ValueList;
declare type ListItemElement<T> = T extends PickList ? HTMLCalcitePickListItemElement : HTMLCalciteValueListItemElement;
declare type List<T> = T extends PickList ? PickList : ValueList;
export declare type ListFocusId = "filter";
export declare function mutationObserverCallback<T extends Lists>(this: List<T>): void;
export declare function initialize<T extends Lists>(this: List<T>): void;
export declare function initializeObserver<T extends Lists>(this: List<T>): void;
export declare function cleanUpObserver<T extends Lists>(this: List<T>): void;
export declare function calciteListItemChangeHandler<T extends Lists>(this: List<T>, event: CustomEvent): void;
export declare function calciteInternalListItemValueChangeHandler<T extends Lists>(this: List<T>, event: CustomEvent): void;
export declare function calciteListFocusOutHandler<T extends Lists>(this: List<T>, event: FocusEvent): void;
export declare function keyDownHandler<T extends Lists>(this: List<T>, event: KeyboardEvent): void;
export declare function moveItemIndex<T extends Lists>(list: List<T>, item: ListItemElement<T>, direction: "up" | "down"): number;
export declare function getItemIndex<T extends Lists>(list: List<T>, item: ListItemElement<T>): number;
export declare function internalCalciteListChangeEvent<T extends Lists>(this: List<T>): void;
export declare function removeItem<T extends Lists, U extends ListItemElement<T>>(this: List<T>, event: CustomEvent): void;
export declare function setFocus<T extends Lists>(this: List<T>, focusId: ListFocusId): Promise<void>;
export declare function setUpItems<T extends Lists>(this: List<T>, tagName: T extends PickList ? "calcite-pick-list-item" : "calcite-value-list-item"): void;
export declare function setUpFilter<T extends Lists>(this: List<T>): void;
export declare function deselectRemovedItems<T extends Lists>(this: List<T>): void;
export declare function deselectSiblingItems<T extends Lists>(this: List<T>, item: ListItemElement<T>): void;
export declare function selectSiblings<T extends Lists>(this: List<T>, item: ListItemElement<T>, deselect?: boolean): void;
export declare function handleFilter<T extends Lists>(this: List<T>, event: CustomEvent): void;
export declare type ItemData = {
  label: string;
  description: string;
  metadata: Record<string, unknown>;
  value: string;
}[];
export declare function getItemData<T extends Lists>(this: List<T>): ItemData;
export {};
