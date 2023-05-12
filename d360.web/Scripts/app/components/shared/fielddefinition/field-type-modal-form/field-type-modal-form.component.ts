import { AfterViewChecked, ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, Input, OnChanges, OnInit, Output, QueryList, SimpleChange, ViewChild, ViewChildren, ViewEncapsulation } from "@angular/core";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";
import { SelectItem } from "primeng/api";
import { Table } from "primeng/table";
import { forkJoin, Subscription } from "rxjs";
import { AssetType, AssetTypeClass, } from "../../../../models/asset.model";
import { FieldType, FieldTypeAPIModel, FieldTypeAPIModelField } from "../../../../models/fieldtype-api.model";
import { AssetTypeService } from "../../../../services/asset-type.service";
import { FieldsObservableService } from "../../../../services/fieldsObservable.service";
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
	@Input() uid: string;

	@Input() actionTypeUid: string;
	@Input() assetTypeUid: string;
	@Input() relationshipTypeUid: string;

	@Output() onClose = new EventEmitter();
	@Output() onUpdated = new EventEmitter();
	fieldTypeForm: FormGroup = null;
	fieldType: FieldTypeAPIModel;

	title = 'unset';
	subTitle = 'unset';

	isLoading = false;
	savingInProgress = false;
	formState: FormState = FormState.FieldTypeSelection;
	areFieldTypesLoading: boolean = false;

	@ViewChild('modal', { static: false }) modal: D3SModal;
	@ViewChild('form', { static: false }) formElement: ElementRef;
	@ViewChild('dt', { static: false }) dt: Table;

	@ViewChildren(PropertyGroupComponent) propertyGroups: QueryList<PropertyGroupComponent>;


	private isEditFormUpdated: boolean = false;
	private changeFormSub: Subscription;
	fieldTypeSelection: Record<string, object>;
	selectedFieldType: string;


	fieldTypes: SelectItem[] = [];

	constructor(private fb: FormBuilder,
		private assetTypeService: AssetTypeService,
		private fieldsService: FieldsObservableService,
		private elRef: ElementRef,
		private cdRef: ChangeDetectorRef
	) {
	
	}

	fieldTokens = [
		{
			"title": "Name"
		}
	]

	ngOnInit() {
		this.isLoading = true;
		this.setForm();
		this.areFieldTypesLoading = true;
		this.fieldsService.getLookups(this.assetTypeUid, this.actionTypeUid, this.relationshipTypeUid)
			.subscribe((res) => {
				this.fieldTypes = res.DataTypes;
				this.areFieldTypesLoading = false;
				this.cdRef.markForCheck();
			});

	}

	ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		if (changes['isModalVisible']) {
			if (changes['isModalVisible'].previousValue !== changes['isModalVisible'].currentValue) { // object has changed            
				this.updateForm();
			}
		}
	}
	setForm() {
		this.fieldTypeForm = this.fb.group({
			FriendlyName: [null, { validators: [Validators.required] }],
			Name: [null, { validators: [Validators.required] }]
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
		//this.fieldTypeForm.controls["name"].setValue('Default');

	}

	updateForm() {
		this.subTitle = this.assetTypeName;

		if (this.uid) {
			//if (this.changeFormSub) {
			//	this.changeFormSub.unsubscribe();
			//}
			//this.isLoading = true;

			//forkJoin(
			//	this.assetTypeService.GetAssetTypeByUid(this.uid),
			//	this.fieldsService.getAssetTypeFields(this.uid)
			//).subscribe((results) => {
			//	const assetType = results[0];
			//	this.fieldTokens = [];
			//	if (results[1] && results[1].length) {
			//		results[1].forEach((field) => {
			//			const keyFieldTypes = ["Text", "Date", "DateTime", "Number", "Boolean", "Decimal", "Lookup", "Counter"];
			//			if (keyFieldTypes.some((ft) => ft.toLowerCase() === field.Type.toLowerCase())) {
			//				this.fieldTokens.push({ title: field.Name });
			//			}
			//		});
			//	}

			//	this.fieldTypeForm.controls["Name"].setValue(assetType.Name);


			//	this.title = $localize`Edit Field`;
			//	this.subTitle = assetType.Name;



			//	this.isEditFormUpdated = false;
			//	setTimeout(() => {
			//		this.changeFormSub = this.fieldTypeForm.valueChanges.subscribe(() => {
			//			this.isEditFormUpdated = true;
			//		});
			//	}, 200);
			//	this.isLoading = false;
			//});
		}
		else {
			this.title = $localize`Add Field`;
			this.formState = FormState.FieldTypeSelection;
			this.fieldTypeSelection = null;
			this.selectedFieldType = '';

			this.setDefaultFormValues();
		}
	}

	save() {
		this.savingInProgress = true;
		const model = new FieldTypeAPIModel();
		model.AssetTypeUid = this.assetTypeUid;
		model.RelationshipTypeUid = this.relationshipTypeUid;
		model.ActionTypeUid = this.actionTypeUid;
		model.Fields = [];
		model.Fields[0] = new FieldTypeAPIModelField();
		model.Fields[0].Type = new FieldType(this.selectedFieldType);
		const type = model.Fields[0].Type;
		model.Fields[0].FriendlyName = this.fieldTypeForm.get("FriendlyName").value;
		model.Fields[0].Name = this.fieldTypeForm.get("Name").value;

		console.log(model);
		return;
		//let saveObs = this.assetTypeService.postAssetType(model);

		//if (this.uid) {
		//	model.Uid = this.uid;
		//	saveObs = this.assetTypeService.putAssetType(model);
		//}

		//saveObs.subscribe((res) => {
		//	if (res) {
		//		this.onUpdated.emit(res);
		//		this.close();
		//	}
		//	this.savingInProgress = false;
		//});
	}

	get isFormDisabled(): boolean {
		return this.savingInProgress || this.fieldTypeForm.invalid || (this.uid && !this.isEditFormUpdated);
	}

	get saveButtonLabel(): string {
		if (this.uid) {
			return $localize`Save Changes`;
		}
		else {
			return $localize`Add Field Type`;
		}
	}

	get closeButtonLabel(): string {
		if (this.uid && this.isEditFormUpdated) {
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
		console.log(this.selectedFieldType);
		this.formState = FormState.Form;
		this.subTitle = this.assetTypeName + " - " + this.fieldTypeSelection["label"];
		this.cdRef.markForCheck();
	}
}
