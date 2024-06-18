import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges, ViewChild } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { PolicyEditAssetTypeOptionsModel, PolicyEditOptionsModel, PolicySecurityType, ReadSecurityPolicy, SecurityPolicyWhen } from '../../../../models/security.model';
import { SecurityService } from '../../../../services/security.service';
import { Operator } from '../../../../models/operator.model';

/*global $localize*/

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
	providers: [SecurityService],
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class PolicyEditor implements OnChanges, OnInit {
	@Input() item: ReadSecurityPolicy;
	@Input() options: PolicyEditOptionsModel;
	@Input() isModalVisible: false;

	@Output() onCancel = new EventEmitter();
	@Output() onSave = new EventEmitter();

	title: string = $localize`Edit`;
	addFieldCheckTitle = $localize`Add Field Condition`;
	addRelationCheckTitle = $localize`Add Relation Condition`;

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
	securityGroups: any[] = [];
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
			securityTypeBool: [true],
			securityType: [PolicySecurityType.Group, { validators: [Validators.required] }],
			applyToType: [false],
			visible: [true],
			whenConditions: this.fb.array([]),
			thenConditions: this.fb.array([
				this.fb.group({
					operator: [Operator.Equals],
					securityUid: ['', Validators.required]
				})
			])
		});
    }

	ngOnChanges(changes: SimpleChanges) {
		if (changes && changes.item && changes.item.currentValue !== changes.item.previousValue) {
			this.loadForm();

			if (this.item && this.item.uid) {
				this.loadSelectedAssetTypeOptions(this.item.assetTypeUid);
			}
		}
	}

	get whenConditions(): FormArray {
		return this.policyForm.get('whenConditions') as FormArray;
	}

	get thenConditions(): FormArray {
		return this.policyForm.get('thenConditions') as FormArray;
	}

	addWhen(type: string, condition: SecurityPolicyWhen): void {
		let group: FormGroup;

		if (condition) {
			group = this.fb.group({
				checkType: [type, Validators.required],
				fieldName: [condition.fieldName,],
				intersectTypeUid: [condition.intersectTypeUid,],
				operator: [Operator[condition.operator], Validators.required],
				value: [condition.value,],
				assetUid: [condition.assetUid,]
			});
			if (condition.checkType === "F") {
				this.loadWhenValuesForField(group, "fieldName");
			}
			else {
				this.loadWhenValuesForRelation(group);
			}
		}
		else { 
			group = this.fb.group({
				checkType: [type, Validators.required],
				fieldName: ['',],
				intersectTypeUid: ['',],
				operator: ['', Validators.required],
				value: ['',],
				assetUid: ['',]
			});
		}

		this.whenConditions.push(group);
	}

	async cancel() {
		await this.loadForm();
		this.onCancel.emit();
	}

	clearConditions() {
		this.whenConditions.clear();
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
		if (this.item?.applyToType) {
			return false;
		}
		return true;
		//if (!this.item?.whenConditions) {
		//	return true;
		//}

		//if (this.item?.whenConditions?.every((w) =>
		//	w.checkType
		//	&& w.operator
		//	&& (
		//		(
		//			w.checkType === 'F'
		//			&& w.fieldName
		//			&& ( (w.value && w.value.length > 0) || (w.assetUid && w.assetUid.length > 0) )
		//		)
		//		||
		//		( w.checkType === 'R' && w.intersectTypeUid && w.assetUid && w.assetUid.length > 0 )
		//	)
		//)
		//) {
		//	return true;
		//}
		//return false;
	}

	async loadForm() {

		this.clearConditions();

		if (this.item && this.item.uid) {
			this.saveLabel = $localize`Save Changes`;
			this.cancelLabel = $localize`Close`;
			this.title = $localize`Edit Policy`;


			if (this.item.securityType === PolicySecurityType.Group) {
				this.securityService.getPolicyEditGroupOptions().subscribe((o) => {
					this.securityGroups = o;
					this.loadThenOptions();
				});
			}
			else {
				this.securityService.getPolicyEditUserOptions().subscribe((o) => {
					this.securityUsers = o;
					this.loadThenOptions();
				});
			}

			this.policyForm.get('securityTypeBool').setValue(
				this.item.securityType === PolicySecurityType.Group
			);


		}
		else {
			this.item = {
				applyToType: false,
				assetTypeName: '', assetTypeUid: '',
				MenuItems: [],
				name: '',
				roleName: '', roleUid: '',
				securityType: PolicySecurityType.Group,
				thenConditions: [], visible: true, whenConditions: [],
				uid: null
			};
			this.title = $localize`Add Policy`;
			this.saveLabel = this.title;
			this.cancelLabel = $localize`Cancel`;

			this.securityService.getPolicyEditGroupOptions().subscribe((o) => {
				this.securityGroups = o;
			});
		}

		this.policyForm.patchValue(this.item);
		if (this.item && this.item.whenConditions) {
			this.item.whenConditions.forEach((wC) => {
				this.addWhen(wC.checkType, wC);
			})
		}
		//this.whenConditions.patchValue(this.item.whenConditions);
	}

	loadSelectedAssetTypeOptions(assetTypeUid: string) {
		this.securityService.getPolicyEditAssetTypeOptions(assetTypeUid)
			.subscribe((o) => {
				this.selectedAssetTypeOptions = o;
			});
	}

	loadWhenValuesForField(item: FormGroup, formFieldName: string) {

		const formFieldValue: string = item.get(formFieldName).value;
		(item as any).operators = this.fieldLookupOperators;

		let selectedFieldType = this.selectedAssetTypeOptions.fields.find((f) => f.value === formFieldValue);
		if (selectedFieldType) {
			switch (selectedFieldType.type) {
				case "Lookup":
					(item as any).operators = this.fieldLookupOperators;
					this.securityService.getPolicyEditFieldLookupOptions(this.item.assetTypeUid, formFieldValue).subscribe((results) => {
						(item as any).options = results;
						this.cdRef.detectChanges();
					});
					break;
				case "Boolean":
					(item as any).operators = this.fieldBooleanOperators;
					(item as any).options = [{ label: "Yes", value: "true" }, { label: "No", value: "false" }];
					this.cdRef.detectChanges();
					break;
				case "Decimal":
				case "Number":
					(item as any).operators = this.fieldNumericOperators;
					(item as any).options = null;
					this.cdRef.detectChanges();
					break;
				default:
					(item as any).operators = this.fieldTextOperators;
					(item as any).options = null;
					this.cdRef.detectChanges();
					break;
			}
		}
		else {
			(item as any).options = [];
			this.cdRef.detectChanges();
		}
	}

	loadWhenValuesForRelation(item: FormGroup) {

		const selectedIntersectType: string = item.get("intersectTypeUid").value;
		(item as any).operators = this.relationshipOperators;

		(item as any).options = [];
		if (selectedIntersectType) {
			if ((item as any).options.length === 0) {
				this.securityService.getPolicyEditRelationLookupOptions(selectedIntersectType, this.item.assetTypeUid).subscribe((results) => {
					(item as any).options = results;
					this.cdRef.detectChanges();
				});
			}
		}
	}

	loadThenOptions() {
		if (this.policyForm.get("securityTypeBool").value) {
			this.thenOptions = this.securityGroups;
		}
		else {
			this.thenOptions = this.securityUsers;
		}
	}

	onSubmit() {
		this.isSaving = true;
		const itemToSave = this.policyForm.value as ReadSecurityPolicy;

		itemToSave.securityType =
			this.policyForm.get("securityTypeBool").value ? PolicySecurityType.Group : PolicySecurityType.User;

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
			!this.policyForm.untouched
		);
	}

	get isSubmitDisabled(): boolean {
		return !this.policyForm.valid || (this.item && !this.isDataAltered);
	}
}