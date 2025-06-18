import { ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges, ViewChild } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { ReadSecurityPolicy, PolicyEditOptionsModel, PolicyEditAssetTypeOptionsModel, SecurityPolicyWhen, SecurityPolicyThen } from '../../../models/security.model';
import { SecurityService } from '../../../services/security.service';
import { Operator } from '../../../models/operator.model';
import { FormFeedbackBadgesModule } from '../../../components/shared/controls/form-feedback-badges/form-feedback-badges.component';
import { LoadingComponent } from '../../../_shared/components/loading';
import { PropertyGroupModule } from '../../../components/shared/controls/property-group/property-group.component';
import { DataCyModule } from '../../../directives/ig-data-cy.directive';
import { CheckboxModule } from 'primeng/checkbox';
import { IgMessageBoxModule } from '../../../components/shared/controls/message-box/message-box.module';
import { RadioButtonModule } from 'primeng/radiobutton';
import { ButtonModule } from '../../../directives/ig-button-directive';
import { SiteModalModule } from '../../../components/shared/modal/gov-modal.module';
import { DirectivesModule } from '../../../directives/directives.module';
import { DropdownModule } from 'primeng/dropdown';

@Component({
	selector: 'policy-editor',
	templateUrl: './policy-editor.html',
	styles: [`
	.form-wrapper { 
		padding-top:16px; 
	} 

	.form-editor-container {     
		max-height: calc(100vh - 147px);
		overflow: auto;
		margin-bottom: 16px;
	}

	div.when-condition-row {
		display: grid;
		grid-template-columns: 24px 1fr 150px 1fr 32px;
		margin-bottom: 8px;

		div:nth-child(1) {

			.when-condition-type {
				background-color: var(--buttonBackColor);
				color: var(--calculatedButtonTextColor);
				font-weight: 600;
				padding: 1px 3px;
				border-radius: 2px;
				font-size: .9em;
				line-height: 32px;
				vertical-align: middle;
				cursor: pointer;
			}
		}

		div:nth-child(2), div:nth-child(3) {
			padding-right: 8px;
		}

		div:nth-child(5) {
			text-align:right;

			a {
				text-decoration:none; 
				color:#000; 
				cursor: pointer; 
				font-size:1.5em;
			}
		}
	}

	div.then-condition-row {
		display: grid;
		grid-template-columns: 24px 1fr 32px;
		margin-bottom: 8px;

		div:nth-child(1) {

			.then-condition-type {
				background-color: var(--buttonBackColor);
				color: var(--calculatedButtonTextColor);
				font-weight: 600;
				padding: 1px 3px;
				border-radius: 2px;
				font-size: .9em;
				line-height: 32px;
				vertical-align: middle;
				cursor: pointer;
			}
		}

		div:nth-child(3) {
			text-align:right;

			a {
				text-decoration:none; 
				color:#000; 
				cursor: pointer; 
				font-size:1.5em;
			}
		}
	}
	
	.form-row-spacer {
		margin-bottom: 12px;

		span {
			label {
				line-height: 20px;
				vertical-align: middle;
			}
		}
	}
	`],
	standalone: true,
	imports: [
		ButtonModule,
		CheckboxModule,
		DataCyModule,
		DirectivesModule,
		DropdownModule,
		FormFeedbackBadgesModule,
		IgMessageBoxModule,
		LoadingComponent,
		PropertyGroupModule,
		RadioButtonModule,
		ReactiveFormsModule,
		SiteModalModule
	]
})
export class PolicyEditor implements OnChanges, OnInit {
	@Input() item: ReadSecurityPolicy;
	@Input() options: PolicyEditOptionsModel;
	@Input() isModalVisible: false;

	@Output() onCancel = new EventEmitter();
	@Output() onSave = new EventEmitter();

	title: string = $localize`Edit`;
	instructions: string = '';
	newInstructions: string = $localize`Create a security policy that will assign users or groups to a role on a set of assets based on attributes of those assets.`;
	editInstructions: string = $localize`Updating this security policy that may alter role assignments on assets.`;
	addFieldCheckTitle = $localize`Add Field Condition`;
	addRelationCheckTitle = $localize`Add Relation Condition`;
	addThenTitle = $localize`Add`;

	saveLabel: string;
	cancelLabel: string;

	isLoading: boolean = false;
	isSaving: boolean = false;

	fieldTextOperators: any[] = [
		{ label: "contains", value: Operator[Operator.Contains] },
		{ label: "does not contain", value: Operator[Operator.NotContains] },
		{ label: "is", value: Operator[Operator.Equals] },
		{ label: "is not", value: Operator[Operator.NotEquals] },
		{ label: "starts with", value: Operator[Operator.StartsWith] },
		{ label: "ends with", value: Operator[Operator.EndsWith] },
		{ label: "is populated", value: Operator[Operator.Populated] },
		{ label: "is not populated", value: Operator[Operator.NotPopulated] }
	];

