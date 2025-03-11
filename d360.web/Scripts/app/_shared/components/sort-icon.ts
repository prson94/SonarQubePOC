import { Component, EventEmitter, Input, OnDestroy, OnInit, Output, Optional } from "@angular/core";
import { Table } from 'primeng/table';
import { CommonModule } from "@angular/common";
import { TreeTable } from "primeng/treetable";


@Component({
	selector: 'sort-icon',
	standalone: true,
	templateUrl: './sort-icon.html',
	imports: [CommonModule],
})
export class SortIconComponent implements OnInit, OnDestroy {
	@Input() field: string;
	@Input() ariaLabel: string;
	@Input() ariaLabelDesc: string;
	@Input() ariaLabelAsc: string;

	subscription: any;
	sortOrder: number;

	@Output() changeCallback = new EventEmitter();

	dt: Table | TreeTable;

	constructor(@Optional() table: Table, @Optional() treeTable: TreeTable) {
		if (table == null && treeTable == null) {
			throw new Error('Failed to resolve primeng/Table or primeng/TreeTable');
		}

		this.dt = table ?? treeTable;
		this.subscription = this.dt.tableService.sortSource$.subscribe((sortMeta) => {
			this.updateSortState();
		});
	}

	ngOnInit() {
		this.updateSortState();
	}

	onClick(event) {
		event.preventDefault();
	}

	updateSortState() {
		if (this.dt.sortMode === 'single') {
			this.sortOrder = this.dt.isSorted(this.field) ? this.dt.sortOrder : 0;
		}
		else if (this.dt.sortMode === 'multiple') {
			const sortMeta = this.dt.getSortMeta(this.field);
			this.sortOrder = sortMeta ? sortMeta.order : 0;
		}
		this.changeCallback.emit({ field: this.dt.sortField, order: this.dt.sortOrder });

	}

	get ariaText(): string {
		let text: string;

		switch (this.sortOrder) {
			case 1:
				text = this.ariaLabelAsc;
				break;

			case -1:
				text = this.ariaLabelDesc;
				break;

			default:
				text = this.ariaLabel;
				break;
		}

		return text;
	}

	ngOnDestroy() {
		if (this.subscription) {
			this.subscription.unsubscribe();
		}
	}
}
