import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, SimpleChanges, ViewChild, ViewEncapsulation } from '@angular/core';
import { FormGroup, UntypedFormControl, Validators } from '@angular/forms';
import { SelectItemGroup } from 'primeng/api';
import { forkJoin, map, Observable } from 'rxjs';
import { ComputedRelationshipLookupDefinition } from '../../../../../models/fieldtype-api.model';
import { FieldsObservableService } from '../../../../../services/fieldsObservable.service';
import { StringHelpers } from '../../../../../static/string-helpers';
import { AdvancedFilteringComponent } from '../../../../assets-grid/advanced-filtering/advanced-filtering.component';
import { AdvancedFilterFieldCondition, AdvancedFilterFieldType } from '../../../../assets-grid/advanced-filtering/advanced-filtering.models';
import { PopupMenuItem } from '../../../controls/popup-menu/popup-menu.component';
import { DatePipe } from "@angular/common";
import { SelectItem } from '../../../../../models/form.model';
import { OperatorString } from '../../../../../models/operator.model';
import { ReuseInterceptor } from '../../../../../http-interceptors/reuse.interceptor';
import { RelationshipTypeSelection } from './relation-lookup-form.models';

/*global $localize*/

export class LookupField {
	idx: number;
	fieldNameControl: string;
	fieldDisplayNameControl: string;
	MenuItems: PopupMenuItem[] = [];
	filterValue?: string;
}

export class SortField {
	idx: number;
	fieldControl: string;
	fieldDirectionControl: string;
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

	fieldOptionsForFields: SelectItemGroup[] = [];
	fieldOptionsForSort: SelectItemGroup[] = [];

	isLoading: boolean = false;
	isFieldsLoading: boolean = false;
	isTableSettingsOpen: boolean = true;

	lookupFields: LookupField[] = [];
	sortFields: SortField[] = [];
	advancedFilterFieldTypes: AdvancedFilterFieldType[] = [];
	
	readonly relationshipFormNamePrefix: string = 'RelationLookup_Rel_';
	readonly sortOrderControlPrefix: string = 'RelationLookup_SortOrder_';
	lastSortIdx: number = 0;
	@ViewChild('advancedFilter', { static: false }) advancedFilter: AdvancedFilteringComponent;

	// ignore usage of any keyword
	// eslint-disable-next-line
	intervalHandle: any;

	constructor(
		private fieldsService: FieldsObservableService,
		private cdRef: ChangeDetectorRef,
		private datePipe: DatePipe,
		private reuseInterceptor: ReuseInterceptor) {
	}

	ngOnChanges(changes: SimpleChanges) {
		if (changes.isVisible && changes.isVisible.currentValue !== changes.isVisible.previousValue) {
			this.resetForm();

			if (this.isVisible) {
				this.load();
			}
		}
	}

	resetForm() {
		this.relationshipTypeSelection = [];
		this.lookupFields = [];
		this.sortFields = [];
		this.reuseInterceptor.forceRefresh();

		if (this.intervalHandle) {
			clearInterval(this.intervalHandle);
		}

		Object.keys(this.fieldTypeForm.controls)
			.filter((x) => x.startsWith('RelationLookup'))
			.forEach((key) => {
				this.fieldTypeForm.removeControl(key);
			});
	}

	validateComponents() {
		const relationshipTypePickers = Object.keys(this.fieldTypeForm.controls).filter((x) => x.startsWith(this.relationshipFormNamePrefix));
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
			this.addNewSortIfNoExists();

			this.fieldsService.getStandardRelations(this.assetTypeUid)
				.subscribe((res) => {
					const item: RelationshipTypeSelection = { index: 1, options: res, selected: null, cntrlName: this.relationshipFormNamePrefix + "1" };
					this.fieldTypeForm.addControl(item.cntrlName, new UntypedFormControl('', [Validators.required]));
					this.relationshipTypeSelection.push(item);
					this.isLoading = false;
					this.cdRef.markForCheck();
				});
		}