	fieldNumericOperators: any[] = [
		{ label: "is", value: Operator[Operator.Equals] },
		{ label: "is not", value: Operator[Operator.NotEquals] },
		{ label: "greater than", value: Operator[Operator.GreaterThan] },
		{ label: "greater than or equals", value: Operator[Operator.GreaterThanOrEquals] },
		{ label: "less than", value: Operator[Operator.LessThan] },
		{ label: "less than or equals", value: Operator[Operator.LessThanOrEquals] },
		{ label: "is populated", value: Operator[Operator.Populated] },
		{ label: "is not populated", value: Operator[Operator.NotPopulated] }
	];

	fieldBooleanOperators: any[] = [
		{ label: "is", value: Operator[Operator.Equals] },
		{ label: "is populated", value: Operator[Operator.Populated] },
		{ label: "is not populated", value: Operator[Operator.NotPopulated] }
	];

	fieldLookupOperators: any[] = [
		{ label: "is", value: Operator[Operator.Equals] },
		{ label: "is not", value: Operator[Operator.NotEquals] },
		{ label: "is populated", value: Operator[Operator.Populated] },
		{ label: "is not populated", value: Operator[Operator.NotPopulated] }
	];

	relationshipOperators: any[] = [
		{ label: "in", value: Operator[Operator.In] },
		{ label: "not in", value: Operator[Operator.NotIn] }
	];

	whenCheckTypes: any[] = [
		{ label: "Field", value: "F" },
		{ label: "Relation", value: "R" }
	];

	selectedAssetTypeOptions: PolicyEditAssetTypeOptionsModel;

	operators: any[] = [];
	securityGroupsLoaded: boolean = false;
	securityGroups: any[] = [];
	securityUsersLoaded: boolean = false;
	securityUsers: any[] = [];
	thenOptions: any[] = [];

	policyForm: FormGroup = null;
	@ViewChild('form', { static: false }) formElement: ElementRef;

	constructor(
		private fb: FormBuilder,
		private securityService: SecurityService,
		protected cdRef: ChangeDetectorRef) {
		this.title = $localize`Add Policy`;
	}


	ngOnInit(): void {
		this.policyForm = this.fb.group({
			name: [null, { validators: [Validators.required, Validators.maxLength(250), Validators.minLength(3)], updateOn: "change" }],
			assetTypeUid: [null, { validators: [Validators.required] }],
			roleUid: [null, { validators: [Validators.required] }],
			securityType: ["Group", { validators: [Validators.required] }],
			applyToType: [false],
			visible: [true],
			whenConditions: this.fb.array([]),
			thenConditions: this.fb.array([], { validators: [Validators.required] })
		});

		this.policyForm.controls['securityType'].valueChanges.subscribe(value => {
			const old = this.policyForm.value['securityType'];
			if (value !== old) {
				this.thenConditions.clear();
				this.addThen(value);
			}
		});
    }

	ngOnChanges(changes: SimpleChanges) {
		if (this.item?.assetTypeUid !== undefined && this.item?.assetTypeUid !== "") {
			this.securityService
				.getPolicyEditAssetTypeOptions(this.item.assetTypeUid)
				.subscribe((o) => {
					this.selectedAssetTypeOptions = o;
					this.loadForm();
				});
		} else {
			this.loadForm();
		}
	}

	get whenConditions(): FormArray {
		return this.policyForm.get('whenConditions') as FormArray;
	}

	get thenConditions(): FormArray {
		return this.policyForm.get('thenConditions') as FormArray;
	}

	get allowAddWhenRelation(): boolean {
		return (this.selectedAssetTypeOptions?.intersectTypes &&  this.selectedAssetTypeOptions?.intersectTypes.length > 0);
	}

	addWhen(type: string, condition: SecurityPolicyWhen): void {
		const group: FormGroup = this.fb.group({
			checkType: [type, Validators.required],
			fieldName: ['',],
			intersectTypeUid: ['',],
			operator: ['', Validators.required],
			value: ['',],
			assetUid: ['',]
		});

		if (!condition) {
			condition = new SecurityPolicyWhen();
			condition.checkType = type;
			if (type === "F") {
				const fieldName = this.selectedAssetTypeOptions?.fields[0].value;
				condition.fieldName = fieldName;
			}
			else {
				if (this.allowAddWhenRelation) {
					const intersectTypeUid = this.selectedAssetTypeOptions?.intersectTypes[0].value;
					condition.intersectTypeUid = intersectTypeUid;
				}
			}
		}

		group.patchValue(condition);

		let obs: Observable<boolean>;
		if (condition.checkType === "F") {
			obs = this.loadWhenValuesForField(group);
		}
		else {
			obs = this.loadWhenValuesForRelation(group);
		}

		obs.subscribe((o) => {
			group.controls["operator"].setValue(condition.operator);
			this.whenConditions.push(group);
		});
	}

