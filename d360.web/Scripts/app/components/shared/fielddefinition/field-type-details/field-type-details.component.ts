import { Component, Input, OnChanges, OnInit, SimpleChanges, ViewEncapsulation } from '@angular/core';
import { FieldDisplayModel } from '../../../../models/fieldtype-api.model';

@Component({
	selector: 'd3s-field-type-details',
	templateUrl: './field-type-details.component.html',
	styleUrls: ['./field-type-details.component.less'],
	encapsulation: ViewEncapsulation.None
})
export class FieldTypeDetailsComponent implements OnInit, OnChanges {
	@Input() fieldType: FieldDisplayModel;

	isLoading: boolean = true;

	ascendingLabel: string = $localize`Ascending`;
	descendingLabel: string = $localize`Descending`;
	currentType: string = '';
	constructor() { }

	ngOnChanges(changes: SimpleChanges): void {
		this.isLoading = true;
		this.currentType = this.fieldType.FieldTypeValue;
		this.isLoading = false;
	}

	ngOnInit(): void {
	}

	editClick() {

	}
}
