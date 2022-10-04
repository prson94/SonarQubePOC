import { ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { SiteNav } from '../../../models/site-menu.model';
import { DefaultTableSettingsService } from '../../../services/settings/default-table-settings.service';

@Component({
  selector: 'd3s-table-data-transfer',
  templateUrl: './table-data-transfer.component.html',
  styleUrls: ['./table-data-transfer.component.less']
})
export class TableDataTransferComponent implements OnInit {
  @Input() isRequired: boolean = true;
  @Input() emptyTargetTableMessage: string = 'Please select at least one item';
  @Input() infoButton: boolean = false;
  @Input() isSortButtons: boolean = false;
  @Input() itemsNameProperty: string = 'Title';
  @Input() sourceTableTitle: string = 'Source Table Title';
  @Input() targetTableTitle: string = 'Target Table Title';
  @Input() itemsFromSource: any[] = [];
  @Input() itemsFromTarget: any[] = [];
  @Output() itemsFromSourceChange = new EventEmitter<any[]>();
  @Output() itemsFromTargetChange = new EventEmitter<any[]>();
  @Output() showInfoEvent = new EventEmitter<object>();

  viewingItem: object;
  sourceTableSearchValue: string = '';
  targetTableSearchValue: string = '';
  selectedItemsFromSource: any[] = [];
  selectedItemsFromTarget: any[] = [];

  constructor(
    public cdRef: ChangeDetectorRef,
    public defaultTableSettingsService: DefaultTableSettingsService,
  ) { }

  get isFirstItemFromTargetSelected(): boolean {
    return this.selectedItemsFromTarget.findIndex((selectedItem) => selectedItem.ObjectID === this.itemsFromTarget[0].ObjectID && selectedItem.Object === this.itemsFromTarget[0].Object) > -1;
  }

  get isLastItemFromTargetSelected(): boolean {
    const lastIndex: number = this.itemsFromTarget.length - 1;
    return this.selectedItemsFromTarget.findIndex((selectedItem): boolean => {
      return selectedItem.ObjectID === this.itemsFromTarget[lastIndex].ObjectID && selectedItem.Object === this.itemsFromTarget[lastIndex].Object; // eslint-disable-line
    }) > -1;
  }

  ngOnInit(): void {
  }

  setIndexes(arrayOfObjects: object[]): void {
    arrayOfObjects.forEach((object, i) => {
      object['index'] = i;
    });
  }

  isMoveUpPossible(): boolean {
    return this.selectedItemsFromTarget?.length && !this.isFirstItemFromTargetSelected;
  }

  isMoveDownPossible(): boolean {
    return this.selectedItemsFromTarget?.length && !this.isLastItemFromTargetSelected;
  }

  sortArrayByIndexes(array: SiteNav[]): void {
    array.sort((a, b) => a.index - b.index);
  }

  sortArrayByIndexesReverse(array: SiteNav[]): void {
    array.sort((a, b) => b.index - a.index);
  }

  moveUp() {
    this.setIndexes(this.itemsFromTarget);
    this.sortArrayByIndexes(this.selectedItemsFromTarget);
    this.selectedItemsFromTarget.forEach((selectedItemFromTarget: SiteNav) => {
      this.itemsFromTarget.splice(selectedItemFromTarget.index - 1, 0, this.itemsFromTarget.splice(selectedItemFromTarget.index, 1)[0]);
    });
    this.itemsFromTarget = [...this.itemsFromTarget];
  }

  moveDown() {
    this.setIndexes(this.itemsFromTarget);
    this.sortArrayByIndexesReverse(this.selectedItemsFromTarget);
    this.selectedItemsFromTarget.forEach((selectedItemFromTarget: SiteNav) => {
      this.itemsFromTarget.splice(selectedItemFromTarget.index + 1, 0, this.itemsFromTarget.splice(selectedItemFromTarget.index, 1)[0]);
    });
    this.itemsFromTarget = [...this.itemsFromTarget];
  }

  moveToTop() {
    if (this.selectedItemsFromTarget.length > 0) {
      for (let j = 0; j < this.selectedItemsFromTarget.length; j++) {
        let x = this.itemsFromTarget.findIndex((i) => i.ObjectID === this.selectedItemsFromTarget[j].ObjectID && i.Object === this.selectedItemsFromTarget[j].Object); // eslint-disable-line
        this.itemsFromTarget.splice(j, 0, this.itemsFromTarget.splice(x, 1)[0]);
      }
    }
    this.itemsFromTarget = [...this.itemsFromTarget];
  }

  moveToBottom() {
    if (this.selectedItemsFromTarget.length > 0) {
      for (let j = 0; j < this.selectedItemsFromTarget.length; j++) {
        let x = this.itemsFromTarget.findIndex((i) => i.ObjectID === this.selectedItemsFromTarget[j].ObjectID && i.Object === this.selectedItemsFromTarget[j].Object); // eslint-disable-line
        let newPosition = this.itemsFromTarget.length - j;
        this.itemsFromTarget.splice(newPosition - 1, 0, this.itemsFromTarget.splice(x, 1)[0]);
      }
    }
    this.itemsFromTarget = [...this.itemsFromTarget];
  }

  moveFromSourceToTarget() {
    this.selectedItemsFromSource.forEach((selectedItemFromSource) => {
      this.itemsFromTarget.push(selectedItemFromSource)
      this.itemsFromSource = this.itemsFromSource.filter((itemFromSource) => itemFromSource !== selectedItemFromSource);
    });

    this.clearInfoPanel();

    this.selectedItemsFromSource = [];
    this.itemsFromTarget = [...this.itemsFromTarget];
    this.itemsFromSource = [...this.itemsFromSource];
    this.itemsFromTargetChange.emit(this.itemsFromTarget);
    this.cdRef.markForCheck();
  }

  moveFromTargetToSource() {
    this.selectedItemsFromTarget.forEach((selectedItemFromTarget) => {
      this.itemsFromSource.push(selectedItemFromTarget)
      this.itemsFromTarget = this.itemsFromTarget.filter((itemFromTarget) => itemFromTarget !== selectedItemFromTarget);
    });

    this.itemsFromSource.sort((a, b) => a[this.itemsNameProperty]?.localeCompare(b[this.itemsNameProperty]));
    this.selectedItemsFromTarget = [];
    this.itemsFromTarget = [...this.itemsFromTarget];
    this.itemsFromSource = [...this.itemsFromSource];
    this.itemsFromTargetChange.emit(this.itemsFromTarget);
    this.cdRef.markForCheck();
  }

  showInfo(item: object) {
    this.viewingItem = item;
    this.showInfoEvent.emit(item);
  }

  clearInfoPanel() {
    if (this.viewingItem) {
      let found = this.selectedItemsFromSource.find((selectedItemFromSource) => selectedItemFromSource == this.viewingItem)
      if (found) {
        this.showInfo({});
      }
    }
  }
}