	deleteWhenCondition(ix: number) {
		this.whenConditions.removeAt(ix);
	}

	addThen(type: string, condition: SecurityPolicyThen = null): void {
		const group: FormGroup = this.fb.group({
			operator: [Operator[Operator.Equals]],
			securityType: [type, Validators.required],
			securityUid: ['', Validators.required]
		});

		group.patchValue(condition);

		let obs: Observable<boolean>;
		if (type === "User") {
			obs = this.loadThenOptionsUser(group);
		}
		else {
			obs = this.loadThenOptionsGroup(group);
		}

		obs.subscribe((o) => {
			this.thenConditions.push(group);
		});

	}

	deleteThenCondition(ix: number) {
		this.thenConditions.removeAt(ix);
	}

	async cancel() {
		await this.loadForm();
		this.onCancel.emit();
	}

	clearConditions() {
		this.whenConditions.clear();
	}

	isNotAppliesToTypeAndHasWhenConditions() {
		const applyToType = this.policyForm.controls["applyToType"].getRawValue() as boolean;
		if (!applyToType && (this.policyForm.controls["whenConditions"] as FormArray).controls.length === 0) {
			return false;
		}
		return true;
	}

	isInputTypeForWhenField(item: FormGroup, targetTypes: string[]) {
		if (this.selectedAssetTypeOptions?.fields) {
			const formFieldValue: string = item.get('fieldName').value; 
			const selectedFieldType = this.selectedAssetTypeOptions?.fields.find((f) => f.value === formFieldValue);
			if (selectedFieldType) {
				return targetTypes.some((t) => t === selectedFieldType.type);
			}
		}
		return false;
	}

	isWhenValid() {
		const applyToType = this.policyForm.controls["applyToType"].getRawValue() as boolean;
		if (applyToType) {
			return false;
		}
		if ((this.policyForm.controls["whenConditions"] as FormArray).controls.length === 0) {
			return true;
		}
		return (this.policyForm.controls["whenConditions"].valid);
	}

	loadForm() {

		this.clearConditions();
		this.policyForm.reset();

		const isEdit = (this.item && this.item.uid !== null);

		//Set UI labels.
		if (isEdit) {
			this.instructions = this.editInstructions;
			this.saveLabel = $localize`Save Changes`;
			this.cancelLabel = $localize`Close`;
			this.title = $localize`Edit Security Policy`;
		}
		else {
			this.instructions = this.newInstructions;
			this.title = $localize`Add Security Policy`;
			this.saveLabel = this.title;
			this.cancelLabel = $localize`Cancel`;
		}

		if (!isEdit) {
			this.item = {
				applyToType: false,
				assetTypeName: '', assetTypeUid: '',
				MenuItems: [],
				name: '',
				roleName: '', roleUid: '',
				securityType: 'Group',
				thenConditions: [], visible: true, whenConditions: [],
				uid: null
			};
		}

		const loadFormValues = () => {
			this.policyForm.patchValue(this.item);
			if (this.item && this.item.whenConditions) {
				this.item.whenConditions.forEach((wC) => {
					this.addWhen(wC.checkType, wC);
				})
			}
			if (this.item && this.item.thenConditions) {
				this.thenConditions.clear();
				this.item.thenConditions.forEach((tC) => {
					this.addThen(tC.securityType, tC)
				});

				if (this.thenConditions.length === 0) {
					this.addThen(this.item.securityType);
				}
			}
		};

		loadFormValues();
	}

	get isGroup(): boolean {
		return (this.policyForm.get("securityType").value === "Group");
	}

	loadSelectedAssetTypeOptions(assetTypeUid: string) {
		if (assetTypeUid !== undefined && assetTypeUid !== "") {
			this.securityService.getPolicyEditAssetTypeOptions(assetTypeUid)
				.subscribe((o) => {
					this.selectedAssetTypeOptions = o;
				});
		}
	}

	loadWhenValues(item: FormGroup, type: string) {
		if (type === 'F') {
			this.loadWhenValuesForField(item).subscribe((o) => { });
		}
		else {
			this.loadWhenValuesForRelation(item).subscribe((o) => { });
		}
	}

