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

  @Output() relocateItemsFromSourceToTargetEvent = new EventEmitter();
  @Output() relocateItemsFromTargetToSourceEvent = new EventEmitter();
  @Output() moveToTopEvent = new EventEmitter();

  constructor() { }

  ngOnInit(): void {
  }

  relocateSelectedItemsFromSourceToTarget(): void {
    this.relocateItemsFromSourceToTargetEvent.emit();
  }

  relocateSelectedItemsFromTargetToSource(): void {
    this.relocateItemsFromTargetToSourceEvent.emit();
  }

  disableBtnMoveToTop() {
		
	}

  addToSelectedFolderItems() {}
  removeFromSelectedFolderItems() {}
  moveToTop() {}
  moveUp() {}
  moveDown() {}
  moveToBottom() {}
  disableBtnMoveUp() {}
  disableBtnMoveDown() {}
  disableBtnMoveToBottom() {}

  isButtonMoveToTopActive(): boolean {
    return this.selectedItemsFromTarget?.length && !this.isFirstItemFromTargetSelected;
  }

  isButtonMoveUpActive(): boolean {
    return 
  }

  isButtonMoveDownActive(): boolean {
    return 
  }

  isButtonMoveToBottomActive(): boolean {
    return 
  }

}
