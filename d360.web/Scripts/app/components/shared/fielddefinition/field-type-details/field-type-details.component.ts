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

	hasPartOfKey() {
		const excludeTypes: string[] = ['Path', 'ComputedRelationshipField', 'Json', 'Link', 'ComputedOwnershipLookup', 'ComputedRelationshipReferenceList', 'ComputedRelationshipLookup', 'Relationship', 'Score', 'Tag'];
		if (excludeTypes.indexOf(this.fieldType.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}
	hasRequired() {
		const excludeTypes: string[] = ['Path', 'Counter', 'ComputedRelationshipField', 'Json', 'ComputedOwnershipLookup', 'ComputedRelationshipReferenceList', 'ComputedRelationshipLookup', 'Relationship', 'Score', 'Tag'];
		if (excludeTypes.indexOf(this.fieldType.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}
	hasDisplayInColumn() {
		const excludeTypes: string[] = ['Json', 'ComputedOwnershipLookup', 'ComputedRelationshipReferenceList', 'ComputedRelationshipLookup', 'Tag'];
		if (excludeTypes.indexOf(this.fieldType.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}
	hasShowIfEmpty() {
		const excludeTypes: string[] = ['Path', 'Json', 'Tag'];
		if (excludeTypes.indexOf(this.fieldType.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}
	hasIsListable() {
		const excludeTypes: string[] = ['ComputedRelationshipReferenceList', 'ComputedRelationshipLookup', 'Relationship'];
		if (excludeTypes.indexOf(this.fieldType.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}
}
