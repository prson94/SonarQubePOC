import { AfterViewChecked, ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, Input, OnChanges, OnInit, Output, QueryList, SimpleChange, ViewChild, ViewChildren, ViewEncapsulation } from "@angular/core";
import { AbstractControl, FormBuilder, FormGroup, ValidatorFn, Validators } from "@angular/forms";
import { SelectItem } from "primeng/api";
import { Table } from "primeng/table";
import { forkJoin, Observable, Subscription } from "rxjs";
import { AssetTypeClass, } from "../../../../models/asset.model";
import { AssetTypeAncestry } from "../../../../models/fields.model";
import { FieldType, FieldTypeAPIModel, FieldTypeAPIModelField } from "../../../../models/fieldtype-api.model";
import { AssetService } from "../../../../services/asset.service";
import { FieldsObservableService } from "../../../../services/fieldsObservable.service";
import { FormHelpers } from "../../../../static/form-helpers";
import { PropertyGroupComponent } from "../../../shared/controls/property-group/property-group.component";
import { D3SModal } from "../../../shared/modal/gov-modal.component";

export enum FormState {
	FieldTypeSelection = "FieldTypeSelection",
	Form = "Form"
}

/*global $localize*/

@Component({
	selector: "field-type-modal-form",
	templateUrl: './field-type-modal-form.component.html',
	styleUrls: ['field-type-modal-form.component.less'],
	encapsulation: ViewEncapsulation.None,
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfigurationFieldTypeModalFormComponent implements OnChanges, OnInit {
	@Input() isModalVisible: boolean = false;
	@Input() assetTypeName: string;
	@Input() assetTypeClass: AssetTypeClass;
	@Input() name: string;

	@Input() actionTypeUid: string;
	@Input() assetTypeUid: string;
	@Input() relationshipTypeUid: string;

	@Input() showShowInDetailTile: boolean = true;
	@Input() showDescription: boolean = true;
	@Input() hasDisplayInColumn: boolean = false;

	@Output() onClose = new EventEmitter();
	@Output() onUpdated = new EventEmitter();
	fieldTypeForm: FormGroup = null;
	fieldType: FieldTypeAPIModel;
	typeFieldTypes: FieldTypeAPIModelField[] = [];

	title = 'unset';
	subTitle = 'unset';

	isLoading = false;
	savingInProgress = false;
	formState: FormState = FormState.FieldTypeSelection;
	areFieldTypesLoaded: boolean = false;

	@ViewChild('modal', { static: false }) modal: D3SModal;
	@ViewChild('form', { static: false }) formElement: ElementRef;
	@ViewChild('dt', { static: false }) dt: Table;

	@ViewChildren(PropertyGroupComponent) propertyGroups: QueryList<PropertyGroupComponent>;


	private isEditFormUpdated: boolean = false;
	private changeFormSub: Subscription;
	fieldTypeSelection: SelectItem;
	selectedFieldType: string;


	fieldTypes: SelectItem[] = [];
	assetTypeAncestries: AssetTypeAncestry[] = [];
	selectedAssetPathListSegment: any;

	isInitialDataLoaded: boolean = false;
	numberOfAssetsForType: number = 0;

	get isEditing(): boolean {
		return this.name ? true : false;
	}

	get supportsPrimaryFilterOption(): boolean {
		return this.assetTypeUid ? true : false;
	}

	constructor(private fb: FormBuilder,
		private fieldsService: FieldsObservableService,
		private assetService: AssetService,
		private cdRef: ChangeDetectorRef
	) {

	}

	categoryTokens = [
		{
			"title": $localize`General`
		}
	]

	ngOnInit() {
		this.isLoading = true;

		forkJoin(
			this.fieldsService.getFieldsV2(this.assetTypeUid, this.actionTypeUid, this.relationshipTypeUid),
			this.fieldsService.getLookups(this.assetTypeUid, this.actionTypeUid, this.relationshipTypeUid),
			this.assetService.getAssetCountsByAssetTypeUid(this.assetTypeUid)
		).subscribe((results) => {
			if (results[0]) {
				this.typeFieldTypes = results[0];
				const categories = Array.from(new Set(this.typeFieldTypes.map((item) => item.Category)));

				this.categoryTokens = [];

				categories.filter((x) => x && x.length > 0).forEach((x) => {
					this.categoryTokens.push({
						title: x
					});
				});
				if (this.categoryTokens.length === 0) {
					this.categoryTokens.push({ title: $localize`General` });
				}
			}

			if (results[1]) {
				this.fieldTypes = results[1].DataTypes;
				this.setForm();
			}

			if (results[2].length > 0) {
				this.numberOfAssetsForType = +results[2].length + 1;
			}

			this.areFieldTypesLoaded = true;
			this.cdRef.markForCheck();
		});

		if (this.assetTypeUid) {
			this.fieldsService.getAssetTypeAncestry(this.assetTypeUid).subscribe((assetTypeAncestries: AssetTypeAncestry[]) => {
				this.assetTypeAncestries = assetTypeAncestries;
				this.cdRef.markForCheck();
			});
		}
	}

	ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		if (changes['isModalVisible']) {
			if (changes['isModalVisible'].previousValue !== changes['isModalVisible'].currentValue) { // object has changed 
				if (this.areFieldTypesLoaded) {
					this.updateForm();
				}
			}
		}
	}



	save() {
		this.savingInProgress = true;
		const model = new FieldTypeAPIModel();
		model.Action = "Merge";

		model.AssetTypeUid = this.assetTypeUid;
		model.RelationshipTypeUid = this.relationshipTypeUid;
		model.ActionTypeUid = this.actionTypeUid;
		model.Fields = [];
		model.Fields[0] = new FieldTypeAPIModelField();
		model.Fields[0].Type = new FieldType(this.selectedFieldType);
		const type = model.Fields[0].Type;
		model.Fields[0].FriendlyName = this.fieldTypeForm.get("FriendlyName").value;
		model.Fields[0].Name = this.fieldTypeForm.get("Name").value;
		model.Fields[0].Category = this.fieldTypeForm.get("Category").value ?? null;

		//Asset Path
		if (this.selectedFieldType === 'Path') {
			type.Path.Definition.AssetTypeUid = this.fieldTypeForm.get("AssetPathListSegment").value ?? null;
		}

		//Counter
		if (this.selectedFieldType === 'Counter') {
			type[this.selectedFieldType].CounterInitialIndex = this.fieldTypeForm.get("CounterInitialIndex").value ?? 1;
			type[this.selectedFieldType].CounterPrefix = this.fieldTypeForm.get("CounterPrefix").value ?? '';
		}

		type[this.selectedFieldType].Description.Display = this.fieldTypeForm.get("DisplayDescription").value ?? null;
		type[this.selectedFieldType].Description.Form = this.fieldTypeForm.get("FormDescription").value ?? null;

		type[this.selectedFieldType].Search.AddToResult = this.fieldTypeForm.get("AddToResult").value ?? false;
		type[this.selectedFieldType].IsDisplayable = this.fieldTypeForm.get("IsDisplayable").value ?? false;
		type[this.selectedFieldType].DisplayInColumn = this.fieldTypeForm.get("DisplayInColumn").value ?? false;
		type[this.selectedFieldType].IsEditable = this.fieldTypeForm.get("IsEditable").value ?? false;
		type[this.selectedFieldType].Validation.IsRequired = this.fieldTypeForm.get("IsRequired").value ?? false;
		type[this.selectedFieldType].IsPartOfKey = this.fieldTypeForm.get("IsPartOfKey").value ?? false;
		type[this.selectedFieldType].IsPrimaryFilter = this.fieldTypeForm.get("IsPrimaryFilter").value ?? false;
		type[this.selectedFieldType].AllowMultipleValues = this.fieldTypeForm.get("AllowMultipleValues").value ?? false;
		type[this.selectedFieldType].ShowIfEmpty = this.fieldTypeForm.get("ShowIfEmpty").value ?? false;

		type[this.selectedFieldType].DefaultValue = this.fieldTypeForm.get("DefaultValue").value ?? null;

		type[this.selectedFieldType].IsListable = this.fieldTypeForm.get("IsListable").value ?? false;
		if (type[this.selectedFieldType].IsListable) {
			type[this.selectedFieldType].ColumnWidth = this.fieldTypeForm.get("ColumnWidth").value ?? null;
			type[this.selectedFieldType].SortOrder = this.fieldTypeForm.get("SortOrder").value ?? null;
			type[this.selectedFieldType].SortByAscending = this.fieldTypeForm.get("SortByAscending").value === 'true' ? true : false;
		} else {
			type[this.selectedFieldType].ColumnWidth = null;
			type[this.selectedFieldType].SortOrder = null;
			type[this.selectedFieldType].SortByAscending = null;
		}

		if (false) {

			window.alert(JSON.stringify(model));
			this.savingInProgress = false;
		}
		else {
			let saveObs = this.fieldsService.putFieldsV2(model);

			saveObs.subscribe((res) => {
				if (res) {
					this.onUpdated.emit(res);
					this.close();
				}
				this.savingInProgress = false;
			});
		}

	}

	setForm() {
		this.fieldTypeForm = this.fb.group({
			FriendlyName: [null, { validators: [Validators.required, Validators.maxLength(250)] }],
			Name: [null, { validators: Validators.compose([Validators.required, this.apiNameValidator(), Validators.maxLength(250)]) }],
			Category: [null, { validators: Validators.maxLength(100) }],
			AssetPathListSegment: [null],
			DisplayDescription: [null],
			FormDescription: [null],
			IsDisplayable: [null],
			DisplayAsList: [null],
			DisplayInColumn: [null],
			AddToResult: [null],
			IsEditable: [null],
			IsListable: [null],
			IsRequired: [null],
			IsPartOfKey: [null],
			IsPrimaryFilter: [null],
			AllowMultipleValues: [null],
			ShowIfEmpty: [null],
			CounterPrefix: [null, { validators: [Validators.maxLength(10), Validators.pattern(/^[a-zA-Z0-9-_]*$/)] }],
			CounterInitialIndex: [null],
			ColumnWidth: [null],
			SortOrder: [null],
			SortByAscending: ['true'],
			DefaultValue: [null]
		});

		this.setDefaultFormValues();
		this.isLoading = false;
		this.cdRef.markForCheck();
	}

	setDefaultFormValues() {
		if (!this.fieldTypeForm) {
			return;
		}
		this.fieldTypeForm.reset();
		this.selectedAssetPathListSegment = null;

		switch (this.selectedFieldType) {
			case 'Counter':
				this.fieldTypeForm.controls["CounterInitialIndex"].setValue(this.numberOfAssetsForType);
				this.fieldTypeForm.controls["IsDisplayable"].setValue(true);
				this.fieldTypeForm.controls["ShowIfEmpty"].setValue(true);
				break;
			case 'Path':
				this.fieldTypeForm.controls["IsDisplayable"].setValue(true);
				this.fieldTypeForm.controls["ShowIfEmpty"].setValue(true);
				break;
			case 'Date':
				this.fieldTypeForm.controls["IsDisplayable"].setValue(true);
				this.fieldTypeForm.controls["IsEditable"].setValue(true);
				break;

			default: break;
		}
		this.cdRef.markForCheck();
	}

	updateForm() {
		this.subTitle = this.assetTypeName;

		if (this.isEditing) {
			this.isLoading = true;
			if (this.changeFormSub) {
				this.changeFormSub.unsubscribe();
			}

			this.fieldTypeForm.controls["Name"].disable();

			forkJoin(
				this.fieldsService.getFieldsV2(this.assetTypeUid, this.actionTypeUid, this.relationshipTypeUid, this.name)
			).subscribe((results) => {
				const fieldType = results[0][0];
				this.selectedFieldType = Object.keys(fieldType.Type)[0];

				this.fieldTypeSelection = this.fieldTypes.find((s) => s.value === this.selectedFieldType);
				this.confirmTypSelection();

				this.fieldTypeForm.controls["Name"].setValue(fieldType.Name);
				this.fieldTypeForm.controls["FriendlyName"].setValue(fieldType.FriendlyName);
				this.fieldTypeForm.controls["Category"].setValue(fieldType.Category);

				const type = fieldType.Type[this.selectedFieldType];

				this.fieldTypeForm.controls["DisplayDescription"].setValue(type?.Description?.Display ?? null);
				this.fieldTypeForm.controls["FormDescription"].setValue(type?.Description?.Form ?? null);

				this.fieldTypeForm.controls["AddToResult"].setValue(type?.Search?.AddToResult ?? null);
				this.fieldTypeForm.controls["IsDisplayable"].setValue(type?.IsDisplayable ?? null);
				this.fieldTypeForm.controls["DisplayInColumn"].setValue(type?.DisplayInColumn ?? null);
				this.fieldTypeForm.controls["IsEditable"].setValue(type?.IsEditable ?? null);
				this.fieldTypeForm.controls["IsListable"].setValue(type?.IsListable ?? null);
				this.fieldTypeForm.controls["IsRequired"].setValue(type?.Validation?.IsRequired ?? null);
				this.fieldTypeForm.controls["IsPartOfKey"].setValue(type?.IsPartOfKey ?? null);
				this.fieldTypeForm.controls["IsPrimaryFilter"].setValue(type?.IsPrimaryFilter ?? null);
				this.fieldTypeForm.controls["AllowMultipleValues"].setValue(type?.AllowMultipleValues ?? null);
				this.fieldTypeForm.controls["ShowIfEmpty"].setValue(type?.ShowIfEmpty ?? null);

				this.fieldTypeForm.controls["DefaultValue"].setValue(type?.DefaultValue ?? null);

				this.fieldTypeForm.controls["ColumnWidth"].setValue(type?.ColumnWidth ?? null);
				this.fieldTypeForm.controls["SortOrder"].setValue(type?.SortOrder ?? null);
				this.fieldTypeForm.controls["SortByAscending"].setValue((type?.SortByAscending ?? '').toString() ?? 'true');

				if (this.selectedFieldType === 'Path') {
					this.fieldTypeForm.controls["AssetPathListSegment"].setValue(type?.Definition?.AssetTypeUid ?? null);
					this.selectedAssetPathListSegment = this.fieldTypeForm.controls["AssetPathListSegment"].value;

					//asset path cannot be empty, so its always visible
					this.fieldTypeForm.controls["ShowIfEmpty"].setValue(true);
				}

				//Counter
				if (this.selectedFieldType === 'Counter') {
					this.fieldTypeForm.controls["CounterInitialIndex"].setValue(type?.CounterInitialIndex ?? this.numberOfAssetsForType);
					this.fieldTypeForm.controls["CounterPrefix"].setValue(type?.CounterPrefix ?? null);;
				}

				this.title = $localize`Edit Field`;

				this.isEditFormUpdated = false;
				setTimeout(() => {
					this.changeFormSub = this.fieldTypeForm.valueChanges.subscribe(() => {
						this.isEditFormUpdated = true;
					});
				}, 200);
				this.cdRef.markForCheck();
				this.isLoading = false;
			});
		}
		else {
			this.title = $localize`Add Field`;
			this.formState = FormState.FieldTypeSelection;
			this.fieldTypeSelection = null;
			this.selectedFieldType = '';

			this.setDefaultFormValues();
		}
	}

	get isFormDisabled(): boolean {
		return this.savingInProgress || this.fieldTypeForm.invalid || (this.name && !this.isEditFormUpdated);
	}

	get saveButtonLabel(): string {
		if (this.name) {
			return $localize`Save Changes`;
		}
		else {
			return $localize`Add Field Type`;
		}
	}

	get closeButtonLabel(): string {
		if (this.name && this.isEditFormUpdated) {
			return $localize`Discard Changes`;
		}

		return $localize`Cancel`;
	}

	close() {
		this.setDefaultFormValues();

		if (this.formElement) {
			this.formElement.nativeElement.scrollTop = 0;
		}

		this.onClose.emit();
	}

	lastVisitedTabIndex: number = 0;
	@HostListener('keydown.tab', ['$event'])
	onKeyDown(event: KeyboardEvent) {
		const target = event.target as HTMLElement;

		if (target.tabIndex > 9) {
			this.lastVisitedTabIndex = target.tabIndex;
			const nextInput = this.getNextInputTab(this.lastVisitedTabIndex);
			if (nextInput) {
				nextInput.focus();
			}
		}
	}

	getNextInputTab(idx: number): HTMLElement {
		const nextTabIndex = idx + 10;
		if (nextTabIndex > 250) {
			return null;
		}
		const nextElement = document.querySelectorAll(`[tabindex='${nextTabIndex}']`);
		if (nextElement.length > 0) {
			const parentOffset = (nextElement[0] as HTMLElement).offsetParent;
			if (parentOffset) {
				return nextElement[0] as HTMLElement;
			}
			else {
				return this.getNextInputTab(nextTabIndex);
			}
		}
		else {
			return this.getNextInputTab(nextTabIndex);
		}
	}

	confirmTypSelection() {
		this.formState = FormState.Form;
		this.subTitle = this.assetTypeName + " - " + this.fieldTypeSelection["label"];
		this.setDefaultFormValues();
		this.cdRef.markForCheck();
	}

	updateApiName(event) {
		if (this.isEditing) {
			return;
		}
		const nameValue: string = event.target.value.replace(/[^a-zA-Z0-9_]/g, '');
		this.fieldTypeForm.controls["Name"].setValue(nameValue);
		this.fieldTypeForm.controls["Name"].markAsDirty();
		this.cdRef.markForCheck();
	}

	updateCategoryName($event) {
		const newValue = `${$event.value}`;
		this.fieldTypeForm.controls["Category"].setValue(newValue);
	}

	onAssetPathListSegmentChange($event) {
		this.fieldTypeForm.controls["AssetPathListSegment"].setValue($event.value);
	}

	onShowDetailChange($event: boolean) {
		if (!$event) {
			this.fieldTypeForm.controls["DisplayInColumn"].setValue(false);
		}
	}

	isSettingDisabled(val: string) {
		if (!this.fieldTypeForm) {
			return;
		}
		const fieldName = this.fieldTypeForm.get("Name").value;

		if (this.objectType === 'TaskType') {
			if (fieldName === 'Name') { return true; }
			if ((fieldName === 'StepNo' || fieldName === 'GovernanceRole') && (val !== 'IsEditable' && val !== 'IsRequired' && val !== 'SearchAddToResult')) {
				return true;
			}
			var staticFields: string[] = [];
			staticFields.push('Name');
			staticFields.push('GovernanceRole');
			staticFields.push('StepNo');

			if (!staticFields.some((x) => x === fieldName)) {
				if (val === 'IsListable' || val === 'IsPartOfKey' || val === 'IsPrimaryFilter') { return true; }
			}
		}

		switch (val) {
			case 'IsDisplayable':
				return (['ComplexRelationLookup', 'RefListRelationship', 'System'].indexOf(this.selectedFieldType) > -1);
			case 'IsEditable':
				return (['ComplexRelationLookup', 'FieldFromRelationship', 'Json', 'JSON', 'JsonElement', 'OwnershipLookup', 'Path', 'RefListRelationship', 'Tag', 'Score', 'Counter', 'System'].indexOf(this.selectedFieldType) > -1);
			case 'IsListable':
				return (['ComplexRelationLookup', 'RefListRelationship', 'Json', 'JSON', 'System'].indexOf(this.selectedFieldType) > -1
					|| (this.selectedFieldType === 'Relationship' && !this.isListableRelationship));
			case 'IsRequired':
				return (['ComplexRelationLookup', 'FieldFromRelationship', 'Json', 'JSON', 'JsonElement', 'OwnershipLookup', 'Path', 'RefListRelationship', 'Relationship', 'Tag', 'Score', 'Counter', 'System'].indexOf(this.selectedFieldType) > -1);
			case 'IsPartOfKey':
				return (['ComplexRelationLookup', 'FieldFromRelationship', 'Json', 'JSON', 'JsonElement', 'OwnershipLookup', 'Path', 'RefListRelationship', 'Relationship', 'Tag', 'Score', 'Link', 'System']
					.indexOf(this.selectedFieldType) > -1
					|| this.selectedFieldType === 'ReferenceItemType');
			case 'IsPrimaryFilter':
				return (!this.supportsPrimaryFilterOption || ['FieldFromRelationship', 'ComplexRelationLookup', 'OwnershipLookup', 'Json', 'JSON', 'JsonElement', 'Path', 'RefListRelationship', 'System'].indexOf(this.selectedFieldType) > -1);
			case 'AllowMultipleValues':
				return (['Lookup'].indexOf(this.selectedFieldType) === -1);
			case 'ShowIfEmpty':
				return (['Path', 'Tag', 'System'].indexOf(this.selectedFieldType) > -1 || (this.selectedFieldType === 'Score' && !this.fieldTypeForm.get("IsDisplayable").value) || (this.objectType === 'ReferenceItemType' && fieldName.toLocaleLowerCase() === "code"));
			case 'SearchAddToResult':
				return (['Path', 'Html', 'Json', 'JSON', 'JsonElement', 'OwnershipLookup', 'ComplexRelationLookup', 'RefListRelationship', 'Score', 'Tag', 'System'].indexOf(this.selectedFieldType) > -1);
			case 'isSettingDisabled':
				return (['Json', 'JSON', 'JsonElement', 'ComplexRelationLookup', 'Tag', 'RefListRelationship', 'System'].indexOf(this.selectedFieldType) > -1);
			case 'DisplayInColumn':
				if (this.selectedFieldType === "OwnershipLookup") {
					const isDisabled = !this.fieldTypeForm.get("DisplayAsList").value;
					if (isDisabled) {
						this.fieldTypeForm.controls["DisplayInColumn"].setValue(false);
					}
					return isDisabled;
				}
				return false;
			default:
				console.warn(`invalid setting[${val}]passed to isSettingDisabled`);
		}
	}

	get objectType(): string {
		return "some object type";
	}

	get isListableRelationship(): boolean {
		return false;
	}

	apiNameValidator(): ValidatorFn {
		return (control: AbstractControl): { [key: string]: any } | null => {
			if (control.value == null || this.isEditing) {
				return {};
			}
			const existing = this.typeFieldTypes.map((x) => x.Name.toLocaleLowerCase());

			if (existing.indexOf((control.value as string).toLocaleLowerCase()) !== -1) {
				return {
					notUnique: { value: control.value }
				};
			}

			const restricted = ["id", "uid", "assetid", "assetuid", "assettypeid",
				"assettypeuid", "createdon", "updatedon", "parentdisplayname", "parentassetuid", "keypath", "displayvalue", "path"];

			if (this.relationshipTypeUid) {
				restricted.push("source");
			}

			if (this.assetTypeUid && this.assetTypeUid.toLocaleLowerCase() === '00000001-0000-0000-0000-A00000000011'.toLocaleLowerCase()) {
				//when type is user
				const user_restricted_fields = ["firstname", "lastname", "email", "status", "state", "resourceid", "resourceuri", "datelastloggedin", "lastloggedinon", "isadministrator"];
				restricted.push(...user_restricted_fields);
			}

			if (restricted.indexOf((control.value as string).toLocaleLowerCase()) !== -1) {
				return {
					restricted: { value: control.value }
				};
			}

			return null;
		};
	}

	private setDefaultValuesDeprecated() {
		const observables: Array<Observable<any>> = [];
		this.showDescription = true;
		//this.enableAllowMultipleValues = true;
		this.hasDisplayInColumn = true;
		//this.showIsRequired = true;

		switch (this.selectedFieldType.toLowerCase()) {
			case 'lookup':
				//if (this.model.FieldType.Type[this.selectedFieldType].List && this.model.FieldType.Type[this.selectedFieldType].List.Uid) {
				//	observables.push(this.lookupTypeSelected(this.model.FieldType.Type[this.selectedFieldType].List.Uid));
				//	this.model.FieldType.Type['Lookup'].AllowMultipleValues = this.model.FieldType.Type['Lookup'].List.AllowMultipleValues;
				//}
				//else if (this.model.FieldType.Type[this.selectedFieldType].List && this.model.FieldType.Type['Lookup'].List.Class && !this.model.FieldType.Type[this.selectedFieldType].List.Uid) {
				//	const valToPass = this.model.FieldType.Type['Lookup'].List.Class === 'Reference' ? 'ReferenceItemType' : 'TaxonomyType';
				//	this.model.FieldType.Type['Lookup'].AllowMultipleValues = this.model.FieldType.Type['Lookup'].List.AllowMultipleValues;
				//	observables.push(this.lookupTypeSelected(valToPass));
				//}
				//else {
				//	this.model.FieldType.Type[this.selectedFieldType].List.Uid = this.lookups.Lookups[0].value;
				//	observables.push(this.lookupTypeSelected(this.lookups.Lookups[0].value));
				//	this.model.FieldType.Type['Lookup'].AllowMultipleValues = this.model.FieldType.Type['Lookup'].List.AllowMultipleValues;
				//}
				break;
			case 'relationship':
				//try {
				//	if (this.model.FieldType.Type["Relationship"].IntersectTypeUid) {
				//		observables.push(this.cardinalRelationshipSelected(`${this.model.FieldType.Type["Relationship"].IntersectTypeUid}|${this.model.FieldType.Type["Relationship"].IsSubject}`));
				//	}
				//	if (!this.model.FieldType.Type["Relationship"].IsEditable) {
				//		this.showDescription = false;
				//		this.model.FieldType.Type["Relationship"].Description.Form = "";
				//	}
				//} catch (e) {
				//	console.log(e);
				//}
				break;
			case 'fieldfromrelationship':
			case 'computedrelationshipfield':
				//try {
				//	if (this.model.FieldType.Type["FieldFromRelationship"].IntersectTypeUid) {
				//		observables.push(this.cardinalFieldFromRelationshipSelected(this.model.FieldType.Type["FieldFromRelationship"].IntersectTypeUid, this.model.FieldType.Type["FieldFromRelationship"].FieldTypeName));
				//	} else if (this.lookups.Field_CardinalRelationships.length > 0) {
				//		observables.push(this.cardinalFieldFromRelationshipSelected(this.lookups.Field_FieldFromRelRelationships[0].value,
				//			this.model.FieldType.Type["FieldFromRelationship"].FieldTypeName));
				//	}
				//	this.model.FieldType.Type.FieldFromRelationship.IsEditable = false;
				//	this.showDescription = false;
				//} catch (e) {
				//	console.log(e);
				//}
				break;
			case 'reflistrelationship':
				//this.hasDisplayInColumn = false;
				//try {
				//	if (this.model.cardinalRelationship && (this.lookups.Field_CardinalReferenceRelationships.length > 0)
				//		&& (this.lookups.Field_CardinalReferenceRelationships.find((x) => x.value === this.model.cardinalRelationship))) {
				//		observables.push(this.cardinalFieldFromRelationshipSelected(this.model.cardinalRelationship));
				//	} else if (this.lookups.Field_CardinalReferenceRelationships.length > 0) {
				//		observables.push(this.cardinalFieldFromRelationshipSelected(this.lookups.Field_CardinalReferenceRelationships[0].value));
				//	}
				//	this.showDescription = false;
				//} catch (e) {
				//	console.log(e);
				//}
				break;
			case 'complexrelationlookup':
			//this.showDescription = false;
			//this.hasDisplayInColumn = false;
			//if (this.model.RelationItems == null || this.model.RelationItems.length === 0) {
			//	const r = new FieldTypeRelationItemEditorModel();

			//	r.DisplayFields = [];
			//	r.AssetTypeUid = this.GetCurrentUid();

			//	this.model.RelationItems = [];
			//	this.model.RelationItems.push(r);
			//	this.relationItemCount = 1;
			//	this.loadRelationItems(this.model.RelationItems.length - 1).subscribe();
			//}
			//break;
			case 'tag':
				//if (!isFromLoad) { this.showIsEditable = false; }
				//this.showDescription = false;
				//this.enableAllowMultipleValues = false;
				//this.hasDisplayInColumn = false;
				break;
			case "ownershiplookup":
				//this.showDescription = false;
				//this.onEnableListSingleResponsibilityType(this.model.FieldType.Type[this.selectedFieldType].Definition.ResponsibilityTypeUid?.length > 1);
				break;
			case 'computedownershiplookup':
			case 'json':
			case 'jsonelement':
				this.hasDisplayInColumn = false;
			case 'path':
				this.showDescription = false;

				break;
			case 'score':
				//observables.push(this.loadAvailableScoreTypes());
				//this.enableAllowMultipleValues = false;
				//this.showDescription = false;
				break;
			case 'counter':
				//this.model.FieldType.Type.Counter.ShowIfEmpty = true;
				//if (!this.model.FieldType.Type.Counter.CounterInitialIndex) {
				//	this.model.FieldType.Type.Counter.CounterInitialIndex = this.numberOfAssetsForType;
				//}
				//this.showIsRequired = false;
				//this.enableAllowMultipleValues = false;
				//this.showDescription = false;
				break;
			default:
				break;
		}
		//if (this.selectedFieldType === 'Date' && this.model.FieldType.Type[this.selectedFieldType].DefaultValue != null) {
		//	this.defaultDate = new Date(this.model.FieldType.Type[this.selectedFieldType].DefaultValue);
		//}

		//if (this.selectedFieldType === 'Link' && this.model.FieldType.Type[this.selectedFieldType].DefaultValue != null) {
		//	this.defaultLinkName = this.model.FieldType.Type[this.selectedFieldType].DefaultValue.Text;
		//	this.defaultLinkAdress = this.model.FieldType.Type[this.selectedFieldType].DefaultValue.Url;
		//}

		//this.errorMessage = ""; //clear the error message when changing types

		//observables
		//	.filter((x) => x != null && x != null)
		//	.forEach((obs) => obs.pipe(map(() => this.validate('*'))).subscribe());
	}


	get showAddToSearch(): boolean {
		const allowedTypes = ['Counter', 'Date'];
		return this.assetTypeUid && allowedTypes.indexOf(this.selectedFieldType) > -1;
	}

	get showIsPartOfKey(): boolean {
		const allowedTypes = ['Counter', 'Date'];
		return this.assetTypeUid && allowedTypes.indexOf(this.selectedFieldType) > -1;
	}

	get showIsListable(): boolean {
		const allowedTypes = ['Counter', 'Date'];
		return this.assetTypeUid && allowedTypes.indexOf(this.selectedFieldType) > -1;
	}

	get showPersistInFilters(): boolean {
		const allowedTypes = ['Counter', 'Date'];
		return this.assetTypeUid && allowedTypes.indexOf(this.selectedFieldType) > -1;
	}

	get showIsEditable(): boolean {
		const allowedTypes = ['Date'];
		return this.assetTypeUid && allowedTypes.indexOf(this.selectedFieldType) > -1;
	}

	get showIsRequired(): boolean {
		const allowedTypes = ['Date'];
		return this.assetTypeUid && allowedTypes.indexOf(this.selectedFieldType) > -1;
	}

	get enableAllowMultipleValues(): boolean {
		const allowedTypes = ['Lookup'];
		return this.assetTypeUid && allowedTypes.indexOf(this.selectedFieldType) > -1;
	}

	get hasFormDescription(): boolean {
		const allowedTypes = ['Date'];
		return allowedTypes.indexOf(this.selectedFieldType) > -1;
	}

	public getLocaleDateString(): string {
		return FormHelpers.getLocaleDateString();
	}

}
