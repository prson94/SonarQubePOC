import { AfterViewChecked, ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, Input, OnChanges, OnInit, Output, QueryList, SimpleChange, ViewChild, ViewChildren } from "@angular/core";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";
import { SelectItem } from "primeng/api";
import { AssetType, AssetTypeClass, AssetTypeEditorModel, IconStyle } from "../../../../models/asset.model";
import { AssetTypeService } from "../../../../services/asset-type.service";
import { AssetService } from "../../../../services/asset.service";
import { PropertyGroupComponent } from "../../../shared/controls/property-group/property-group.component";

@Component({
	selector: "asset-type-modal-form",
	templateUrl: './asset-type-modal-form.component.html',
	styleUrls: ['asset-type-modal-form.component.less']
})
export class ConfigurationAssetTypeModalForm implements OnChanges, OnInit, AfterViewChecked {
	@Input() isModalVisible: boolean = false;
	@Input() assetTypeClass: AssetTypeClass;
	@Input() uid: string;
	@Output() onClose = new EventEmitter();

	assetTypeForm: FormGroup = null;

	title = 'Add Asset Type';
	subTitle = 'Business Assets';

	savingInProgress = false;
	defaultColors: SelectItem[] = [];
	chosenColor: string;
	defaultColorItem: SelectItem = { label: $localize`Custom`, value: 'Custom', title: 'Custom' };

	@ViewChild('form', { static: false }) formElement: ElementRef;
	@ViewChildren(PropertyGroupComponent) propertyGroups: QueryList<PropertyGroupComponent>;

	constructor(private fb: FormBuilder,
		private assetService: AssetService,
		private assetTypeService: AssetTypeService,
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
		this.setForm();
		this.assetService.getAllColors().subscribe((x) => {
			this.defaultColors = x;
			this.defaultColors.unshift(this.defaultColorItem);
			this.chosenColor = "Ebony";
		});
	}

	ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		if (changes['uid']) {
			if (changes['uid'].previousValue !== changes['uid'].currentValue) { // object has changed            
				this.updateForm();
			}
		}
	}
	setForm() {
		this.assetTypeForm = this.fb.group({
			name: [null, { validators: [Validators.required], updateOn: "blur" }],
			displayFormat: ['{Name}', { validators: [Validators.required], updateOn: "blur" }],
			description: [null, { updateOn: "blur" }],
			isDescriptionEnabled: [false, { updateOn: "blur" }],
			descriptionButtonName: [$localize`Information`, { updateOn: "blur" }],
			isDescriptionVisibleByDefault: [false, { updateOn: "blur" }],
			backgroundColor: ['#202020', { updateOn: "blur" }],
			icon: [null, { updateOn: "blur" }],
			useAsTransformation: [null, { updateOn: "blur" }],
		});
	}

	updateForm() {
		console.log("updateing form");
	}

	updateDisplayFormat($event) {
		var newValue = this.assetTypeForm.get("displayFormat").value + `{${$event.value}}`;
		this.assetTypeForm.controls["displayFormat"].setValue(newValue);
	}

	save() {
		let model = new AssetType();
		model.Class = this.assetTypeClass;
		model.Name = this.assetTypeForm.get("name").value;
		model.DisplayFormat = this.assetTypeForm.get("displayFormat").value;
		model.Description = this.assetTypeForm.get("description").value;
		model.IsDescriptionEnabled = this.assetTypeForm.get("isDescriptionEnabled").value;
		model.DescriptionButtonName = this.assetTypeForm.get("descriptionButtonName").value;
		model.IsDescriptionVisibleByDefault = this.assetTypeForm.get("isDescriptionVisibleByDefault").value;
		model.BackgroundColor = this.assetTypeForm.get("backgroundColor").value;
		model.IconStyle = new IconStyle();
		model.IconStyle.Icon = this.assetTypeForm.get("backgroundColor").value;
		model.IconStyle.BackColor = "#000";
		model.IconStyle.ForeColor = "#FFF";
		model.UseAsTransformation = this.assetTypeForm.get("useAsTransformation").value;

		this.assetTypeService.postAssetType(model)
			.subscribe((res) => {

			});
	}

	onColorSelect($event) {
		this.chosenColor = $event;
		let selectedValue = this.defaultColors.find((x) => x.value === $event).title;
		if (selectedValue === 'Custom') {
			selectedValue = "#202020";
		}
		this.assetTypeForm.controls["backgroundColor"].setValue(selectedValue);
	}

	get hasUseAsTransformation(): boolean {
		return this.assetTypeClass === AssetTypeClass.BusinessAsset || this.assetTypeClass === AssetTypeClass.TechnicalAsset;
	}

	@HostListener('window:resize', ['$event'])
	onResize(event) {
		this.setFormHeight();
	}

	ngAfterViewChecked() {
		this.setFormHeight();
	}

	modalFormMaxHeight = 400;
	private setFormHeight() {
		var groupsHeight = 0;
		var topPos = 260;
		if (this.elRef.nativeElement) {
			var els = this.elRef.nativeElement.getElementsByClassName('form-wrapper');
			if (els[0]) {
				var rect = els[0].getBoundingClientRect();
				topPos = rect.top + 120;
			}
		}
		var maxHeight = window.innerHeight - topPos;
		if (this.propertyGroups) {
			var a = this.propertyGroups.first;
			this.propertyGroups.forEach((pg) => {
				var height = pg.inputContainer.nativeElement.offsetHeight;
				groupsHeight += height !== 0 ? (height + 34) : 34;
			});
			groupsHeight += 26; //form-wrapper bottom padding
		}

		this.modalFormMaxHeight = groupsHeight > maxHeight ? maxHeight : groupsHeight;
		this.cdRef.markForCheck();
	}
}
