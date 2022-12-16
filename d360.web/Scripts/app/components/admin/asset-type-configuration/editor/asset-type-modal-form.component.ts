import { Component, ElementRef, Input, OnInit, ViewChild } from "@angular/core";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";
import { SelectItem } from "primeng/api";
import { AssetType, AssetTypeEditorModel } from "../../../../models/asset.model";
import { AssetService } from "../../../../services/asset.service";

@Component({
	selector: "asset-type-modal-form",
	templateUrl: './asset-type-modal-form.component.html',
	styleUrls: ['asset-type-modal-form.component.less']
})
export class ConfigurationAssetTypeModalForm implements OnInit {
	@Input() isModalVisible: boolean = true;
	assetTypeForm: FormGroup = null;
	model: AssetTypeEditorModel;

	title = 'Add Asset Type';
	subTitle = 'Business Assets';

	savingInProgress = false;
	defaultColors: SelectItem[] = [];
	chosenColor: string;
	defaultColorItem: SelectItem = { label: $localize`Custom`, value: 'Custom', title: 'Custom' };

	@ViewChild('form', { static: false }) formElement: ElementRef;
	constructor(private fb: FormBuilder,
		private assetService: AssetService) {
	}

	ngOnInit() {
		this.setForm();
		this.assetService.getAllColors().subscribe((x) => {
			this.defaultColors = x;
			this.defaultColors.unshift(this.defaultColorItem);
			this.chosenColor = "Ebony";
		});
	}

	setForm() {
		this.assetTypeForm = this.fb.group({
			name: [null, { validators: [Validators.required,], updateOn: "blur" }],
			displayFormat: ['{Name}', { updateOn: "blur" }],
			description: [null, { updateOn: "blur" }],
			isDescriptionEnabled: [false, { updateOn: "blur" }],
			descriptionButtonName: [$localize`Information`, { updateOn: "blur" }],
			isDescriptionVisibleByDefault: [false, { updateOn: "blur" }],
			backgroundColor: ['#202020', { updateOn: "blur" }]
		});
	}


	fieldTokens = [
		{
			"title": "Name"
		}
	]

	updateDisplayFormat($event) {
		var newValue = this.assetTypeForm.get("displayFormat").value + `{${$event.value}}`;
		this.assetTypeForm.controls["displayFormat"].setValue(newValue);
	}

	save() {
		var keys = Object.keys(this.assetTypeForm.controls);
		keys.forEach((key) => {
			console.log(key + "=>" + this.assetTypeForm.get(key).value);
		})
	}

	onColorSelect($event) {
		this.chosenColor = $event;
		let selectedValue = this.defaultColors.find((x) => x.value === $event).title;
		if (selectedValue === 'Custom') {
			selectedValue = "#202020";
		}
		this.assetTypeForm.controls["backgroundColor"].setValue(selectedValue);
	}
}
