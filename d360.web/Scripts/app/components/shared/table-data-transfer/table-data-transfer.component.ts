import { ChangeDetectorRef, Component, Input, OnInit } from '@angular/core';
import { cloneDeep } from 'lodash';
import { SiteNav } from '../../../models/site-menu.model';
import { DefaultTableSettingsService } from '../../../services/settings/default-table-settings.service';

@Component({
  selector: 'd3s-table-data-transfer',
  templateUrl: './table-data-transfer.component.html',
  styleUrls: ['./table-data-transfer.component.less']
})
export class TableDataTransferComponent implements OnInit {
  @Input() sourceTableTitle: string = 'Source Table Title';
  @Input() targetTableTitle: string = 'Target Table Title';
  @Input() itemsFromSource: any[] = [];
  @Input() itemsFromTarget: any[] = [];

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
    for (let j = 0; j < this.selectedItemsFromSource.length; j++) {
      if (this.itemsFromTarget.indexOf(this.selectedItemsFromSource[j]) === -1) {
        this.itemsFromTarget.push(this.selectedItemsFromSource[j]);
        this.itemsFromSource = this.itemsFromSource.filter((x) => x != this.selectedItemsFromSource[j]);
      }
    }
    this.itemsFromTarget = this.itemsFromTarget.sort((a, b) => a.Title.localeCompare(b.Title));
    this.selectedItemsFromSource = [];
    this.itemsFromTarget = [...this.itemsFromTarget];
    this.cdRef.markForCheck();
  }

  moveFromTargetToSource() {
    for (let j = 0; j < this.selectedItemsFromTarget.length; j++) {
      let x: number = this.itemsFromSource.findIndex((itemFromSource) => itemFromSource.ObjectID === this.selectedItemsFromTarget[j].ObjectID && itemFromSource.Object === this.selectedItemsFromTarget[j].Object);
      let y: number = this.itemsFromTarget.findIndex((i) => i.ObjectID === this.selectedItemsFromTarget[j].ObjectID && i.Object === this.selectedItemsFromTarget[j].Object);
      if (y > -1) {
        let i = cloneDeep(this.itemsFromTarget.splice(y, 1)[0]);
        if (x === -1) {
          this.itemsFromSource.push(i);
        }
      }
    }
    this.itemsFromSource = this.itemsFromSource.sort((a, b) => a.Title.localeCompare(b.Title));
    this.selectedItemsFromTarget = [];
    this.itemsFromTarget = [...this.itemsFromTarget];
    this.itemsFromSource = [...this.itemsFromSource];
    this.cdRef.markForCheck();
  }
}
