import { ChangeDetectorRef, Component, Input, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { SiteNav } from '../../../models/site-menu.model';
import { DefaultTableSettingsService } from '../../../services/settings/default-table-settings.service';
import { cloneDeep } from 'lodash';

@Component({
  selector: 'd3s-table-data-transfer',
  templateUrl: './table-data-transfer.component.html',
  styleUrls: ['./table-data-transfer.component.less']
})
export class TableDataTransferComponent implements OnInit {
  @Input() sourceTableTitle = 'Source Table Title';
  @Input() targetTableTitle = 'Target Table Title';
  simpleTextFilter: string = '';
  simpleTextFilterForExistingItems: string = '';

  itemsFromSource: any[] = [
    { Title: "Source 1"},
    { Title: "Source 2"},
    { Title: "Source 3"},
    { Title: "Source 4"},
    { Title: "Source 5"},
    { Title: "Source 6"},
    { Title: "Source 7"},
    { Title: "Source 8"},
  ];
  itemsFromTarget: any[] = [
    { Title: "Target 1"},
    { Title: "Target 2"},
    { Title: "Target 3"},
    { Title: "Target 4"},
    { Title: "Target 5"},
    { Title: "Target 6"},
    { Title: "Target 7"},
  ];
  selectedItemsFromSource: any[];
  selectedItemsFromTarget: SiteNav[] = [];
  
  isSourceTableLoading: boolean = false;

  requiredCount: number = 2;
  folderModel: SiteNav;
  hasFolderItems: boolean = false;



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

  setRequiredCount() {
		this.requiredCount = 2;
		if (this.folderModel.Title?.length > 0) {
			this.requiredCount--;
		}
		if (this.itemsFromTarget?.length > 0 || !this.hasFolderItems) {
			this.requiredCount--;
		}
	}

  setIndexes(arrayOfObjects: object[]): void {
		arrayOfObjects.forEach((object, i) => {
			object['index'] = i;
		});
	}

  selectAllItemsFromSource(table: Table) {
    this.selectedItemsFromSource = [];
    for (let i = table.first; i < table.first + table.rows; i++) {
      this.selectedItemsFromSource.push(this.itemsFromSource.find((item) => item.ID === table.selection[i]?.ID));
    }
  }

  selectAllItemsFromTarget(table: Table) {
		this.selectedItemsFromTarget = [];
		for (let i = 0; i < table.selection.length; i++) {
			let x: number = this.itemsFromTarget.findIndex((item) => item.ObjectID === table.selection[i].ObjectID && item.Object === table.selection[i].Object); // eslint-disable-line
			this.selectedItemsFromTarget.push(cloneDeep(this.itemsFromTarget[x])); // eslint-disable-line
		}
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

  addToSelectedFolderItems() {
    if (this.selectedItemsFromSource.length > 0) {
      for (let j = 0; j < this.selectedItemsFromSource.length; j++) {
        if (this.itemsFromTarget.indexOf(this.selectedItemsFromSource[j]) === -1) { // eslint-disable-line
          this.itemsFromTarget.push(this.selectedItemsFromSource[j]); // eslint-disable-line
          this.itemsFromSource = this.itemsFromSource.filter((x) => x != this.selectedItemsFromSource[j]);
        }
      }
      this.itemsFromTarget = this.itemsFromTarget.sort((a, b) => a.Title.localeCompare(b.Title));
      this.selectedItemsFromSource = [];
      this.setRequiredCount();
    }
    this.itemsFromTarget = [...this.itemsFromTarget];
    this.cdRef.markForCheck();
  }

  removeFromSelectedFolderItems() {
    if (this.selectedItemsFromTarget.length > 0) {
      for (let j = 0; j < this.selectedItemsFromTarget.length; j++) {
        let x = this.itemsFromSource.findIndex((i) => i.ObjectID === this.selectedItemsFromTarget[j].ObjectID && i.Object === this.selectedItemsFromTarget[j].Object); // eslint-disable-line
        let y = this.itemsFromTarget.findIndex((i) => i.ObjectID === this.selectedItemsFromTarget[j].ObjectID && i.Object === this.selectedItemsFromTarget[j].Object); // eslint-disable-line
        if (y > -1) {
          let i = cloneDeep(this.itemsFromTarget.splice(y, 1)[0]);
          if (x === -1) {
            this.itemsFromSource.push(i);
          }
        }
      }
      this.itemsFromSource = this.itemsFromSource.sort((a, b) => a.Title.localeCompare(b.Title));
      this.selectedItemsFromTarget = [];
      this.setRequiredCount();
    }
    this.itemsFromTarget = [...this.itemsFromTarget];
    this.itemsFromSource = [...this.itemsFromSource];
    this.cdRef.markForCheck();
  }
}