		this.intervalHandle = setInterval(() => {
			this.setDropdowns();
		}, 250);
	}

	setDropdowns() {
		if (!this.isVisible || this.isLoading) {
			return;
		}
		const selectedFields: string[] = [];
		Object.keys(this.fieldTypeForm.controls)
			.filter((ctrl) => ctrl.startsWith('RelationLookupField_Name'))
			.forEach((key) => {
				if (this.fieldTypeForm.get(key).value) {
					selectedFields.push(this.fieldTypeForm.get(key).value);
				}
			});

		this.fieldOptionsForFields = JSON.parse(JSON.stringify(this.fieldOptions));
		this.fieldOptionsForFields.forEach((grp) => {
			grp.items.forEach((item) => {
				if (selectedFields.some((x) => x.toLowerCase() === item.value.toLowerCase())) {
					item.disabled = true;
				}
			});
		});

		const selectedSortFields: string[] = [];

		this.sortFields.forEach((x) => {
			if (this.fieldTypeForm.get(x.fieldControl).value) {
				selectedSortFields.push(this.fieldTypeForm.get(x.fieldControl).value);
			}
		});

		this.fieldOptionsForSort = JSON.parse(JSON.stringify(this.fieldOptions));
		this.fieldOptionsForSort.forEach((grp) => {
			grp.items = grp.items.filter((x) => selectedFields.some((f) => f.toLowerCase() === x.value.toLowerCase()));

			grp.items.forEach((item) => {
				if (selectedSortFields.some((x) => x.toLowerCase() === item.value.toLowerCase())) {
					item.disabled = true;
				}
			});
		});
	}

	addNewSortFormElements(field: string = null, sortDirection: string = 'asc') {
		this.lastSortIdx = this.lastSortIdx + 1;
		const ctrlName = this.sortOrderControlPrefix + this.lastSortIdx;
		const ctrlDirection = this.sortOrderControlPrefix + this.lastSortIdx + "_Direction";
		this.sortFields.push({ idx: this.lastSortIdx + 1, fieldControl: ctrlName, fieldDirectionControl: ctrlDirection });
		this.fieldTypeForm.addControl(ctrlName, new UntypedFormControl(field));
		this.fieldTypeForm.addControl(ctrlDirection, new UntypedFormControl(sortDirection));
	}

	addNewSortIfNoExists() {
		if (this.sortFields.length === 0 || this.areAllSortsSet) {
			this.addNewSortFormElements();
		}
	}

	get areAllSortsSet(): boolean {
		let allSet = true;
		Object.keys(this.fieldTypeForm.controls)
			.filter((key) => key.startsWith(this.sortOrderControlPrefix))
			.forEach((key) => {
				if (!this.fieldTypeForm.get(key).value) {
					allSet = false;
				}
			});

		return allSet;
	}

	sortChanged() {
		this.addNewSortIfNoExists();
	}

	removeSortOption(item: SortField) {
		this.sortFields.splice(this.sortFields.indexOf(item), 1);
		this.fieldTypeForm.removeControl(item.fieldControl);
		this.fieldTypeForm.removeControl(item.fieldDirectionControl);

		this.addNewSortIfNoExists();
	}

	loadingHopDetailsInProgress: boolean = false;
	loadNewHop() {
		this.loadingHopDetailsInProgress = true;
		const lastRel = this.relationshipTypeSelection[this.relationshipTypeSelection.length - 1];
		this.fieldsService.getStandardRelations(lastRel?.assetTypeUid ?? this.assetTypeUid)
			.subscribe((res) => {
				const idx = lastRel ? lastRel.index + 1 : 1;
				const item: RelationshipTypeSelection = { index: idx, options: res, selected: null, cntrlName: this.relationshipFormNamePrefix + idx };
				this.fieldTypeForm.addControl(item.cntrlName, new UntypedFormControl('', [Validators.required]));
				this.relationshipTypeSelection.push(item);
				this.validateComponents();
				this.loadingHopDetailsInProgress = false;
				this.cdRef.markForCheck();
			});
	}
	// ignore Function usage error
	// eslint-disable-next-line
	relationshipTypeSelected($event, item: RelationshipTypeSelection, callback: Function = null, addNew: boolean = true) {
		item.selected = $event.value;
		const values = item.selected.split('|');
		if (values.length === 3) {
			item.relationshipTypeUid = values[0];
			item.assetTypeUid = values[1];
			item.direction = +values[2];
		}
		else {
			item.relationshipTypeUid = item.assetTypeUid = item.direction = null;
		}
		this.isFieldsLoading = true;
		item.fieldOptions = [];
		return this.fieldsService.getRelationLookupDisplayFields(item.assetTypeUid, item.relationshipTypeUid)
			.subscribe((fields) => {
				item.fieldOptions = JSON.parse(JSON.stringify(fields));
				if (addNew) {
					this.addFormFieldForField();
				}
				this.setFieldsDropdown();
				item.valuesResolved = true;
				const allItemsResolved: boolean = this.relationshipTypeSelection.filter((x) => x.valuesResolved).length === this.relationshipTypeSelection.length;
				if (callback && allItemsResolved) {
					callback();
				}
				this.isFieldsLoading = false;
			});
	}

	setFieldsDropdown() {
		this.fieldOptions = [];

		this.relationshipTypeSelection.forEach((rel) => {
			if (rel.fieldOptions && rel.fieldOptions.length > 0) {
				rel.fieldOptions.filter((item) => (item.value as string).indexOf('|') === -1).forEach((item) => {
					item.value += "|" + rel.index;
				})
				this.fieldOptions.push({ items: rel.fieldOptions, label: $localize`Relationship Type ` + rel.index, value: this.relationshipFormNamePrefix + rel.index });
			}
		});


		//advanced filtering filters

		this.advancedFilterFieldTypes = [];

		let relationIndex: number = 1;
		this.fieldOptions.forEach((grp) => {
			grp.items.forEach((itm) => {
				if (itm["fieldType"]) {
					const ft = itm["fieldType"];
					const _rawType = ft.Type;
					const typeName = Object.keys(_rawType)[0];

					if (ft["Id"]) {
						ft['Name'] = `H${relationIndex}_${ft['Id']}`;
					}
					else {
						ft['Name'] = `H${relationIndex}_${ft['Name']}`;
					}
					_rawType[`${typeName}`]["IsPrimaryFilter"] = false;
					this.advancedFilterFieldTypes.push({
						Category: grp.label,
						FriendlyName: ft['FriendlyName'],
						Name: ft['Name'],
						Type: _rawType
					})
				}
			});
			relationIndex++;
		});

		setTimeout(() => {
			if (this.advancedFilter) {
				const filters = this.getExistingFilters();
				this.advancedFilter.clearFilters();
				this.advancedFilter.initializeData(true, this.advancedFilterFieldTypes, filters);
			}
		}, 300);
	}

	// ignore complexity codacy issue
	// eslint-disable-next-line
	getExistingFilters(): AdvancedFilterFieldCondition[] {
		const res: AdvancedFilterFieldCondition[] = [];
		if (this.definition && this.definition.Filters) {
			const regexp = /\(.*?\)/g;
			const matches = this.definition.Filters.match(regexp);
			for (const match of matches) {
				const expression = StringHelpers.trimChar(StringHelpers.trimChar(match.trim(), ')'), '(');
				const filter = expression.split(' ').map((x) => x.trim());
				const value = StringHelpers.trimChar(filter[2], `'`);
				const newfilter = new AdvancedFilterFieldCondition(this.datePipe);
				newfilter.connectingOperator = 'and';
				newfilter.field = filter[0] ?? '';

				const ft = this.advancedFilterFieldTypes.find((x) => x.Name === newfilter.field);
				newfilter.fieldType = Object.keys(ft.Type)[0];
				newfilter.friendlyFieldName = ft.FriendlyName;
				newfilter.isRelationship = false;
				newfilter.markForDeletion = false;
				newfilter.relationshipFieldName = ``;
				newfilter.operator = StringHelpers.getOperatorFromString(filter[1], filter[2]);

				if (newfilter.operator === OperatorString.Contains && value.endsWith('*')) {
					newfilter.operator = OperatorString.StartsWith;
				}

				if (newfilter.operator === OperatorString.Contains && value.startsWith('*')) {
					newfilter.operator = OperatorString.EndsWith;
				}

				newfilter.type = ft;
				newfilter.isDefaultFilter = false;
				newfilter.isPreloaded = true;
				newfilter.isConfirmed = true;

				const doNotPopulateValue: OperatorString[] = [OperatorString.IsTrue, OperatorString.IsFalse, OperatorString.Populated, OperatorString.Populated];

				if (!doNotPopulateValue.some((x) => x === newfilter.operator)) {
					if (value && newfilter.type && (newfilter.type.Type.Date || newfilter.type.Type.DateTime)) {
						newfilter.value = new Date(value);
					}
					else {
						newfilter.value = value;
					}
				}

				//if (filter.value2 && newfilter.type && (newfilter.type.Type.Date || filter.type.Type.DateTime)) {
				//	newfilter.value2 = new Date(filter.value2);
				//}
				//else {
				//	newfilter.value2 = filter.value2;
				//}
				res.push(newfilter);
			}
		}
		return res;
	}

	addFormFieldForField(name = '', displayOverrideName = '') {
		const fieldIndex = this.fieldTypeControls.length + 1;
		const fieldNameControl = 'RelationLookupField_Name_' + fieldIndex;
		const fieldDisplayNameControl = 'RelationLookupField_DisplayName_' + fieldIndex;
		this.lookupFields.push(
			{ idx: fieldIndex, fieldNameControl, fieldDisplayNameControl, MenuItems: [] }
		);
		this.fieldTypeForm.addControl(fieldNameControl, new UntypedFormControl(name, [Validators.required]));
		this.fieldTypeForm.addControl(fieldDisplayNameControl, new UntypedFormControl(displayOverrideName));
		this.updateMenuItems();
		this.cdRef.markForCheck();
	}

	get addRelationshipHopEnabled(): boolean {
		const lastIdx = this.relationshipTypeSelection.length;
		if (lastIdx <= 0) {
			return true;
		}

		return !this.loadingHopDetailsInProgress && this.fieldTypeForm.get(this.relationshipFormNamePrefix + lastIdx).value;
	}

	get fieldTypeControls(): string[] {
		if (!this.fieldTypeForm) {
			return [];
		}
		return Object.keys(this.fieldTypeForm.controls).filter((x) => x.startsWith('RelationLookupField_Name_'));
	}

	// ignore complexity codacy issue
	// eslint-disable-next-line
	getDefinition(): ComputedRelationshipLookupDefinition {
		const definition = new ComputedRelationshipLookupDefinition();
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
			const rel = this.relationshipTypeSelection[`${relationIndex}`];

			//get sorts
			const sortKey = Object.keys(this.fieldTypeForm.controls)
				.filter((key) => key.startsWith(this.sortOrderControlPrefix))
				.find((key) => (this.fieldTypeForm.get(key).value as string) === fieldValue);

			let sortOrder: number = null;
			let sortByAscending: boolean = null;

			if (sortKey) {
				const sortItem = this.sortFields.find((x) => x.fieldControl === sortKey);
				sortOrder = this.sortFields.indexOf(sortItem) + 1;
				sortByAscending = (this.fieldTypeForm.get(sortItem.fieldDirectionControl).value as string) !== 'desc';
			}

			definition.Fields.push({
				AssetTypeUid: rel.assetTypeUid,
				FieldTypeName: fieldName,
				DisplayOrder: i,
				OverrideDisplayName: overrideDisplayName,
				Filter: null,
				RelationIndex: relationIndex,
				Show: true,
				SortOrder: sortOrder,
				Width: null,
				SortByAscending: sortByAscending
			});
		}

		//handle filter fields
		if (this.filters && this.filters.length > 0) {
			this.filters.forEach((ft) => {
				const apiFieldName = ft.field;
				let selection: SelectItem = null;

				this.fieldOptions.forEach((grp) => {
					grp.items.forEach((itm) => {
						if (itm['fieldType']?.Name === apiFieldName) {
							selection = itm as SelectItem;
						}
					});
				})

				if (selection) {
					const fieldName: string = selection.value.split('|')[0];
					const relIdx: number = +selection.value.split('|')[1] - 1;

					if (!definition.Fields.some((x) => x.RelationIndex === relIdx && x.FieldTypeName === fieldName)) {
						const _rel = this.relationshipTypeSelection[`${relIdx}`];

						definition.Fields.push({
							AssetTypeUid: _rel.assetTypeUid,
							FieldTypeName: fieldName,
							DisplayOrder: null,
							OverrideDisplayName: null,
							Filter: null,
							RelationIndex: relIdx,
							Show: false,
							SortOrder: null,
							Width: null,
							SortByAscending: null
						});
					}
				}
			});
		}

		definition.Filters = this.filter;

		return definition;
	}

	loadDefinition() {

		// ignore any type
		// eslint-disable-next-line
		const relationshipTypesObservables: Observable<any>[] = new Array();

		for (let i = 0; i < this.definition.Relations.length; i++) {
			const assetTypeUid = i === 0 ? this.assetTypeUid : this.definition.Relations[i - 1].AssetTypeUid;
			const relItem = this.definition.Relations[`${i}`];

			const obs = this.fieldsService.getStandardRelations(assetTypeUid).pipe(map((res) => { return { item: relItem, result: res }; }));
			relationshipTypesObservables.push(obs);
		}

		forkJoin(relationshipTypesObservables).subscribe((data) => {
			let idx = 1;
			data.forEach((result) => {
				const res = result.result;
				const x = result.item;

				const item: RelationshipTypeSelection = { index: idx, options: res, selected: null, cntrlName: this.relationshipFormNamePrefix + idx };
				const value = `${x.IntersectTypeUid}|${x.AssetTypeUid}|${this.resolveDirectionId(x.Direction)}`.toUpperCase();

				this.relationshipTypeSelection.push(item);

				this.relationshipTypeSelected({ value }, item, this.loadDefinitionFields.bind(this), false);

				this.fieldTypeForm.addControl(item.cntrlName, new UntypedFormControl(value, [Validators.required]));
				this.cdRef.markForCheck();
				idx++;
			});
		});
	}

	loadDefinitionFields() {
		setTimeout(() => {
			this.definition.Fields.filter((x) => x.Show === true).forEach((field) => {
				const fieldName = `${field.FieldTypeName}|${field.RelationIndex + 1}`;
				if (this.getFieldFromFieldOptions(fieldName)) {
					this.addFormFieldForField(fieldName, field.OverrideDisplayName);
				}
			});

			this.setDropdowns();
			const sortFields = this.definition.Fields.filter((x) => x.SortOrder !== null && x.SortOrder !== 0).sort((a, b) => a.SortOrder > b.SortOrder ? 1 : -1);

			sortFields.forEach((field) => {
				this.addNewSortFormElements(`${field.FieldTypeName}|${field.RelationIndex + 1}`, field.SortByAscending ? 'asc' : 'desc');
			});

			this.addNewSortIfNoExists();
			this.validateComponents();
			this.isLoading = false;
			this.cdRef.markForCheck();
		}, 200);
	}

	getFieldFromFieldOptions(fieldName: string) {
		let ret = null;
		this.fieldOptions.forEach((grp) => {
			grp.items.forEach((item) => {
				if (item.value === fieldName) {
					ret = item;
				}
			});
		});

		return ret;
	}

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
		const ctrlName: string = $event as string;
		const relIndex: number = +ctrlName.replace(this.relationshipFormNamePrefix, '');
		this.lookupFields.forEach((item) => {
			const value = (this.fieldTypeForm.get(item.fieldNameControl).value ?? '') as string;
			if (value.endsWith('|' + relIndex)) {
				this.deleteField(item);
			}
		})
		this.fieldTypeForm.removeControl(ctrlName);

		//we can only remove last relationship type so we can sefely remove just last elements here
		this.relationshipTypeSelection.splice(this.relationshipTypeSelection.length - 1, 1);
		this.fieldOptions.splice(this.fieldOptions.length - 1, 1);
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
				const positionDisabled = false;
				const positionTooltip = '';

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

	// ignore complexity codacy issue
	// eslint-disable-next-line
	onMenuItemSelect(item: LookupField, $event) {
		switch ($event.action) {
			case 'delete':
				this.deleteField(item);
				break;
			case 'movetop':
				this.moveToTop(item);
				break;
			case 'moveup':
				this.moveUp(item);
				break;
			case 'movedown':
				this.moveDown(item);
				break;
			case 'movebottom':
				this.moveToLast(item);
				break;
		}
	}

	deleteField(item: LookupField) {
		const idx = this.lookupFields.indexOf(item);
		this.fieldTypeForm.removeControl(item.fieldNameControl);
		this.fieldTypeForm.removeControl(item.fieldDisplayNameControl);
		this.lookupFields.splice(idx, 1);
	}

	moveToTop(field: LookupField) {
		const idx = this.lookupFields.indexOf(field);
		const newIdx = 0;
		this.updateArrayPosition(this.lookupFields, idx, newIdx);
	}

	moveToLast(field: LookupField) {
		const idx = this.lookupFields.indexOf(field);
		const newIdx = this.lookupFields.length - 1;
		this.updateArrayPosition(this.lookupFields, idx, newIdx);
	}

	moveUp(field: LookupField) {
		const idx = this.lookupFields.indexOf(field);
		const newIdx = idx - 1;
		this.updateArrayPosition(this.lookupFields, idx, newIdx);
	}

	moveDown(field: LookupField) {
		const idx = this.lookupFields.indexOf(field);
		const newIdx = idx + 1;
		this.updateArrayPosition(this.lookupFields, idx, newIdx);
	}

	updateArrayPosition(arr, fromIndex, toIndex) {
		const element = arr[`${fromIndex}`];
		arr.splice(fromIndex, 1);
		arr.splice(toIndex, 0, element);
	}

	filters: AdvancedFilterFieldCondition[] = [];
	filter: string = null;
	advancedFiltersChanged($event) {
		this.lookupFields.forEach((f) => {
			f.filterValue = null;
		});
		if ($event && $event.data) {
			this.filters = $event.data;
		}

		this.filter = $event.filter;
	}
}



