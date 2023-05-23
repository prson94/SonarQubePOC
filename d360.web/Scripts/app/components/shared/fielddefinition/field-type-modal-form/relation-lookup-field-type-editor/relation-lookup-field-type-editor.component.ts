import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnInit, ViewEncapsulation } from '@angular/core';
import { FormGroup, UntypedFormControl, Validators } from '@angular/forms';
import { SelectItemGroup } from 'primeng/api';
import { FieldsObservableService } from '../../../../../services/fieldsObservable.service';
import { RelationshipTypeSelection } from './relation-lookup-form.models';

@Component({
	selector: 'd3s-relation-lookup-field-type-editor',
	templateUrl: './relation-lookup-field-type-editor.component.html',
	styleUrls: ['./relation-lookup-field-type-editor.component.less'],
	encapsulation: ViewEncapsulation.None,
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class RelationLookupFieldTypeEditorComponent implements OnInit {
	@Input() uid: string;
	@Input() assetTypeUid: string;
	@Input() fieldTypeForm: FormGroup = null;

	relationshipTypeSelection: RelationshipTypeSelection[] = []
	fieldOptions: SelectItemGroup[] = [];

	constructor(
		private fieldsService: FieldsObservableService,
		private cdRef: ChangeDetectorRef) {
	}

	ngOnInit() {
		this.load();
	}

	load() {
		this.relationshipTypeSelection = [];
		this.fieldsService.getStandardRelations(this.assetTypeUid)
			.subscribe((res) => {
				const item: RelationshipTypeSelection = { index: 1, options: res, selected: null, cntrlName: 'RelationLookup_Rel_' + 1 };
				this.fieldTypeForm.addControl(item.cntrlName, new UntypedFormControl('', [Validators.required]));
				this.relationshipTypeSelection.push(item);
				this.cdRef.markForCheck();
			})
	}

	relationshipTypeSelected($event, item: RelationshipTypeSelection) {
		item.selected = $event.value;
		console.log($event);
		var values = item.selected.split('|');
		if (values.length === 3) {
			item.relationshipTypeUid = values[0];
			item.assetTypeUid = values[1];
			item.direction = +values[2];
		}
		else {
			item.relationshipTypeUid = item.assetTypeUid = item.direction = null;
		}

		item.fieldOptions = [];
		this.fieldsService.getRelationLookupDisplayFields(item.assetTypeUid, item.relationshipTypeUid)
			.subscribe((fields) => {
				item.fieldOptions = fields;
				this.addFormFieldForField();
				this.setFieldsDropdown();
			});
	}

	setFieldsDropdown() {
		this.fieldOptions = [];
		this.relationshipTypeSelection.forEach((rel) => {
			if (rel.fieldOptions.length > 0) {
				rel.fieldOptions.forEach((item) => {
					item.value += "|" + rel.index;
				})
				this.fieldOptions.push({ items: rel.fieldOptions, label: $localize`Relationship Type ` + rel.index, value: 'RelationLookup_Rel_' + rel.index })
			}
		});
	}

	addFormFieldForField() {
		const fieldIndex = this.fieldTypeControls.length + 1;
		this.fieldTypeForm.addControl('RelationLookupField_Name_' + fieldIndex, new UntypedFormControl('', [Validators.required]));
		this.fieldTypeForm.addControl('RelationLookupField_DisplayName_' + fieldIndex, new UntypedFormControl('', [Validators.required]));
		this.cdRef.markForCheck();
	}

	get addRelationshipHopEnabled(): boolean {
		return (this.relationshipTypeSelection[this.relationshipTypeSelection.length - 1]?.selected ?? '').length > 0;
	}

	get fieldTypeControls(): string[] {
		if (!this.fieldTypeForm) {
			return [];
		}
		return Object.keys(this.fieldTypeForm.controls).filter((x) => x.startsWith('RelationLookupField_Name_'));
	}

	getState() {
		Object.keys(this.fieldTypeForm.controls).filter((x) => x.startsWith('RelationLookup')).
			forEach((key) => {
				console.log(key + "==>" + this.fieldTypeForm.get(key).value);
			})

	}
}



