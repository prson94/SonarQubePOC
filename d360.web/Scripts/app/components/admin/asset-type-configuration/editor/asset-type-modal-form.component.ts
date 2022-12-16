import { Component, ElementRef, Input, OnInit, ViewChild } from "@angular/core";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";

@Component({
	selector: "asset-type-modal-form",
	templateUrl: './asset-type-modal-form.component.html',
	styleUrls: ['asset-type-modal-form.component.less']
})
export class ConfigurationAssetTypeModalForm implements OnInit {
	@Input() isModalVisible: boolean = true;
	assetTypeForm: FormGroup = null;

	title = 'Add Asset Type';
	subTitle = 'Business Assets';

	savingInProgress = false;

	@ViewChild('form', { static: false }) formElement: ElementRef;
	constructor(private fb: FormBuilder) {

	}

	ngOnInit() {
		this.setForm();
	}

	setForm() {
		this.assetTypeForm = this.fb.group({
			name: [null, { validators: [Validators.required,], updateOn: "blur" }]
		});
	}
}