	loadWhenValuesForField(item: FormGroup): Observable<boolean> {

		return new Observable<boolean>(obs => {
			if (!item) {
				obs.next();
				return;
			} 

			const formFieldValue: string = item.get("fieldName").value;
			(item as any).operators = this.fieldLookupOperators;

			if (!this.selectedAssetTypeOptions) {
				return;
			}

			const selectedFieldType = this.selectedAssetTypeOptions.fields.find((f) => f.value === formFieldValue);
			if (selectedFieldType) {
				const assetTypeUid = this.policyForm.get("assetTypeUid").getRawValue();

				const setValidation = (controltoClear: string, controlToValidate: string) => {
					item.get(controltoClear).clearValidators();
					item.get(controltoClear).setErrors(null);
					item.get(controlToValidate).addValidators([Validators.required]);
				};

				switch (selectedFieldType.type) {
					case "Lookup":
						(item as any).operators = this.fieldLookupOperators;
						if (assetTypeUid) {
							this.securityService.getPolicyEditFieldLookupOptions(assetTypeUid, formFieldValue).subscribe((results) => {
								(item as any).options = results;
								setValidation("value", "assetUid");
								obs.next();
							});
						}
						break;
					case "Boolean":
						(item as any).operators = this.fieldBooleanOperators;
						(item as any).options = [{ label: "Yes", value: "true" }, { label: "No", value: "false" }];
						setValidation("assetUid", "value");
						obs.next();
						break;
					case "Decimal":
					case "Number":
						(item as any).operators = this.fieldNumericOperators;
						(item as any).options = null;
						setValidation("assetUid", "value");
						obs.next();
						break;
					default:
						(item as any).operators = this.fieldTextOperators;
						(item as any).options = null;
						setValidation("assetUid", "value");
						obs.next();
						break;
				}
			}
			else {
				(item as any).options = [];
				obs.next();
			}
		});
	}

	loadWhenValuesForRelation(item: FormGroup): Observable<boolean> {
		return new Observable<boolean>(obs => {
			if (!item) {
				obs.next();
				return;
			} 

			const selectedIntersectType: string = item.get("intersectTypeUid").value;
			(item as any).operators = this.relationshipOperators;

			(item as any).options = [];

			const selectedAssetTypeUID = this.policyForm.controls["assetTypeUid"].getRawValue();

			if (selectedIntersectType) {
				if ((item as any).options.length === 0) {
					this.securityService.getPolicyEditRelationLookupOptions(selectedIntersectType, selectedAssetTypeUID).subscribe((results) => {
						(item as any).options = results;
						obs.next();
						//this.cdRef.detectChanges();
					});
				}
			}
		});
	}

	loadThenOptionsGroup(item: FormGroup): Observable<boolean> {
		return new Observable<boolean>(obs => {
			if (!this.securityGroupsLoaded) {
				this.securityService.getPolicyEditGroupOptions().subscribe((o) => {
					this.securityGroups = o;
					this.securityGroupsLoaded = true;
					(item as any).options = this.securityGroups;
					obs.next();
				});
			} else {
				(item as any).options = this.securityGroups;
				obs.next();
			}
		});
	}

	loadThenOptionsUser(item: FormGroup): Observable<boolean> {
		return new Observable<boolean>(obs => {
			if (!this.securityUsersLoaded) {
				this.securityService.getPolicyEditUserOptions().subscribe((o) => {
					this.securityUsers = o;
					this.securityUsersLoaded = true;
					(item as any).options = this.securityUsers;
					obs.next();
				});
			} else {
				(item as any).options = this.securityUsers;
				obs.next();
			}
		});
	}

	onSubmit() {
		this.isSaving = true;
		const itemToSave = this.policyForm.value as ReadSecurityPolicy;

		itemToSave.securityType = this.policyForm.get("securityType").value;

		if (this.item.uid) {
			itemToSave.uid = this.item.uid;
			this.securityService.updatePolicy(itemToSave)
				.subscribe((d) => {
					this.isSaving = false;
					if (d) {
						this.onSave.emit(d);
						this.cancel();
					}
				});
		}
		else {
			this.securityService.createPolicy(itemToSave)
				.subscribe((d) => {
					this.isSaving = false;
					if (d) {
						this.onSave.emit(d);
						this.cancel();
					}
				});
		}
	}

	get isDataAltered(): boolean {
		return (
			this.policyForm.touched || this.policyForm.dirty
		);
	}

	get isSubmitDisabled(): boolean {
		if (!this.isNotAppliesToTypeAndHasWhenConditions()) {
			return true;
		}
		return !this.policyForm.valid || (this.item && !this.isDataAltered);
	}

	debug() {
		console.log(this.policyForm);
	}
}