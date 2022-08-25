import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
  selector: 'd3s-relocate-buttons',
  templateUrl: './relocate-buttons.component.html',
  styleUrls: ['./relocate-buttons.component.less']
})
export class RelocateButtonsComponent implements OnInit {
  @Input() sourceItems: any[] = [];
  @Input() targetItems: any[] = [];
  @Input() selectedItemsFromSource: any[] = [];
  @Input() selectedItemsFromTarget: any[] = [];
  @Input() isFirstItemFromTargetSelected: boolean;
  @Input() isLastItemFromTargetSelected: boolean;
  @Input() isOrderButtonsPresent: boolean = true;
  @Input() relocateToTargetButtonTooltip: string = "Add to selected folder items";
  @Input() relocateToSourceButtonTooltip: string = "Remove from selected folder items";

  @Output() relocateItemsFromSourceToTargetEvent = new EventEmitter();
  @Output() relocateItemsFromTargetToSourceEvent = new EventEmitter();
  @Output() moveToTopEvent = new EventEmitter();
  @Output() moveUpEvent = new EventEmitter();
  @Output() moveDownEvent = new EventEmitter();
  @Output() moveToBottomEvent = new EventEmitter();

  constructor() { }

  ngOnInit(): void {
  }

  relocateSelectedItemsFromSourceToTarget(): void {
    this.relocateItemsFromSourceToTargetEvent.emit();
  }

  relocateSelectedItemsFromTargetToSource(): void {
    this.relocateItemsFromTargetToSourceEvent.emit();
  }

  moveToTop(): void {
    this.moveToTopEvent.emit();
  }

  moveUp(): void {
    this.moveUpEvent.emit();
  }

  moveDown(): void {
    this.moveDownEvent.emit();
  }

  moveToBottom(): void {
    this.moveToBottomEvent.emit();
  }

  isMoveUpPossible(): boolean {
    return this.selectedItemsFromTarget?.length && !this.isFirstItemFromTargetSelected;
  }

  isMoveDownPossible(): boolean {
    return this.selectedItemsFromTarget?.length && !this.isLastItemFromTargetSelected;
  }
}
