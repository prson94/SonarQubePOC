import { Component, Input, OnInit } from '@angular/core';
import { FieldDisplayModel } from '../../../../models/fieldtype-api.model';

@Component({
	selector: 'd3s-field-type-details',
	templateUrl: './field-type-details.component.html',
	styleUrls: ['./field-type-details.component.less']
})
export class FieldTypeDetailsComponent implements OnInit {
	@Input() fieldType: FieldDisplayModel;

	constructor() { }

	ngOnInit(): void {
	}

}
