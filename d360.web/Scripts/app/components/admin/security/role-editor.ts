import { Component, ElementRef, EventEmitter, Input, OnChanges, Output, SimpleChanges, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { ReadRole } from '../../../models/security.model';
import { RelationshipsService } from '../../../services/relationships.service';
import { SecurityService } from '../../../services/security.service';

/*global $localize*/

@Component({
	selector: 'role-editor',
	templateUrl: './role-editor.html',
	styles: [`
	.form-wrapper { 
		padding-top:16px; 
	} 

	.form-editor-container {     
		max-height: calc(100vh - 147px);
		overflow: auto;
		margin-bottom: 16px;
	}`],
	providers: [RelationshipsService],

})

export class RoleEditor implements OnChanges {
	@Input() item: ReadRole;
	@Input() isModalVisible: false;

	@Output() onCancel = new EventEmitter();
	@Output() onSave = new EventEmitter();

	title: string = $localize`Edit`;

	saveLabel: string;
	cancelLabel: string;

	isLoading: boolean = false;

	isFormDisabled: boolean = false;
	isSaveDisabled: boolean = false;
	isFormSet: boolean = false;
	hasChanges: boolean = false;

	isSaving: boolean = false;
	formSub: Subscription;

	roleForm: FormGroup = null;
	@ViewChild('form', { static: false }) formElement: ElementRef;

	constructor(
		private fb: FormBuilder,
		private securityService: SecurityService) {
		this.title = $localize`Add Role`;

		this.roleForm = this.fb.group({
			name: [null, { validators: [Validators.required, Validators.maxLength(250)], updateOn: "blur" }],
			description: [null, { validators: [Validators.maxLength(1000)], updateOn: "blur" }]
		});
	}

	ngOnChanges(changes: SimpleChanges) {
		if (changes && changes.name && changes.name.currentValue !== changes.name.previousValue) {
			this.loadForm();
		}
	}

	async loadForm() {
		this.isFormDisabled = false;
		this.isSaveDisabled = false;
		this.isFormSet = false;
		this.hasChanges = false;

		if (this.formSub) {
			this.formSub.unsubscribe();
		}

		if (this.item.name) {
			this.saveLabel = $localize`Save Changes`;
			this.cancelLabel = $localize`Close`;
			this.title = $localize`Edit Role`;
		}
		else {
			this.item = new ReadRole();
			this.title = $localize`Add Role`;
			this.saveLabel = this.title;
			this.cancelLabel = $localize`Cancel`;
			this.isFormSet = true;
		}
	}

	onSubmit() {
		this.isSaving = true;
		if (this.item && this.item.uid !== '') {
			this.securityService.updateRole(this.item.uid, this.item)
				.subscribe((d) => {
					this.isSaving = false;
					this.onSave.emit(d);
					this.cancel();
				});
		}
		else {
			this.securityService.createRole(this.item)
				.subscribe((d) => {
					this.isSaving = false;
					this.onSave.emit(d);
					this.cancel();
				});
		}
	}

	get isSubmitDisabled(): boolean {
		return !this.roleForm.valid || (this.item && !this.hasChanges);
	}

	async cancel() {
		await this.loadForm();
		this.onCancel.emit();
	}
}