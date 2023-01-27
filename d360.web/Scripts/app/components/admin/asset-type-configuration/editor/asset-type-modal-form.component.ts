import { AfterViewChecked, ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, Input, OnChanges, OnInit, Output, QueryList, SimpleChange, ViewChild, ViewChildren } from "@angular/core";
import { FormBuilder, FormControl, FormGroup, Validators } from "@angular/forms";
import { SelectItem } from "primeng/api";
import { forkJoin } from "rxjs";
import { AssetType, AssetTypeClass, Hierarchy, IconStyle } from "../../../../models/asset.model";
import { Predicate } from "../../../../models/predicate.model";
import { AssetTypeService } from "../../../../services/asset-type.service";
import { AssetService } from "../../../../services/asset.service";
import { FieldsObservableService } from "../../../../services/fieldsObservable.service";
import { RelationshipsService } from "../../../../services/relationships.service";
import { PropertyGroupComponent } from "../../../shared/controls/property-group/property-group.component";

/*global $localize*/

@Component({
	selector: "asset-type-modal-form",
	templateUrl: './asset-type-modal-form.component.html'
})
export class ConfigurationAssetTypeModalForm implements OnChanges, OnInit, AfterViewChecked {
	@Input() isModalVisible: boolean = false;
	@Input() assetTypeClass: AssetTypeClass;
	@Input() uid: string;
	@Input() parentUid: string;

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
	hierarchyPredicates: Predicate[] = [];
	hierarchyPredicatesSelectItem: SelectItem[] = [];

	flowObjectTypes: SelectItem[] = [];

	@ViewChild('form', { static: false }) formElement: ElementRef;
	@ViewChildren(PropertyGroupComponent) propertyGroups: QueryList<PropertyGroupComponent>;

	selectedIcon: string = '';

	constructor(private fb: FormBuilder,
		private assetService: AssetService,
		private assetTypeService: AssetTypeService,
		private fieldsService: FieldsObservableService,
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
		let parentPredicatesSub = this.relationshipService.getPredicates("InterTypeHierarchy");

		if (this.isHierarchy) {
			parentPredicatesSub = this.relationshipService.getPredicates("IntraTypeHierarchy");
		}

		forkJoin(
			this.assetService.getAllColors(),
			this.relationshipService.getPredicates('Grammar'),
			parentPredicatesSub
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

			this.hierarchyPredicates = results[2];
			this.hierarchyPredicatesSelectItem = [];
			this.hierarchyPredicates.forEach((p) => {
				this.hierarchyPredicatesSelectItem.push({ value: p.Uid, title: p.Inverse, label: p.Inverse });
			});

			this.flowObjectTypes = [];
			this.flowObjectTypes.push({ value: 'Event', label: $localize`Event` });
			this.flowObjectTypes.push({ value: 'Activity', label: $localize`Activity` });
			this.flowObjectTypes.push({ value: 'Gateway', label: $localize`Gateway` });
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
			predicateUid: [null, { updateOn: "blur" }],
			autoDisplayParent: [null, { updateOn: "blur" }],
			canEditParent: [null, { updateOn: "blur" }],
			flowObjectType: [null, { updateOn: "blur" }],
			maxDepth: [null, { updateOn: "blur" }]
		});

