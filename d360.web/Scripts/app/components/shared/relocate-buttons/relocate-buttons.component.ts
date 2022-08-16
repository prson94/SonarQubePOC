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

  moveToTop() {
    this.moveToTopEvent.emit();
  }

  moveUp() {
    this.moveUpEvent.emit();
  }

  moveDown() {
    this.moveDownEvent.emit();
  }

  moveToBottom() {
    this.moveToBottomEvent.emit();
  }

  isMoveUpPossible(): boolean {
    return this.selectedItemsFromTarget?.length && !this.isFirstItemFromTargetSelected;
  }

  isMoveDownPossible(): boolean {
    return this.selectedItemsFromTarget?.length && !this.isLastItemFromTargetSelected;
  }
}
