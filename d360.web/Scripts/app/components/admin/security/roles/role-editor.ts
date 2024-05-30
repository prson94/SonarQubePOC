import { Component, ElementRef, EventEmitter, Input, OnChanges, Output, SimpleChanges, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { ReadRole } from '../../../../models/security.model';
import { RelationshipsService } from '../../../../services/relationships.service';
import { SecurityService } from '../../../../services/security.service';

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
	isSaving: boolean = false;
	formSub: Subscription;

	roleForm: FormGroup = null;
	@ViewChild('form', { static: false }) formElement: ElementRef;

	//Read_Asset_Checked: boolean = false;
	Read_Asset: number = 1;
	Add_Asset: number = 2;
	Delete_Asset: number = 4;
	Edit_Asset: number = 8;

	Read_Owner: number = 32;
	Add_Owner: number = 64;
	Delete_Owner: number = 128;
	Edit_Owner: number = 256;

	Read_Relation: number = 1024;
	Add_Relation: number = 2048;
	Delete_Relation: number = 4096;
	Edit_Relation: number = 8192;

	constructor(
		private fb: FormBuilder,
		private securityService: SecurityService) {
		this.title = $localize`Add Role`;

		this.roleForm = this.fb.group({
			name: [null, { validators: [Validators.required, Validators.maxLength(250), Validators.minLength(3)], updateOn: "change" }],
			description: [null, { validators: [Validators.minLength(0), Validators.maxLength(4000)], updateOn: "change" }],
			Read_Asset: [null],
			Add_Asset: [null],
			Edit_Asset: [null],
			Delete_Asset: [null],
			Read_Owner: [null],
			Add_Owner: [null],
			Edit_Owner: [null],
			Delete_Owner: [null],
			Read_Relation: [null],
			Add_Relation: [null],
			Edit_Relation: [null],
			Delete_Relation: [null]
		});
	}

	ngOnChanges(changes: SimpleChanges) {
		if (changes && changes.item && changes.item.currentValue !== changes.item.previousValue) {
			this.loadForm();
		}
	}

	async loadForm() {
		if (this.formSub) {
			this.formSub.unsubscribe();
		}

		var permissions: number = 0;
		if (this.item && this.item.uid) {
			this.saveLabel = $localize`Save Changes`;
			this.cancelLabel = $localize`Close`;
			this.title = $localize`Edit Role`;

			this.roleForm.get("name").setValue(this.item.name);
			this.roleForm.get("description").setValue(this.item.description);

			permissions = this.item.permissions;
		}
		else {
			this.item = new ReadRole();
			this.title = $localize`Add Role`;
			this.saveLabel = this.title;
			this.cancelLabel = $localize`Cancel`;

			this.roleForm.get("name").setValue('');
			this.roleForm.get("description").setValue('');
		}

		this.roleForm.get("Read_Asset").setValue((permissions & this.Read_Asset) === this.Read_Asset);
		this.roleForm.get("Add_Asset").setValue((permissions & this.Add_Asset) === this.Add_Asset);
		this.roleForm.get("Edit_Asset").setValue((permissions & this.Edit_Asset) === this.Edit_Asset);
		this.roleForm.get("Delete_Asset").setValue((permissions & this.Delete_Asset) === this.Delete_Asset);

		this.roleForm.get("Read_Owner").setValue((permissions & this.Read_Owner) === this.Read_Owner);
		this.roleForm.get("Add_Owner").setValue((permissions & this.Add_Owner) === this.Add_Owner);
		this.roleForm.get("Edit_Owner").setValue((permissions & this.Edit_Owner) === this.Edit_Owner);
		this.roleForm.get("Delete_Owner").setValue((permissions & this.Delete_Owner) === this.Delete_Owner);

		this.roleForm.get("Read_Relation").setValue((permissions & this.Read_Relation) === this.Read_Relation);
		this.roleForm.get("Add_Relation").setValue((permissions & this.Add_Relation) === this.Add_Relation);
		this.roleForm.get("Edit_Relation").setValue((permissions & this.Edit_Relation) === this.Edit_Relation);
		this.roleForm.get("Delete_Relation").setValue((permissions & this.Delete_Relation) === this.Delete_Relation);
	}

	onSubmit() {
		this.isSaving = true;

		if (!this.item) {
			this.item = new ReadRole();
		}
		this.item.name = this.roleForm.get("name").value;
		this.item.description = this.roleForm.get("description").value;
		this.item.permissions = this.newFormPermissions;

		if (this.item.uid) {
			this.securityService.updateRole(this.item)
				.subscribe((d) => {
					this.isSaving = false;
					if (d) {
						this.onSave.emit(d);
						this.cancel();
					}
				});
		}
		else {
			this.securityService.createRole(this.item)
				.subscribe((d) => {
					this.isSaving = false;
					if (d) {
						this.onSave.emit(d);
						this.cancel();
					}
				});
		}
	}

	get newFormPermissions(): number {
		return (
			(this.roleForm.get("Read_Asset").value ? this.Read_Asset : 0)
			+ (this.roleForm.get("Add_Asset").value ? this.Add_Asset : 0)
			+ (this.roleForm.get("Edit_Asset").value ? this.Edit_Asset : 0)
			+ (this.roleForm.get("Delete_Asset").value ? this.Delete_Asset : 0)

			+ (this.roleForm.get("Read_Owner").value ? this.Read_Owner : 0)
			+ (this.roleForm.get("Add_Owner").value ? this.Add_Owner : 0)
			+ (this.roleForm.get("Edit_Owner").value ? this.Edit_Owner : 0)
			+ (this.roleForm.get("Delete_Owner").value ? this.Delete_Owner : 0)

			+ (this.roleForm.get("Read_Relation").value ? this.Read_Relation : 0)
			+ (this.roleForm.get("Add_Relation").value ? this.Add_Relation : 0)
			+ (this.roleForm.get("Edit_Relation").value ? this.Edit_Relation : 0)
			+ (this.roleForm.get("Delete_Relation").value ? this.Delete_Relation : 0)
		);
	}

	get isDataAltered(): boolean {
		return (
			this.roleForm.get("name").value !== this.item.name
			|| this.roleForm.get("description").value !== this.item.description
			|| this.newFormPermissions !== this.item.permissions
		);
	}

	get isSubmitDisabled(): boolean {
		return !this.roleForm.valid || (this.item && !this.isDataAltered);
	}

	async cancel() {
		await this.loadForm();
		this.onCancel.emit();
	}
}