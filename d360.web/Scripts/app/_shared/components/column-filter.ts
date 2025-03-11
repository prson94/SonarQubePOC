import { Component, EventEmitter, Input, Output,  } from "@angular/core";
import { Table } from 'primeng/table';

@Component({
	selector: 'column-filter',
	standalone: true,
	templateUrl: './column-filter.html'
})
export class ColumnFilterComponent {
	@Input() datatype: string = 'text';
	@Input() field: string;
	@Input() filterMatchMode = 'contains';
	@Input() value: any;
	@Output() onChangeCallback = new EventEmitter();

	constructor(public dt: Table) {
	}

	onChange(event) {
		if (this.onChangeCallback) {
			this.onChangeCallback.emit({ value: event, prop: this.field });
		}
	}

}
