import { AfterViewChecked, ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, Input, OnChanges, OnInit, Output, QueryList, SimpleChange, ViewChild, ViewChildren } from "@angular/core";
import { FormBuilder, FormControl, FormGroup, Validators } from "@angular/forms";
import { result } from "lodash";
import { SelectItem } from "primeng/api";
import { forkJoin } from "rxjs";
import { AssetType, AssetTypeApiModel, AssetTypeClass, AssetTypeEditorModel, IconStyle } from "../../../../models/asset.model";
import { Predicate } from "../../../../models/predicate.model";
import { AssetTypeService } from "../../../../services/asset-type.service";
import { AssetService } from "../../../../services/asset.service";
import { RelationshipsService } from "../../../../services/relationships.service";
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
	@Output() onUpdated = new EventEmitter();
	assetTypeForm: FormGroup = null;

	title = 'unset';
	subTitle = 'unset';

	isLoading = false;
	savingInProgress = false;
	defaultColors: SelectItem[] = [];
	chosenColor: string;
	defaultColorItem: SelectItem = { label: $localize`Custom`, value: 'Custom', title: 'Custom' };

	synonyms: Predicate[] = [];

	@ViewChild('form', { static: false }) formElement: ElementRef;
	@ViewChildren(PropertyGroupComponent) propertyGroups: QueryList<PropertyGroupComponent>;

	constructor(private fb: FormBuilder,
		private assetService: AssetService,
		private assetTypeService: AssetTypeService,
		private relationshipService: RelationshipsService,
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
		forkJoin(
			this.assetService.getAllColors(),
			this.relationshipService.getSynonyms()
		).subscribe((results) => {
			this.synonyms = results[1];
			this.defaultColors = results[0];
			this.defaultColors.unshift(this.defaultColorItem);
			this.chosenColor = "Ebony";

			if (this.synonyms && this.synonyms.length > 0) {
				this.synonyms.forEach((syn) => {
					this.assetTypeForm.addControl(`syn_${syn.Uid}`, new FormControl(''));
				});
			}
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
		this.assetTypeForm = this.fb.group({
			name: [null, { validators: [Validators.required], updateOn: "blur" }],
			displayFormat: [null, { validators: [Validators.required], updateOn: "blur" }],
			description: [null, { updateOn: "blur" }],
			isDescriptionEnabled: [false, { updateOn: "blur" }],
			descriptionButtonName: [null, { updateOn: "blur" }],
			isDescriptionVisibleByDefault: [false, { updateOn: "blur" }],
			backgroundColor: [null, { updateOn: "blur" }],
			icon: [null, { updateOn: "blur" }],
			useAsTransformation: [null, { updateOn: "blur" }],
		});

		this.setDefaultFormValues();
	}

	setDefaultFormValues() {
		this.assetTypeForm.reset();
		this.assetTypeForm.controls["displayFormat"].setValue('{Name}');
		this.assetTypeForm.controls["descriptionButtonName"].setValue($localize`Information`);
		this.assetTypeForm.controls["backgroundColor"].setValue('#202020');
	}

	updateForm() {
		if (this.uid) {
			this.isLoading = true;
			this.assetTypeService.GetAssetTypeByUid(this.uid)
				.subscribe((assetType: AssetTypeApiModel) => {
					this.assetTypeForm.controls["name"].setValue(assetType.Name);
					this.assetTypeForm.controls["displayFormat"].setValue(assetType.DisplayFormat);
					this.assetTypeForm.controls["description"].setValue(assetType.Description);
					this.assetTypeForm.controls["isDescriptionEnabled"].setValue(assetType.IsDescriptionEnabled);
					this.assetTypeForm.controls["descriptionButtonName"].setValue(assetType.DescriptionButtonName);
					this.assetTypeForm.controls["isDescriptionVisibleByDefault"].setValue(assetType.IsDescriptionVisibleByDefault);
					this.assetTypeForm.controls["backgroundColor"].setValue(assetType.IconStyle.BackColor);
					this.assetTypeForm.controls["icon"].setValue(assetType.IconStyle.Icon);
					this.assetTypeForm.controls["useAsTransformation"].setValue(assetType.UseAsTransformation);

					this.title = $localize`Edit Asset Type`;
					this.subTitle = assetType.Name;

					if (assetType.SynonymAllocations && assetType.SynonymAllocations.length > 0) {
						assetType.SynonymAllocations.forEach((syn) => {
							this.assetTypeForm.controls[`syn_${syn}`].setValue(true);
						});
					}

					this.isLoading = false;
				});
		}
		else {
			this.title = $localize`Add Asset Type`;
			this.subTitle = this.assetTypeClass.toString();

			this.setDefaultFormValues();
		}
	}

	updateDisplayFormat($event) {
		var newValue = this.assetTypeForm.get("displayFormat").value + `{${$event.value}}`;
		this.assetTypeForm.controls["displayFormat"].setValue(newValue);
	}

	save() {
		this.savingInProgress = true;
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
		model.SynonymAllocations = [];

		if (this.synonyms && this.synonyms.length > 0) {
			this.synonyms.forEach((syn) => {
				this.assetTypeForm.get(`syn_${syn.Uid}`).value;
				if (this.assetTypeForm.get(`syn_${syn.Uid}`).value) {
					model.SynonymAllocations.push(syn.Uid);
				}
			});
		}

		if (!this.uid) {
			this.assetTypeService.postAssetType(model)
				.subscribe((res) => {
					this.onUpdated.emit(res);
					this.close();
					this.savingInProgress = false;
				});
		}
		else {
			model.Uid = this.uid;
			this.assetTypeService.putAssetType(model)
				.subscribe((res) => {
					this.onUpdated.emit(res);
					this.close();
					this.savingInProgress = false;
				});
		}

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

	get isFormDisabled(): boolean {
		return this.savingInProgress || this.assetTypeForm.invalid;
	}

	get saveButtonLabel(): string {
		return this.uid ? $localize`Save Changes` : $localize`Add Asset Type`;

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

	close() {
		this.setDefaultFormValues();
		this.onClose.emit();
	}
}
