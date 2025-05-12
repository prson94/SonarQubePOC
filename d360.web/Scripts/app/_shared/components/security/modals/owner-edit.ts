import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AssetOwnerModel, UpdateSecurityPolicyOverride } from '../../../../models/security.model';
import { SecurityService } from '../../../../services/security.service';
import { CompanySettingsService } from '../../../../services/settings.service';
import { BaseComponent } from '../../../../components/shared/base.component';
import { FormFeedbackBadgesModule } from '../../../../components/shared/controls/form-feedback-badges/form-feedback-badges.component';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { SiteModalModule } from '../../../../components/shared/modal/gov-modal.module';
import { IgMessageBoxModule } from '../../../../components/shared/controls/message-box/message-box.module';
import { LoadingComponent } from '../../loading';
import { CoreModule } from '../../../../components/shared/core.module';

@Component({
	selector: 'owner-edit',
	templateUrl: './owner-edit.html',
	standalone: true,
	imports: [
		CoreModule,
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
export class OwnerEdit extends BaseComponent implements OnChanges, OnInit {
	@Input() uid: string;
	@Input() model: AssetOwnerModel;
	@Input() isModalVisible: false;

	@Output() onSave = new EventEmitter();
	@Output() onCancel = new EventEmitter();

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
		this.frm = this.fb.group({
			context: [this.model.context, {}]
		});
	}

    ngOnChanges(changes: SimpleChanges): void {
		if (changes) {
			this.frm?.patchValue({
				context: this.model.context
			});
		}
    }

	async cancel(): Promise<void> {
		this.onCancel.emit();
	}

	onSubmit() {
		this.isSaving = true;
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

	get isDataAltered(): boolean {
		return (this.frm) ? !this.frm.untouched : false;
	}

	get isSubmitDisabled(): boolean {
		return ((this.frm) ? !this.frm.valid : false) || (!this.isDataAltered);
	}
}
