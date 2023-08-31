import { Component, EventEmitter, Input, OnChanges, Output, ViewEncapsulation } from '@angular/core';
import { FieldDisplayModel } from '../../../../models/fieldtype-api.model';

/*global $localize*/

@Component({
	selector: 'd3s-field-type-details',
	templateUrl: './field-type-details.component.html',
	styleUrls: ['./field-type-details.component.less'],
	encapsulation: ViewEncapsulation.None,
	host: {
		"(document:click)": "clickedOutside($event)",
	}
})
export class FieldTypeDetailsComponent implements OnChanges {
	@Input() fieldType: FieldDisplayModel;
	@Output() onEdit = new EventEmitter();
	@Output() close = new EventEmitter();

	ascendingLabel: string = $localize`Ascending`;
	descendingLabel: string = $localize`Descending`;
	currentType: string = '';

	ngOnChanges(): void {
		this.currentType = this.fieldType.FieldTypeValue;
	}

	editClick() {
		this.onEdit.emit();
	}

	hasPartOfKey() {
		const excludeTypes: string[] = ['Path', 'ComputedRelationshipField', 'Json', 'JsonElement', 'Link', 'ComputedOwnershipLookup', 'ComputedRelationshipReferenceList', 'ComputedRelationshipLookup', 'Relationship', 'Score', 'Tag'];
		if (excludeTypes.indexOf(this.fieldType.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}
	hasRequired() {
		const excludeTypes: string[] = ['Path', 'Counter', 'ComputedRelationshipField', 'Json', 'JsonElement', 'ComputedOwnershipLookup', 'ComputedRelationshipReferenceList', 'ComputedRelationshipLookup', 'Relationship', 'Score', 'Tag'];
		if (excludeTypes.indexOf(this.fieldType.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}
	hasDisplayInColumn() {
		const excludeTypes: string[] = ['Json', 'JsonElement', 'ComputedOwnershipLookup', 'ComputedRelationshipReferenceList', 'ComputedRelationshipLookup', 'Tag'];
		if (excludeTypes.indexOf(this.fieldType.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}
	hasShowIfEmpty() {
		const excludeTypes: string[] = ['Path', 'Tag'];
		if (excludeTypes.indexOf(this.fieldType.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}

	hasFormDescription() {
		const excludeTypes: string[] = ['Path', 'Counter', 'Json', 'JsonElement', 'ComputedOwnershipLookup', 'ComputedRelationshipReferenceList', 'ComputedRelationshipLookup', 'Score', 'Tag'];
		if (excludeTypes.indexOf(this.fieldType.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}

	hasIsListable() {
		const excludeTypes: string[] = ['ComputedRelationshipReferenceList', 'ComputedRelationshipLookup','Json'];
		if (excludeTypes.indexOf(this.fieldType.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}

	hasAllowMultipleItems() {
		const includeTypes: string[] = ['Lookup'];
		if (includeTypes.indexOf(this.fieldType.FieldTypeValue) > -1) {
			return true;
		}
		return false;
	}

	hasEditableOnUI() {
		const excludeTypes: string[] = ['Path', 'Counter', 'ComputedRelationshipField', 'Json', 'JsonElement', 'ComputedOwnershipLookup', 'ComputedRelationshipReferenceList', 'ComputedRelationshipLookup', 'Score', 'Tag'];
		if (excludeTypes.indexOf(this.fieldType.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}

	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	clickedOutside(event: any) {
		if (!(event.composedPath().filter((f) => f?.classList?.contains("secondary-side-panel")).length > 0)) {
			this.close.emit();
		}
	}

}
