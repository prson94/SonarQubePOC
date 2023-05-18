import { AfterViewChecked, ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, Input, OnChanges, OnInit, Output, QueryList, SimpleChange, ViewChild, ViewChildren, ViewEncapsulation } from "@angular/core";
import { AbstractControl, FormBuilder, FormGroup, ValidatorFn, Validators } from "@angular/forms";
import { stubFalse, values } from "lodash-es";
import { SelectItem } from "primeng/api";
import { Table } from "primeng/table";
import { forkJoin, map, Observable, Subscription } from "rxjs";
import { AssetTypeClass, } from "../../../../models/asset.model";
import { AssetTypeAncestry } from "../../../../models/fields.model";
import { FieldType, FieldTypeAPIModel, FieldTypeAPIModelField } from "../../../../models/fieldtype-api.model";
import { AssetService } from "../../../../services/asset.service";
import { FieldsObservableService } from "../../../../services/fieldsObservable.service";
import { FormHelpers } from "../../../../static/form-helpers";
import { PropertyGroupComponent } from "../../../shared/controls/property-group/property-group.component";
import { D3SModal } from "../../../shared/modal/gov-modal.component";
import { PopupMenuItem } from "../../controls/popup-menu/popup-menu.component";

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
	editedFieldType: FieldTypeAPIModelField;

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
	fieldFromRelationshipItems: SelectItem[] = [];
	fieldsFromRelation: SelectItem[] = [];
	referenceListFromRelationshipRelations: SelectItem[] = [];
	relationshipItems: SelectItem[] = [];

	relationshipDisplayFormatValueOptions: SelectItem[];
	scoreTypeOptions: SelectItem[];

	lookupAssetTypes: SelectItem[] = [];
	lookupFieldTokens: any[] = [];
	regexPatternTokens: any[] = [];

	lookupDefaultValueOptions: SelectItem[] = [];
	lookupSelectedDefaultValueOption: any;

	responsibilityTypes: SelectItem[] = [];

	assetTypeAncestries: AssetTypeAncestry[] = [];

	isInitialDataLoaded: boolean = false;
	numberOfAssetsForType: number = 0;

	private fieldTypeNameToApiNameMap = {
		'FieldFromRelationship': 'ComputedRelationshipField',
		'OwnershipLookup': 'ComputedOwnershipLookup',
		'RefListRelationship': 'ComputedRelationshipReferenceList'
	}

	get isEditing(): boolean {
		return this.name ? true : false;
	}

	get supportsPrimaryFilterOption(): boolean {
		return this.assetTypeUid ? true : false;
	}

	linkFieldOptionalPlaceholder: string = $localize`Optional: you should start the URL with a protocol prefix eg. http:// or https://`;
	linkFieldRequiredPlaceholder: string = $localize`Value required: you should start the URL with a protocol prefix eg. http:// or https://`;


	constructor(private fb: FormBuilder,
		private fieldsService: FieldsObservableService,
		private assetService: AssetService,
		private cdRef: ChangeDetectorRef
	) {

		this.relationshipDisplayFormatValueOptions = [
			{ label: $localize`Display Format`, value: true },
			{ label: $localize`Asset Path`, value: false },
		];
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
			this.assetService.getAssetCountsByAssetTypeUid(this.assetTypeUid),
			this.fieldsService.getAvailableScoreTypes(this.assetTypeUid)
		).subscribe((results) => {
			//fields
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

			//lookups
			if (results[1]) {
				this.fieldFromRelationshipItems = results[1].Field_FieldFromRelRelationships;
				this.responsibilityTypes = results[1].FieldResponsibilityTypes;
				this.fieldTypes = results[1].DataTypes;

				this.fieldTypes.forEach((ft) => {
					if (this.fieldTypeNameToApiNameMap[ft.value]) {
						ft.value = this.fieldTypeNameToApiNameMap[ft.value];
					}
				});

				this.lookupAssetTypes = results[1].Lookups.map((x) => {
					if (x.value.length && x.value.length === 36) { return { value: x.value.toLowerCase(), label: x.label }; }
					else { return { value: x.value, label: x.label }; }
				});

				this.referenceListFromRelationshipRelations = results[1].Field_CardinalReferenceRelationships;

				this.relationshipItems = [];
				results[1].Field_Relationships.forEach((i) => {
					if (typeof i.value === "undefined") {
						i.value = null;
					}
					this.relationshipItems.push({ label: i.title, value: `${i.value}|${i.isSubject}` });
				});

				this.regexPatternTokens = [];
				results[1].Patterns.forEach((item) => {
					this.regexPatternTokens.push({ title: item.label, value: item.value });
				});
			}

			//asset count
			if (results[2].length > 0) {
				this.numberOfAssetsForType = +results[2].length + 1;
			}

			//score types
			if (results[3].length > 0) {
				this.scoreTypeOptions = results[3];
			}

			this.setForm();
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

		model.Fields[0].FriendlyName = this.fieldTypeForm.get("FriendlyName").value;
		model.Fields[0].Name = this.fieldTypeForm.get("Name").value;
		model.Fields[0].Category = this.fieldTypeForm.get("Category").value ?? null;

		model.Fields[0].Type = new FieldType(this.selectedFieldType);
		const type = model.Fields[0].Type;
		let fieldTypeApiObject = type[this.selectedFieldType];

		//Asset Path
		if (this.selectedFieldType === 'Path') {
			type.Path.Definition.AssetTypeUid = this.fieldTypeForm.get("AssetPathListSegment").value ?? null;
		}

		//Counter
		if (this.selectedFieldType === 'Counter') {
			fieldTypeApiObject.CounterInitialIndex = this.fieldTypeForm.get("CounterInitialIndex").value ?? 1;
			fieldTypeApiObject.CounterPrefix = this.fieldTypeForm.get("CounterPrefix").value ?? '';
		}

		if (this.selectedFieldType === 'ComputedRelationshipField') {
			fieldTypeApiObject.IntersectTypeUid = this.fieldTypeForm.get("IntersectTypeUid").value ?? null;
			fieldTypeApiObject.FieldTypeName = this.fieldTypeForm.get("FieldTypeName").value ?? null;
		}

		fieldTypeApiObject.Description.Display = this.fieldTypeForm.get("DisplayDescription").value ?? null;
		fieldTypeApiObject.Description.Form = this.fieldTypeForm.get("FormDescription").value ?? null;

		fieldTypeApiObject.Search.AddToResult = this.fieldTypeForm.get("AddToResult").value ?? false;
		if (fieldTypeApiObject.Search.AddToResult) {
			fieldTypeApiObject.Search.Prefix = this.fieldTypeForm.get("Prefix").value ?? null;
			fieldTypeApiObject.Search.Suffix = this.fieldTypeForm.get("Suffix").value ?? null;
			fieldTypeApiObject.Search.DisplayOrder = this.fieldTypeForm.get("DisplayOrder").value ?? null;
		}
		else {
			fieldTypeApiObject.Search.Prefix = null;
			fieldTypeApiObject.Search.Suffix = null;
			fieldTypeApiObject.Search.DisplayOrder = null;
		}


		fieldTypeApiObject.IsDisplayable = this.fieldTypeForm.get("IsDisplayable").value ?? false;
		fieldTypeApiObject.DisplayInColumn = this.fieldTypeForm.get("DisplayInColumn").value ?? false;
		fieldTypeApiObject.IsEditable = this.fieldTypeForm.get("IsEditable").value ?? false;
		fieldTypeApiObject.Validation.IsRequired = this.fieldTypeForm.get("IsRequired").value ?? false;
		fieldTypeApiObject.IsPartOfKey = this.fieldTypeForm.get("IsPartOfKey").value ?? false;
		fieldTypeApiObject.IsPrimaryFilter = this.fieldTypeForm.get("IsPrimaryFilter").value ?? false;
		fieldTypeApiObject.AllowMultipleValues = this.fieldTypeForm.get("AllowMultipleValues").value ?? false;
		fieldTypeApiObject.ShowIfEmpty = this.fieldTypeForm.get("ShowIfEmpty").value ?? false;

		fieldTypeApiObject.Validation.MinimumValue = this.fieldTypeForm.get("MinimumValue").value ?? null;
		fieldTypeApiObject.Validation.MaximumValue = this.fieldTypeForm.get("MaximumValue").value ?? null;
		fieldTypeApiObject.Validation.Precision = this.fieldTypeForm.get("Precision").value ?? null;
		fieldTypeApiObject.Increment = this.fieldTypeForm.get("Increment").value ?? null;

		fieldTypeApiObject.DefaultValue = this.fieldTypeForm.get("DefaultValue").value ?? null;

		if (this.selectedFieldType === 'Link') {
			fieldTypeApiObject.DefaultValue = {
				Text: this.fieldTypeForm.get("LinkDefaultName").value ?? null,
				Url: this.fieldTypeForm.get("LinkDefaultUrl").value ?? null
			}
		}

		if (this.selectedFieldType === 'Lookup') {
			fieldTypeApiObject.List.Uid = this.fieldTypeForm.get("LookupUid").value ?? null;
			fieldTypeApiObject.List.AllowMultipleValues = this.fieldTypeForm.get("AllowMultipleValues").value ?? null;
			fieldTypeApiObject.AllowAllValue = this.fieldTypeForm.get("AllowAllValue").value ?? null;
			fieldTypeApiObject.AllowAllLabel = this.fieldTypeForm.get("AllowAllLabel").value ?? null;
			fieldTypeApiObject.Format.Display = this.fieldTypeForm.get("DisplayFormat").value ?? null;
			fieldTypeApiObject.Format.Edit = this.fieldTypeForm.get("EditFormat").value ?? null;
		}


		fieldTypeApiObject.IsListable = this.fieldTypeForm.get("IsListable").value ?? false;
		if (fieldTypeApiObject.IsListable) {
			fieldTypeApiObject.ColumnWidth = this.fieldTypeForm.get("ColumnWidth").value ?? null;
			fieldTypeApiObject.SortOrder = this.fieldTypeForm.get("SortOrder").value ?? null;
			fieldTypeApiObject.SortByAscending = this.fieldTypeForm.get("SortByAscending").value === 'true' ? true : false;
		} else {
			fieldTypeApiObject.ColumnWidth = null;
			fieldTypeApiObject.SortOrder = null;
			fieldTypeApiObject.SortByAscending = null;
		}

		if (this.selectedFieldType === 'ComputedOwnershipLookup') {
			fieldTypeApiObject.Definition.ResponsibilityTypeUid = this.fieldTypeForm.get("ResponsibilityTypeUid").value ?? null;
			fieldTypeApiObject.Definition.ExpandGroupMembership = this.fieldTypeForm.get("ExpandGroupMembership").value ?? null;
			fieldTypeApiObject.Definition.DisplayAssignmentSource = this.fieldTypeForm.get("DisplayAssignmentSource").value ?? null;
			fieldTypeApiObject.Definition.DisplayAsList = this.fieldTypeForm.get("DisplayAsList").value === 'true' ? true : false;
			fieldTypeApiObject.HideFilter = this.fieldTypeForm.get("HideFilter").value ?? false;
			fieldTypeApiObject.HideHeader = this.fieldTypeForm.get("HideHeader").value ?? false;
			fieldTypeApiObject.HideFooter = this.fieldTypeForm.get("HideFooter").value ?? false;
		}

		if (this.selectedFieldType === 'ComputedRelationshipReferenceList') {
			fieldTypeApiObject.DisplayRefListDescription = this.fieldTypeForm.get("DisplayRefListDescription").value ?? null;
			fieldTypeApiObject.IntersectTypeUid = this.fieldTypeForm.get("IntersectTypeUid").value ?? null;
		}


		if (this.selectedFieldType === 'Relationship') {
			const relValues = (this.fieldTypeForm.get("IntersectTypeUid").value as string).split('|');
			fieldTypeApiObject.IntersectTypeUid = relValues[0] ?? null;
			fieldTypeApiObject.UseDisplayFormat = this.fieldTypeForm.get("UseDisplayFormat").value ?? null;
			fieldTypeApiObject.IsSubject = relValues[1] === 'true' ? true : false;
		}

		if (this.selectedFieldType === 'Score') {
			fieldTypeApiObject.ScoreType = this.fieldTypeForm.get("ScoreType").value ?? null;
		}

		if (this.selectedFieldType === 'Text') {
			fieldTypeApiObject.Validation.Pattern = this.fieldTypeForm.get("ValidationPattern").value ?? null;
			fieldTypeApiObject.Validation.MaximumLength = this.fieldTypeForm.get("MaximumLength").value ?? null;
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
			DefaultValue: [null],
			MinimumValue: [null, { validators: [this.minMaxValueValidator(this.min_number, this.max_number), this.minimumValueValidator()] }],
			MaximumValue: [null, { validators: [this.minMaxValueValidator(this.min_number, this.max_number)] }],
			MaximumLength: [null, { validators: [this.minMaxValueValidator(this.min_number, this.max_number)] }],
			Precision: [null, { validators: [this.minMaxValueValidator(0, 5, true)] }],
			Increment: [null, { validators: [this.incrementValidation()] }],
			Suffix: [null],
			Prefix: [null],
			DisplayOrder: [null],
			IntersectTypeUid: [null],
			FieldTypeName: [null],
			LinkDefaultName: [null],
			LinkDefaultUrl: [null, { validators: Validators.pattern(/^(http|https):\/\//) }],
			LookupUid: [null],
			AllowAllValue: ['false'],
			AllowAllLabel: [null],
			DisplayFormat: [null],
			EditFormat: [null],
			ResponsibilityTypeUid: [null],
			ExpandGroupMembership: [null],
			DisplayAsList: ['false'],
			DisplayAssignmentSource: [null],
			HideFilter: [null],
			HideHeader: [null],
			HideFooter: [null],
			DisplayRefListDescription: [null],
			UseDisplayFormat: [null],
			ScoreType: [null],
			ValidationPattern: [null],
			RegexTestString: [null]
		});

		this.fieldTypeForm.controls["IntersectTypeUid"].valueChanges.subscribe((value) => {
			if (this.selectedFieldType === 'ComputedRelationshipField') {
				this.loadFieldsFromRelationships(value);
			}
		});

		this.fieldTypeForm.controls["LookupUid"].valueChanges.subscribe((value) => {
			this.loadLookupDefaultValue(value);
		});

		this.fieldTypeForm.controls["DisplayAsList"].valueChanges.subscribe((value) => {
			if (value === 'true') {
				this.fieldTypeForm.get('DisplayAssignmentSource').setValue(false);
				this.fieldTypeForm.get('HideFilter').setValue(false);
				this.fieldTypeForm.get('HideHeader').setValue(false);
				this.fieldTypeForm.get('HideFooter').setValue(false);
			}
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
		this.fieldTypeForm.get('DisplayAsList').setValue('false');
		this.fieldTypeForm.get('AllowAllValue').setValue('false');
		this.fieldTypeForm.get('SortByAscending').setValue('true');

		if (this.selectedFieldType === 'Decimal' || this.selectedFieldType === 'Number') {
			this.fieldTypeForm.controls["DefaultValue"].addValidators(this.numberDefaultValueValidator());
		}

		this.lookupSelectedDefaultValueOption = null;

		switch (this.selectedFieldType) {
			case 'Counter':
				this.fieldTypeForm.controls["CounterInitialIndex"].setValue(this.numberOfAssetsForType);
				this.fieldTypeForm.controls["IsDisplayable"].setValue(true);
				this.fieldTypeForm.controls["ShowIfEmpty"].setValue(true);
				break;
			case 'Path':
			case 'ComputedRelationshipField':
				this.fieldTypeForm.controls["IsDisplayable"].setValue(true);
				this.fieldTypeForm.controls["ShowIfEmpty"].setValue(true);
				break;
			case 'Date':
			case 'DateTime':
			case 'Decimal':
			case 'Html':
			case 'Link':
			case 'Lookup':
			case 'Number':
			case 'Text':
				this.fieldTypeForm.controls["IsDisplayable"].setValue(true);
				this.fieldTypeForm.controls["IsEditable"].setValue(true);
				this.fieldTypeForm.controls["ShowIfEmpty"].setValue(true);
				break;
			case 'Relationship':
				this.fieldTypeForm.controls["IsDisplayable"].setValue(true);
				this.fieldTypeForm.controls["IsEditable"].setValue(true);
				this.fieldTypeForm.controls["ShowIfEmpty"].setValue(true);
				this.fieldTypeForm.controls["UseDisplayFormat"].setValue(false);
				break;
			case 'JSON':
				this.fieldTypeForm.controls["ShowIfEmpty"].setValue(true);
				this.fieldTypeForm.controls["IsDisplayable"].setValue(true);
				break;
			case 'ComputedOwnershipLookup':
				this.fieldTypeForm.controls["IsDisplayable"].setValue(true);
				break;
			case 'ComputedRelationshipReferenceList':
				this.fieldTypeForm.controls["IsDisplayable"].setValue(true);
				this.fieldTypeForm.controls["ShowIfEmpty"].setValue(true);
				break;
			case 'Score':
				this.fieldTypeForm.controls["IsDisplayable"].setValue(true);
				this.fieldTypeForm.controls["ShowIfEmpty"].setValue(true);
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
				this.editedFieldType = results[0][0];
				this.selectedFieldType = Object.keys(this.editedFieldType.Type)[0];

				this.fieldTypeSelection = this.fieldTypes.find((s) => s.value === this.selectedFieldType);
				this.confirmTypeSelection();

				this.fieldTypeForm.controls["Name"].setValue(this.editedFieldType.Name);
				this.fieldTypeForm.controls["FriendlyName"].setValue(this.editedFieldType.FriendlyName);
				this.fieldTypeForm.controls["Category"].setValue(this.editedFieldType.Category);

				const type = this.editedFieldType.Type[this.selectedFieldType];

				this.fieldTypeForm.controls["DisplayDescription"].setValue(type?.Description?.Display ?? null);
				this.fieldTypeForm.controls["FormDescription"].setValue(type?.Description?.Form ?? null);

				this.fieldTypeForm.controls["AddToResult"].setValue(type?.Search?.AddToResult ?? null);
				this.fieldTypeForm.controls["Prefix"].setValue(type?.Search?.Prefix ?? null);
				this.fieldTypeForm.controls["Suffix"].setValue(type?.Search?.Suffix ?? null);
				this.fieldTypeForm.controls["DisplayOrder"].setValue(type?.Search?.DisplayOrder ?? null);


				this.fieldTypeForm.controls["IsDisplayable"].setValue(type?.IsDisplayable ?? null);
				this.fieldTypeForm.controls["DisplayInColumn"].setValue(type?.DisplayInColumn ?? null);
				this.fieldTypeForm.controls["IsEditable"].setValue(type?.IsEditable ?? null);
				this.fieldTypeForm.controls["IsListable"].setValue(type?.IsListable ?? null);
				this.fieldTypeForm.controls["IsRequired"].setValue(type?.Validation?.IsRequired ?? null);
				this.fieldTypeForm.controls["IsPartOfKey"].setValue(type?.IsPartOfKey ?? null);
				this.fieldTypeForm.controls["IsPrimaryFilter"].setValue(type?.IsPrimaryFilter ?? null);
				this.fieldTypeForm.controls["AllowMultipleValues"].setValue(type?.List?.AllowMultipleValues ?? null);
				this.fieldTypeForm.controls["ShowIfEmpty"].setValue(type?.ShowIfEmpty ?? null);

				this.fieldTypeForm.controls["DefaultValue"].setValue(type?.DefaultValue ?? null);
				this.fieldTypeForm.controls["MinimumValue"].setValue(type?.Validation?.MinimumValue ?? null);
				this.fieldTypeForm.controls["MaximumValue"].setValue(type?.Validation?.MaximumValue ?? null);
				this.fieldTypeForm.controls["Precision"].setValue(type?.Validation?.Precision ?? null);
				this.fieldTypeForm.controls["Increment"].setValue(type?.Increment ?? null);

				this.fieldTypeForm.controls["ColumnWidth"].setValue(type?.ColumnWidth ?? null);
				this.fieldTypeForm.controls["SortOrder"].setValue(type?.SortOrder ?? null);
				this.fieldTypeForm.controls["SortByAscending"].setValue((type?.SortByAscending ?? '').toString() ?? 'true');

				if (this.selectedFieldType === 'Path') {
					this.fieldTypeForm.controls["AssetPathListSegment"].setValue(type?.Definition?.AssetTypeUid ?? null);

					//asset path cannot be empty, so its always visible
					this.fieldTypeForm.controls["ShowIfEmpty"].setValue(true);
				}

				//Counter
				if (this.selectedFieldType === 'Counter') {
					this.fieldTypeForm.controls["CounterInitialIndex"].setValue(type?.CounterInitialIndex ?? this.numberOfAssetsForType);
					this.fieldTypeForm.controls["CounterPrefix"].setValue(type?.CounterPrefix ?? null);;
				}

				if (this.selectedFieldType === 'ComputedRelationshipField') {
					this.fieldTypeForm.controls["IntersectTypeUid"].setValue(type?.IntersectTypeUid ?? null);
					this.fieldTypeForm.controls["FieldTypeName"].setValue(type?.FieldTypeName ?? null);
				}

				if (this.selectedFieldType === 'Link') {
					this.fieldTypeForm.controls["LinkDefaultName"].setValue(type?.DefaultValue?.Text ?? null);
					this.fieldTypeForm.controls["LinkDefaultUrl"].setValue(type?.DefaultValue?.Url ?? null);
				}

				if (this.selectedFieldType === 'Lookup') {
					this.fieldTypeForm.controls["LookupUid"].setValue(type?.List?.Uid ?? null);
					this.fieldTypeForm.controls["AllowAllValue"].setValue(type?.AllowAllValue ?? null);
					this.fieldTypeForm.controls["AllowAllLabel"].setValue(type?.AllowAllLabel ?? null);
					this.fieldTypeForm.controls["DisplayFormat"].setValue(type?.Format?.Display ?? null);
					this.fieldTypeForm.controls["EditFormat"].setValue(type?.Format?.Edit ?? null);
				}

				if (this.selectedFieldType === 'ComputedOwnershipLookup') {
					this.fieldTypeForm.controls["ResponsibilityTypeUid"].setValue(type?.Definition?.ResponsibilityTypeUid ?? null);
					this.fieldTypeForm.controls["ExpandGroupMembership"].setValue(type?.Definition?.ExpandGroupMembership ?? null);
					this.fieldTypeForm.controls["DisplayAssignmentSource"].setValue(type?.Definition?.DisplayAssignmentSource ?? null);
					this.fieldTypeForm.controls["DisplayAsList"].setValue((type?.Definition?.DisplayAsList ?? 'false').toString());
					this.fieldTypeForm.controls["HideFilter"].setValue(type?.HideFilter ?? null);
					this.fieldTypeForm.controls["HideHeader"].setValue(type?.HideHeader ?? null);
					this.fieldTypeForm.controls["HideFooter"].setValue(type?.HideFooter ?? null);
				}

				if (this.selectedFieldType === 'ComputedRelationshipReferenceList') {
					this.fieldTypeForm.controls["DisplayRefListDescription"].setValue(type?.DisplayRefListDescription ?? null);
					this.fieldTypeForm.controls["IntersectTypeUid"].setValue(type?.IntersectTypeUid ?? null);
				}

				if (this.selectedFieldType === 'Relationship') {
					this.fieldTypeForm.controls["IntersectTypeUid"].setValue((type?.IntersectTypeUid + '|' + type?.IsSubject) ?? null);
					this.fieldTypeForm.controls["UseDisplayFormat"].setValue(type?.UseDisplayFormat ?? null);
				}

				if (this.selectedFieldType === 'Score') {
					this.fieldTypeForm.controls["ScoreType"].setValue(type?.ScoreType ?? null);
				}

				if (this.selectedFieldType === 'Text') {
					this.fieldTypeForm.controls["ValidationPattern"].setValue(type?.Validation?.Pattern ?? null);
					this.fieldTypeForm.controls["MaximumLength"].setValue(type?.Validation?.MaximumLength ?? null);
				}

				this.title = $localize`Edit Field`;

				this.isEditFormUpdated = false;
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
		this.changeFormSub = this.fieldTypeForm.valueChanges.subscribe((change) => {
			this.isEditFormUpdated = true;
			//this.fieldTypeForm.controls["MinimumValue"]
			//	.patchValue(this.fieldTypeForm.get("MinimumValue").value, { emitEvent: false, onlySelf: true });
		});
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

	confirmTypeSelection() {
		this.formState = FormState.Form;
		if (this.isEditing) {
			this.subTitle = this.assetTypeName + " - " + this.editedFieldType.FriendlyName;
		}
		else {
			this.subTitle = this.assetTypeName + " - " + this.fieldTypeSelection["label"];
		}
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

	onShowDetailChange($event: boolean) {
		if (!$event) {
			this.fieldTypeForm.controls["DisplayInColumn"].setValue(false);
		}
	}

	onMaxValueChange() {
		this.fieldTypeForm.controls['MinimumValue'].updateValueAndValidity();
		this.fieldTypeForm.controls['DefaultValue'].updateValueAndValidity();
	}

	onMinValueChange() {
		this.fieldTypeForm.controls['DefaultValue'].updateValueAndValidity();
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
				return (['ComplexRelationLookup', 'ComputedRelationshipReferenceList', 'System'].indexOf(this.selectedFieldType) > -1);
			case 'IsEditable':
				return (['ComplexRelationLookup', 'ComputedRelationshipField', 'Json', 'JSON', 'JsonElement', 'ComputedOwnershipLookup', 'Path', 'ComputedRelationshipReferenceList', 'Tag', 'Score', 'Counter', 'System'].indexOf(this.selectedFieldType) > -1);
			case 'IsListable':
				return (['ComplexRelationLookup', 'ComputedRelationshipReferenceList', 'Json', 'JSON', 'System'].indexOf(this.selectedFieldType) > -1
					|| (this.selectedFieldType === 'Relationship' && !this.isListableRelationship));
			case 'IsRequired':
				return (['ComplexRelationLookup', 'ComputedRelationshipField', 'Json', 'JSON', 'JsonElement', 'ComputedOwnershipLookup', 'Path', 'ComputedRelationshipReferenceList', 'Relationship', 'Tag', 'Score', 'Counter', 'System'].indexOf(this.selectedFieldType) > -1);
			case 'IsPartOfKey':
				return (['ComplexRelationLookup', 'ComputedRelationshipField', 'Json', 'JSON', 'JsonElement', 'ComputedOwnershipLookup', 'Path', 'ComputedRelationshipReferenceList', 'Relationship', 'Tag', 'Score', 'Link', 'System']
					.indexOf(this.selectedFieldType) > -1
					|| this.selectedFieldType === 'ReferenceItemType');
			case 'IsPrimaryFilter':
				return (!this.supportsPrimaryFilterOption || ['ComputedRelationshipField', 'ComplexRelationLookup', 'ComputedOwnershipLookup', 'Json', 'JSON', 'JsonElement', 'Path', 'ComputedRelationshipReferenceList', 'System'].indexOf(this.selectedFieldType) > -1);
			case 'AllowMultipleValues':
				return (['Lookup'].indexOf(this.selectedFieldType) === -1);
			case 'ShowIfEmpty':
				return (['Path', 'Tag', 'System'].indexOf(this.selectedFieldType) > -1 || (this.selectedFieldType === 'Score' && !this.fieldTypeForm.get("IsDisplayable").value) || (this.objectType === 'ReferenceItemType' && fieldName.toLocaleLowerCase() === "code"));
			case 'SearchAddToResult':
				return (['Path', 'Html', 'Json', 'JSON', 'JsonElement', 'ComputedOwnershipLookup', 'ComplexRelationLookup', 'ComputedRelationshipReferenceList', 'Score', 'Tag', 'System'].indexOf(this.selectedFieldType) > -1);
			case 'isSettingDisabled':
				return (['Json', 'JSON', 'JsonElement', 'ComplexRelationLookup', 'Tag', 'ComputedRelationshipReferenceList', 'System'].indexOf(this.selectedFieldType) > -1);
			case 'DisplayInColumn':
				if (this.selectedFieldType === "ComputedOwnershipLookup") {
					const isDisabled = !(this.fieldTypeForm.get("DisplayAsList").value === 'true');
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

	private max_number: number = Number.MAX_SAFE_INTEGER;
	private min_number: number = Number.MIN_SAFE_INTEGER;
	minMaxValueValidator(min: number, max: number, is_precision: boolean = false): ValidatorFn {
		return (control: AbstractControl): { [key: string]: any } | null => {
			if (control.value == null) {
				return {};
			}

			if (is_precision && (+control.value > max || +control.value < min)) {
				return {
					max_number: { value: control.value, message: $localize`Please enter decimal places between ${min} and ${max}` }
				};
			}

			if (+control.value > max) {
				return {
					max_number: { value: control.value, message: $localize`Please enter value smaller than ` + max }
				};
			}
			if (+control.value < min) {
				return {
					min_number: { value: control.value, message: $localize`Please enter value bigger than ` + min }
				};
			}
			return null;
		};
	}

	incrementValidation(): ValidatorFn {
		return (control: AbstractControl): { [key: string]: any } | null => {
			if (control.value == null) {
				return {};
			}

			if (+control.value < 0) {
				return {
					error: { value: control.value, message: $localize`Please enter a positive number for increment` }
				};
			}
			return null;
		};
	}

	numberDefaultValueValidator(): ValidatorFn {
		return (control: AbstractControl): { [key: string]: any } | null => {
			if (control.value == null || !this.fieldTypeForm) {
				return {};
			}
			const max_value = this.fieldTypeForm.get("MaximumValue").value;
			const min_value = this.fieldTypeForm.get("MinimumValue").value;


			if (max_value && +control.value > max_value) {
				return {
					error_max_value: { value: control.value, message: $localize`Please enter a maximum value of ` + max_value }
				};
			}
			if (max_value && +control.value < min_value) {
				return {
					error_min_value: { value: control.value, message: $localize`Please enter a minimum value of ` + min_value }
				};
			}
			return null;
		};
	}

	minimumValueValidator(): ValidatorFn {
		return (control: AbstractControl): { [key: string]: any } | null => {
			if (!this.fieldTypeForm) {
				return {};
			}
			const max_value = this.fieldTypeForm.get("MaximumValue").value;
			if (control.value == null || !max_value) {
				return {};
			}

			if (+control.value > +max_value) {
				return {
					invalid_value: { value: control.value, message: $localize`Please enter a value which is lower than maximum value` }
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
			case "ComputedOwnershipLookup":
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
		const allowedTypes = ['Counter', 'Date', 'DateTime', 'Decimal', 'ComputedRelationshipField', 'Link', 'Lookup', 'Number', 'Relationship', 'Text'];
		return this.assetTypeUid && allowedTypes.indexOf(this.selectedFieldType) > -1;
	}

	get showIsPartOfKey(): boolean {
		const allowedTypes = ['Counter', 'Date', 'DateTime', 'Decimal', 'Html', 'Number', 'Text'];
		return this.assetTypeUid && allowedTypes.indexOf(this.selectedFieldType) > -1;
	}

	get showIsListable(): boolean {
		const allowedTypes = ['Counter', 'Date', 'DateTime', 'Decimal', 'Html', 'Link', 'Lookup', 'Number', 'ComputedOwnershipLookup', 'Score', 'Text'];
		return this.assetTypeUid && allowedTypes.indexOf(this.selectedFieldType) > -1;
	}

	get showPersistInFilters(): boolean {
		const allowedTypes = ['Counter', 'Date', 'DateTime', 'Decimal', 'Html', 'Link', 'Lookup', 'Number', 'Relationship', 'Score', 'Text'];
		return this.assetTypeUid && allowedTypes.indexOf(this.selectedFieldType) > -1;
	}

	get showIsEditable(): boolean {
		const allowedTypes = ['Date', 'DateTime', 'Decimal', 'Html', 'Link', 'Lookup', 'Number', 'Relationship', 'Text'];
		return this.assetTypeUid && allowedTypes.indexOf(this.selectedFieldType) > -1;
	}

	get showIsRequired(): boolean {
		const allowedTypes = ['Date', 'DateTime', 'Decimal', 'Html', 'Link', 'Lookup', 'Number', 'Text'];
		return this.assetTypeUid && allowedTypes.indexOf(this.selectedFieldType) > -1;
	}

	get enableAllowMultipleValues(): boolean {
		const allowedTypes = ['Lookup'];
		return this.assetTypeUid && allowedTypes.indexOf(this.selectedFieldType) > -1;
	}

	get hasFormDescription(): boolean {
		const allowedTypes = ['Date', 'DateTime', 'Decimal', 'Html', 'Link', 'Lookup', 'Number', 'Relationship', 'Text'];
		return allowedTypes.indexOf(this.selectedFieldType) > -1;
	}

	public getLocaleDateString(): string {
		return FormHelpers.getLocaleDateString();
	}

	getObjectKeys(obj: Record<string, object>): string[] {
		if (!obj) {
			return [];
		}
		return Object.keys(obj);
	}

	loadingRelationFields: boolean = false;
	loadFieldsFromRelationships(intersectTypeUid: string) {
		if (intersectTypeUid == null) {
			return
		}

		this.loadingRelationFields = true;
		this.fieldsService.getRelationObjectFields(this.assetTypeUid, this.actionTypeUid, this.relationshipTypeUid, intersectTypeUid)
			.subscribe((d) => {
				this.fieldsFromRelation = d;
				this.loadingRelationFields = false;
				this.cdRef.markForCheck();
			});
	}

	loadingDefaultValues: boolean = false;
	loadLookupDefaultValue(uid: string) {
		if (!uid) {
			return;
		}
		this.loadingDefaultValues = true;
		forkJoin(
			this.fieldsService.getLookupDefaultValueOptions(uid),
			this.fieldsService.getLookupTokens(uid)
		).subscribe((data) => {
			this.lookupDefaultValueOptions = [];
			if (data[0] && data[0].length > 0) {
				this.lookupDefaultValueOptions = data[0].filter((x) => x.value !== null);
				this.lookupDefaultValueOptions.forEach((item) => item.value = (item.value as string).toUpperCase());
			}

			this.loadingDefaultValues = false;
			this.lookupFieldTokens = [];
			if (data[1] && data[1].length > 0) {
				data[1].forEach((item) => {
					this.lookupFieldTokens.push({ title: item.label, value: item.value });
				});

				if (!this.isEditing && !this.fieldTypeForm.get('DisplayFormat').value) {
					this.fieldTypeForm.controls['DisplayFormat'].setValue(`${this.lookupFieldTokens[0].value}`);
				}

				if (!this.isEditing && !this.fieldTypeForm.get('EditFormat').value) {
					this.fieldTypeForm.controls['EditFormat'].setValue(`${this.lookupFieldTokens[0].value}`);
				}
			}

			this.cdRef.markForCheck();
		});
	}
	updateControls(ctrlName, $event) {
		const currentValue = this.fieldTypeForm.get(ctrlName).value ?? '';
		const newValue = `${currentValue}{${$event.value}}`;

		this.fieldTypeForm.controls[ctrlName].setValue(newValue);
	}

	fieldTypeDisabledTooltip(item): string {
		if (item.value === 'ComputedRelationshipField' && (!this.fieldFromRelationshipItems || this.fieldFromRelationshipItems.length === 0)) {
			return $localize`No relationships are currently defined for this asset type`;
		}

		if (item.value === 'ComputedRelationshipReferenceList' && (!this.referenceListFromRelationshipRelations || this.referenceListFromRelationshipRelations.length === 0)) {
			return $localize`No reference item list from relationship is currently defined`;
		}

		if (item.value === 'Relationship' && (!this.relationshipItems || this.relationshipItems.length === 0)) {
			return $localize`No relationships are currently defined for this asset type`;
		}

		if (item.value === 'Score' && (!this.scoreTypeOptions || this.scoreTypeOptions.length === 0)) {
			return $localize`No scores are currently defined for this asset type`;
		}

		return null;
	}

	isValidPattern: boolean = null;
	validatePattern() {
		var pattern = this.fieldTypeForm.get('ValidationPattern').value;
		var testValue = this.fieldTypeForm.get('RegexTestString').value;

		if (((typeof pattern) !== "undefined") && pattern !== null && pattern.length > 0) {
			try {
				var regex = new RegExp(pattern);
				this.isValidPattern = regex.test(testValue);
				return;
			}
			catch (e) {
				this.isValidPattern = false;
				return;
			}
		}

		this.isValidPattern = false;
	}
}
