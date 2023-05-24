import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges, ViewEncapsulation } from '@angular/core';
import { FormGroup, UntypedFormControl, Validators } from '@angular/forms';
import { SelectItemGroup } from 'primeng/api';
import { forkJoin, map, Observable, of, Subscription } from 'rxjs';
import { ComputedRelationshipLookupDefinition } from '../../../../../models/fieldtype-api.model';
import { FieldsObservableService } from '../../../../../services/fieldsObservable.service';
import { PopupMenuItem } from '../../../controls/popup-menu/popup-menu.component';
import { RelationshipTypeSelection } from './relation-lookup-form.models';

export class LookupField {
	name: string;
	idx: number;
	fieldNameControl: string;
	fieldDisplayNameControl: string;
	MenuItems: PopupMenuItem[] = [];
}

@Component({
	selector: 'd3s-relation-lookup-field-type-editor',
	templateUrl: './relation-lookup-field-type-editor.component.html',
	styleUrls: ['./relation-lookup-field-type-editor.component.less'],
	encapsulation: ViewEncapsulation.None,
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class RelationLookupFieldTypeEditorComponent implements OnChanges {
	@Input() uid: string;
	@Input() isVisible: boolean = false;
	@Input() assetTypeUid: string;
	@Input() fieldTypeForm: FormGroup = null;
	@Input() definition: ComputedRelationshipLookupDefinition = null;

	relationshipTypeSelection: RelationshipTypeSelection[] = []
	fieldOptions: SelectItemGroup[] = [];

	isLoading: boolean = false;

	lookupFields: LookupField[] = [];

	constructor(
		private fieldsService: FieldsObservableService,
		private cdRef: ChangeDetectorRef) {

	}

	ngOnChanges(changes: SimpleChanges) {
		if (changes.isVisible && changes.isVisible.currentValue !== changes.isVisible.previousValue) {
			this.relationshipTypeSelection = [];

			Object.keys(this.fieldTypeForm.controls)
				.filter((x) => x.startsWith('RelationLookup'))
				.forEach((key) => {
					this.fieldTypeForm.removeControl(key);
				});

			if (this.isVisible) {
				this.load();
			}
		}
	}

	validateComponents() {
		var relationshipTypePickers = Object.keys(this.fieldTypeForm.controls).filter((x) => x.startsWith("RelationLookup_Rel_"));
		relationshipTypePickers.slice(0, -1).forEach((key) => {
			this.fieldTypeForm.get(key).disable();
		});
	}

	load() {
		this.isLoading = true;
		if (this.definition) {
			this.loadDefinition();
		}
		else {
			this.fieldsService.getStandardRelations(this.assetTypeUid)
				.subscribe((res) => {
					const item: RelationshipTypeSelection = { index: 1, options: res, selected: null, cntrlName: 'RelationLookup_Rel_' + 1 };
					this.fieldTypeForm.addControl(item.cntrlName, new UntypedFormControl('', [Validators.required]));
					this.relationshipTypeSelection.push(item);
					this.isLoading = false;
					this.cdRef.markForCheck();
				})
		}
	}

	loadingHopDetailsInProgress: boolean = false;
	loadNewHop() {
		this.loadingHopDetailsInProgress = true;
		const lastRel = this.relationshipTypeSelection[this.relationshipTypeSelection.length - 1];
		this.fieldsService.getStandardRelations(lastRel.assetTypeUid)
			.subscribe((res) => {
				const item: RelationshipTypeSelection = { index: lastRel.index + 1, options: res, selected: null, cntrlName: 'RelationLookup_Rel_' + (lastRel.index + 1) };
				this.fieldTypeForm.addControl(item.cntrlName, new UntypedFormControl('', [Validators.required]));
				this.relationshipTypeSelection.push(item);
				this.validateComponents();
				this.loadingHopDetailsInProgress = false;
				this.cdRef.markForCheck();
			})
	}

	relationshipTypeSelected($event, item: RelationshipTypeSelection, callback: Function = null, addNew: boolean = true) {
		item.selected = $event.value;
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
		return this.fieldsService.getRelationLookupDisplayFields(item.assetTypeUid, item.relationshipTypeUid)
			.subscribe((fields) => {
				item.fieldOptions = fields;
				if (addNew) {
					this.addFormFieldForField();
				}
				this.setFieldsDropdown();

				if (callback) {
					callback();
				}
			});
	}

	setFieldsDropdown() {
		this.fieldOptions = [];
		this.relationshipTypeSelection.forEach((rel) => {
			if (rel.fieldOptions && rel.fieldOptions.length > 0) {
				rel.fieldOptions.filter((item) => (item.value as string).indexOf('|') === -1).forEach((item) => {
					item.value += "|" + rel.index;
				})
				this.fieldOptions.push({ items: rel.fieldOptions, label: $localize`Relationship Type ` + rel.index, value: 'RelationLookup_Rel_' + rel.index })
			}
		});
	}

	addFormFieldForField(name = '', displayOverrideName = '') {
		const fieldIndex = this.fieldTypeControls.length + 1;
		const fieldNameControl = 'RelationLookupField_Name_' + fieldIndex;
		const fieldDisplayNameControl = 'RelationLookupField_DisplayName_' + fieldIndex;
		this.lookupFields.push(
			{ idx: fieldIndex, name: 'Name', fieldNameControl, fieldDisplayNameControl, MenuItems: [] }
		);
		this.fieldTypeForm.addControl(fieldNameControl, new UntypedFormControl(name, [Validators.required]));
		this.fieldTypeForm.addControl(fieldDisplayNameControl, new UntypedFormControl(displayOverrideName));
		this.updateMenuItems();
		this.cdRef.markForCheck();
	}

	get addRelationshipHopEnabled(): boolean {
		return !this.loadingHopDetailsInProgress && (this.relationshipTypeSelection[this.relationshipTypeSelection.length - 1]?.selected ?? '').length > 0;
	}

	get fieldTypeControls(): string[] {
		if (!this.fieldTypeForm) {
			return [];
		}
		return Object.keys(this.fieldTypeForm.controls).filter((x) => x.startsWith('RelationLookupField_Name_'));
	}

	getDefinition(): ComputedRelationshipLookupDefinition {
		var definition = new ComputedRelationshipLookupDefinition();
		definition.Fields = [];
		definition.Relations = [];

		this.relationshipTypeSelection.forEach((rel) => {
			definition.Relations.push({
				AssetTypeUid: rel.assetTypeUid,
				IntersectTypeUid: rel.relationshipTypeUid,
				RelationType: null,
				Direction: this.resolveDirectionString(rel.direction)
			});
		});

		for (let i = 1; i <= this.lookupFields.length; i++) {
			const lookupField = this.lookupFields[i - 1];

			if (!this.fieldTypeForm.controls[lookupField.fieldNameControl] || !this.fieldTypeForm.controls[lookupField.fieldDisplayNameControl]) {
				continue;
			}
			const fieldValue = this.fieldTypeForm.get(lookupField.fieldNameControl).value;
			const overrideDisplayName = this.fieldTypeForm.get(lookupField.fieldDisplayNameControl).value;
			if (!fieldValue) {
				continue;
			}

			const parsedFieldValue = (fieldValue as string).split('|');
			if (parsedFieldValue.length < 2) {
				continue;
			}
			const fieldName = parsedFieldValue[0];
			const relationIndex = +parsedFieldValue[1] - 1;
			const rel = this.relationshipTypeSelection[relationIndex];

			definition.Fields.push({
				AssetTypeUid: rel.assetTypeUid,
				FieldTypeName: fieldName,
				DisplayOrder: i,
				OverrideDisplayName: overrideDisplayName,
				Filter: null,
				RelationIndex: relationIndex,
				Show: true,
				SortOrder: null,
				Width: null
			});
		}

		return definition;
	}

	loadDefinition() {
		let relationshipTypesObservables: Observable<any>[] = new Array();

		for (let i = 0; i < this.definition.Relations.length; i++) {
			const assetTypeUid = i === 0 ? this.assetTypeUid : this.definition.Relations[i - 1].AssetTypeUid;
			const relItem = this.definition.Relations[i];

			var obs = this.fieldsService.getStandardRelations(assetTypeUid).pipe(map((res) => { return { item: relItem, result: res } }));
			relationshipTypesObservables.push(obs);
		}

		forkJoin(relationshipTypesObservables).subscribe((data) => {
			let idx = 1;
			data.forEach((result) => {
				const res = result.result;
				const x = result.item;

				const item: RelationshipTypeSelection = { index: idx, options: res, selected: null, cntrlName: 'RelationLookup_Rel_' + idx };
				const value = `${x.IntersectTypeUid}|${x.AssetTypeUid}|${this.resolveDirectionId(x.Direction)}`.toUpperCase();

				const isLast = data.indexOf(result) === data.length - 1;

				this.relationshipTypeSelected({ value: value }, item, isLast ? this.loadDefinitionFields.bind(this) : null, false);

				this.fieldTypeForm.addControl(item.cntrlName, new UntypedFormControl(value, [Validators.required]));
				this.relationshipTypeSelection.push(item);
				this.cdRef.markForCheck();
				idx++;
			});
		});
	}
	loadDefinitionFields() {
		this.definition.Fields.forEach((field) => {
			this.addFormFieldForField(`${field.FieldTypeName}|${field.RelationIndex + 1}`, field.OverrideDisplayName);
		});
		this.validateComponents();
		this.isLoading = false;
	};

	resolveDirectionString(id: number): string {
		switch (id) {
			case 1: return 'Back';
			case 2: return 'Forward';
			case 3: return 'Both';
		}
		return null;
	}

	resolveDirectionId(name: string): number {
		switch (name) {
			case 'Back': return 1;
			case 'Forward': return 2;
			case 'Both': return 3;
		}
		return null;
	}

	removeRelationshipHop($event) {
		console.log($event);
	}

	private updateMenuItems() {
		let position = 0;
		// ignore complexity codacy issue
		// eslint-disable-next-line
		this.lookupFields.forEach((item) => {
			position++;
			const menuItems = [];

			if (this.lookupFields.length > 1) {

				menuItems.push({ title: $localize`Delete`, action: 'delete' });
				let positionDisabled = false;
				let positionTooltip = '';

				if (position !== 1) {
					menuItems.push({ title: $localize`Move To Top`, disabled: positionDisabled, tooltip: positionTooltip, action: 'movetop' });
					menuItems.push({ title: $localize`Move Up`, disabled: positionDisabled, tooltip: positionTooltip, action: 'moveup' });
				}
				if (position !== this.lookupFields.length) {
					menuItems.push({ title: $localize`Move Down`, disabled: positionDisabled, tooltip: positionTooltip, action: 'movedown' });
					menuItems.push({ title: $localize`Move To Bottom`, disabled: positionDisabled, tooltip: positionTooltip, action: 'movebottom' });
				}
			}

			item.MenuItems = menuItems;
		});

		this.cdRef.markForCheck();
	}
}



