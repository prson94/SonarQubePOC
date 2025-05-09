import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable, shareReplay } from 'rxjs';
import { CreateSecurityPolicyOverride, ReadRole, ReadSecurityPolicyOverride, UpdateSecurityPolicyOverride } from '../../../../models/security.model';
import { SecurityService } from '../../../../services/security.service';
import { CompanySettingsService } from '../../../../services/settings.service';
import { DropdownChangeEvent, DropdownModule } from 'primeng/dropdown';
import { BaseComponent } from '../../../../components/shared/base.component';
import { FormFeedbackBadgesModule } from '../../../../components/shared/controls/form-feedback-badges/form-feedback-badges.component';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { SiteModalModule } from '../../../../components/shared/modal/gov-modal.module';
import { IgMessageBoxModule } from '../../../../components/shared/controls/message-box/message-box.module';
import { LoadingComponent } from '../../loading';
import { CoreModule } from '../../../../components/shared/core.module';

@Component({
	selector: 'owner-create',
	templateUrl: './owner-create.html',
	standalone: true,
	imports: [
		CoreModule,
		DropdownModule,
		FormFeedbackBadgesModule,
		FormsModule,
		InputTextareaModule,
		IgMessageBoxModule,
		LoadingComponent,
		ReactiveFormsModule,
		SiteModalModule
	],
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class OwnerCreate extends BaseComponent implements OnChanges, OnInit {
	@Input() uid: string;
	@Input() assetUid: string;
	@Input() roleUid: string;
	@Input() securityType: number;
	@Input() context: string = '';
	@Input() securityUid: string;
	@Input() isModalVisible: false;

	@Output() onSave = new EventEmitter();
	@Output() onCancel = new EventEmitter();

	roles: ReadRole[];
	roles$: Observable<ReadRole[]>;
	assigneeOptions: any[] = [];
	securityGroups$: Observable<any[]>;
	securityUsers$: Observable<any[]>;

	frm: FormGroup = null;

	saveLabel: string = $localize`Save Changes`;
	cancelLabel: string = $localize`Close`;

	isLoading: boolean = false;
	isSaving: boolean = false;

	@ViewChild('form', { static: false }) formElement: ElementRef;

	constructor(
		private fb: FormBuilder,
		private securityService: SecurityService,
		protected settingsService: CompanySettingsService,
		protected cdr: ChangeDetectorRef) {
		super(settingsService);
		this.frm = new FormGroup({});
	}

	ngOnInit(): void {
		this.roles$ = this.securityService.getRoles().pipe(shareReplay());
		this.securityGroups$ = this.securityService.getPolicyEditGroupOptions().pipe(shareReplay());
		this.securityUsers$ = this.securityService.getPolicyEditUserOptions().pipe(shareReplay());

		this.roles$.subscribe((result) => {
			this.roles = result;
		})

		this.securityGroups$.subscribe((result) => {
			result.forEach((r) => {
				this.assigneeOptions.push({ value: r.value, label: r.label, icon: "fa-users", groupName: "Group" });
			});
			this.cdr.detectChanges();
		})

		this.securityUsers$.subscribe((result) => {
			result.forEach((r) => {
				this.assigneeOptions.push({ value: r.value, label: r.label, icon: "fa-user", groupName: "User" });
			});
			this.cdr.detectChanges();
		})

		this.frm = this.fb.group({
			assetUid: [this.assetUid, { validators: [Validators.required] }],
			roleUid: [this.roleUid, { validators: [Validators.required] }],
			securityType: [this.securityType, { }],
			securityUid: [this.securityUid, { validators: [Validators.required] }],
			context: [this.context, {}]
		});
	}

    ngOnChanges(changes: SimpleChanges): void {
		if (changes //&& (
			//(changes.uid && changes.uid.currentValue !== changes.uid.previousValue) ||
			//(changes.roleUid && changes.roleUid.currentValue !== changes.roleUid.previousValue) ||
			//(changes.securityType && changes.securityType.currentValue !== changes.securityType.previousValue) ||
			//(changes.securityUid && changes.securityUid.currentValue !== changes.securityUid.previousValue)
		//)
		) {
			this.frm?.patchValue({
				assetUid: this.assetUid,
				roleUid: this.roleUid,
				securityType: this.securityType,
				securityUid: this.securityUid,
				context: this.context
			});
		}
    }

	assigneeChanged(event: DropdownChangeEvent) {
		const assigneeUid = event.value;
		const option = this.assigneeOptions.find((a) => { return a.value === assigneeUid; });
		if (option.groupName === "Group") {
			this.frm.get("securityType").setValue("Group");
		}
		else {
			this.frm.get("securityType").setValue("User");
		}
	}

	async cancel(): Promise<void> {
		this.onCancel.emit();
	}

	onSubmit() {
		this.isSaving = true;
		if (this.uid) {
			const itemToSave = this.frm.value as UpdateSecurityPolicyOverride;
			itemToSave.uid = this.uid;
			this.securityService.updatePolicyOverride(itemToSave)
				.subscribe((d) => {
					this.isSaving = false;
					if (d) {
						this.onSave.emit(d);
						this.cancel();
					}
				});
		}
		else {
			const itemToSave = this.frm.value as CreateSecurityPolicyOverride;
			this.securityService.createPolicyOverride(itemToSave)
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
		return (this.frm) ? !this.frm.untouched : false;
	}

	get isSubmitDisabled(): boolean {
		return ((this.frm) ? !this.frm.valid : false) || (!this.isDataAltered);
	}
}