		this.setDefaultFormValues();
	}

	setDefaultFormValues() {
		if (!this.assetTypeForm) {
			return;
		}
		this.assetTypeForm.reset();
		this.assetTypeForm.controls["displayFormat"].setValue('{Name}');
		this.assetTypeForm.controls["descriptionButtonName"].setValue($localize`Information`);
		this.assetTypeForm.controls["backgroundColor"].setValue('#202020');
	}

	updateForm() {
		if (this.uid) {
			this.isLoading = true;

			forkJoin(
				this.assetTypeService.GetAssetTypeByUid(this.uid),
				this.fieldsService.getAssetTypeFields(this.uid)
			).subscribe((results) => {
				const assetType = results[0];
				this.fieldTokens = [];
				if (results[1] && results[1].length) {
					results[1].forEach((field) => {
						const keyFieldTypes = ["Text", "Date", "DateTime", "Number", "Decimal", "Lookup"];
						if (keyFieldTypes.some((ft) => ft.toLowerCase() === field.Type.toLowerCase())) {
							this.fieldTokens.push({ title: field.Name });
						}
					});
				}

				this.assetTypeForm.controls["name"].setValue(assetType.Name);
				this.assetTypeForm.controls["displayFormat"].setValue(assetType.DisplayFormat);
				this.assetTypeForm.controls["description"].setValue(assetType.Description);
				this.assetTypeForm.controls["isDescriptionEnabled"].setValue(assetType.IsDescriptionEnabled);
				this.assetTypeForm.controls["descriptionButtonName"].setValue(assetType.DescriptionButtonName);
				this.assetTypeForm.controls["isDescriptionVisibleByDefault"].setValue(assetType.IsDescriptionVisibleByDefault);
				this.assetTypeForm.controls["backgroundColor"].setValue(assetType.IconStyle.BackColor);
				this.assetTypeForm.controls["icon"].setValue(assetType.IconStyle.Icon);
				this.selectedIcon = assetType.IconStyle.Icon;
				this.assetTypeForm.controls["useAsTransformation"].setValue(assetType.UseAsTransformation);
				this.assetTypeForm.controls["autoDisplayParent"].setValue(assetType.AutoDisplayParent);
				this.assetTypeForm.controls["canEditParent"].setValue(assetType.CanEditParent);

				let predicateUid = null;
				if (assetType.PredicateInverse) {
					predicateUid = this.hierarchyPredicates.find((x) => x.Inverse.toLowerCase() === assetType.PredicateInverse.toLowerCase())?.Uid;
				}

				if (this.isHierarchy) {
					this.assetTypeForm.controls["maxDepth"].setValue(assetType.HierarchyMaximumDepth);
				}

				this.assetTypeForm.controls["predicateUid"].setValue(predicateUid);

				this.title = $localize`Edit Asset Type`;
				this.subTitle = assetType.Name;

				if (assetType.SynonymAllocations && assetType.SynonymAllocations.length > 0) {
					assetType.SynonymAllocations.forEach((syn) => {
						this.assetTypeForm.controls[`syn_${syn}`].setValue(true);
					});
				}

				if (this.isDiagramAssetTypeForm) {
					this.assetTypeForm.controls["flowObjectType"].setValue(assetType.FlowObjectType);
				}

				this.isLoading = false;
			});
		}
		else {
			this.title = $localize`Add Asset Type`;

			switch (this.assetTypeClass) {
				case AssetTypeClass.BusinessAsset:
					this.subTitle = $localize`Business Assets`;
					break;
				case AssetTypeClass.TechnicalAsset:
					this.subTitle = $localize`Technical Assets`;
					break;
				case AssetTypeClass.Rule:
					this.subTitle = $localize`Rules`;
					break;
				case AssetTypeClass.Policy:
					this.subTitle = $localize`Policies`;
					break;
				case AssetTypeClass.Model:
					this.subTitle = $localize`Models`;
					break;
				case AssetTypeClass.DiagramAsset:
					this.subTitle = $localize`Diagram Assets`;
					break;
				default:
					this.subTitle = `unset`;
			}

			this.setDefaultFormValues();
		}
	}

	updateDisplayFormat($event) {
		const newValue = this.assetTypeForm.get("displayFormat").value + `{${$event.value}}`;
		this.assetTypeForm.controls["displayFormat"].setValue(newValue);
	}

	save() {
		this.savingInProgress = true;
		const model = new AssetType();
		model.Class = this.assetTypeClass;
		model.Name = this.assetTypeForm.get("name").value;
		model.DisplayFormat = this.assetTypeForm.get("displayFormat").value;
		model.Description = this.assetTypeForm.get("description").value;
		model.IsDescriptionEnabled = this.assetTypeForm.get("isDescriptionEnabled").value;
		model.DescriptionButtonName = this.assetTypeForm.get("descriptionButtonName").value;
		model.IsDescriptionVisibleByDefault = this.assetTypeForm.get("isDescriptionVisibleByDefault").value;

		model.BackgroundColor = this.assetTypeForm.get("backgroundColor").value;
		model.IconStyle = new IconStyle();
		model.IconStyle.Icon = this.assetTypeForm.get("icon").value;
		model.IconStyle.BackColor = this.assetTypeForm.get("backgroundColor").value;
		model.IconStyle.ForeColor = "#FFF";
		model.UseAsTransformation = this.assetTypeForm.get("useAsTransformation").value;

		model.AutoDisplayParent = this.assetTypeForm.get("autoDisplayParent").value;
		model.CanEditParent = this.assetTypeForm.get("canEditParent").value;

		if (this.hasPredicateUid) {
			model.Hierarchy = new Hierarchy();
			model.Hierarchy.PredicateUid = this.assetTypeForm.get("predicateUid").value;
		}

		if (this.parentUid) {
			model.ParentUid = this.parentUid;
		}

		if (this.isHierarchy) {
			model.Hierarchy.MaximumDepth = this.assetTypeForm.get("maxDepth").value;
			model.ParentUid = null;
		}

		if (this.isDiagramAssetTypeForm) {
			model.FlowObjectType = this.assetTypeForm.get("flowObjectType").value;
		}

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

	get hasPredicateUid(): boolean {
		return this.parentUid != null || this.isHierarchy;
	}

	get isHierarchy(): boolean {
		return this.assetTypeClass === AssetTypeClass.Model || this.assetTypeClass === AssetTypeClass.Policy;
	}

	get isFormDisabled(): boolean {
		return this.savingInProgress || this.assetTypeForm.invalid;
	}

	get saveButtonLabel(): string {
		return this.uid ? $localize`Save Changes` : $localize`Add Asset Type`;
	}

	@HostListener('window:resize', ['$event'])
	onResize() {
		this.setFormHeight();
	}

	ngAfterViewChecked() {
		this.setFormHeight();
	}

	modalFormMaxHeight = 400;
	private setFormHeight() {
		let groupsHeight = 0;
		let topPos = 260;
		if (this.elRef.nativeElement) {
			const els = this.elRef.nativeElement.getElementsByClassName('form-wrapper');
			if (els[0]) {
				const rect = els[0].getBoundingClientRect();
				topPos = rect.top + 120;
			}
		}
		const maxHeight = window.innerHeight - topPos;
		if (this.propertyGroups) {
			this.propertyGroups.forEach((pg) => {
				const height = pg.inputContainer.nativeElement.offsetHeight;
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

	get isDiagramAssetTypeForm() {
		return this.assetTypeClass === AssetTypeClass.DiagramAsset;
	}

	get showStylesPropertyGroup() {
		return !this.isDiagramAssetTypeForm;
	}

	get showSynonymPropertyGroup() {
		return !this.isDiagramAssetTypeForm && this.assetTypeClass !== AssetTypeClass.Rule;
	}
}
