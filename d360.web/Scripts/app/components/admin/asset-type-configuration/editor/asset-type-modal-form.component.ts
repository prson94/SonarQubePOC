import { AfterViewChecked, ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, Input, OnChanges, OnInit, Output, QueryList, SimpleChange, ViewChild, ViewChildren, ViewEncapsulation } from "@angular/core";
import { FormBuilder, FormControl, FormGroup, Validators } from "@angular/forms";
import { SelectItem } from "primeng/api";
import { forkJoin, Subscription } from "rxjs";
import { Editor } from "primeng/editor";
import { AssetType, AssetTypeClass, Hierarchy, IconStyle } from "../../../../models/asset.model";
import { Predicate } from "../../../../models/predicate.model";
import { AssetTypeService } from "../../../../services/asset-type.service";
import { AssetService } from "../../../../services/asset.service";
import { FieldsObservableService } from "../../../../services/fieldsObservable.service";
import { RelationshipsService } from "../../../../services/relationships.service";
import { IconPickerComponent } from "../../../shared/controls/icon-picker/icon-picker.component";
import { PropertyGroupComponent } from "../../../shared/controls/property-group/property-group.component";

/*global $localize*/

@Component({
	selector: "asset-type-modal-form",
	templateUrl: './asset-type-modal-form.component.html',
	encapsulation: ViewEncapsulation.None
})
export class ConfigurationAssetTypeModalForm implements OnChanges, OnInit, AfterViewChecked {
	@Input() isModalVisible: boolean = false;
	@Input() assetTypeClass: AssetTypeClass;
	@Input() uid: string;
	@Input() parentUid: string;
	@Input() parentTypeName: string;

	@Output() onClose = new EventEmitter();
	@Output() onUpdated = new EventEmitter();
	assetTypeForm: FormGroup = null;

	title = 'unset';
	subTitle = 'unset';

	isLoading = false;
	savingInProgress = false;
	defaultColors: SelectItem[] = [];
	defaultColorItem: SelectItem = { label: $localize`Custom`, value: 'Custom', title: 'Custom' };

	synonyms: Predicate[] = [];
	hierarchyPredicates: Predicate[] = [];
	hierarchyPredicatesSelectItem: SelectItem[] = [];

	flowObjectTypes: SelectItem[] = [];

	@ViewChild('form', { static: false }) formElement: ElementRef;
	@ViewChild('ed', { static: false }) primeEditor: Editor;

	@ViewChildren(PropertyGroupComponent) propertyGroups: QueryList<PropertyGroupComponent>;

	selectedIcon: string = '';
	eventTooltip = $localize`An event is represented by a circle and is something that "happens" during the course of a business process. These events affect the flow of the process and usually have a cause (trigger) or an impact (result).`;
	gatewayTooltip = $localize`A gateway is represented by the diamond shape and is used to control the divergence and convergence of connections. It will determine traditional decisions, as well as the forking, merging, and joining of paths.`;
	activityTooltip = $localize`An activity is represented by a rounded-corner rectangle and is a generic term for work that the company performs. The types of activities are Task and Sub-Process.`;

	private isEditFormUpdated: boolean = false;
	private changeFormSub: Subscription;

	defaultDescriptionButtonTextValue = $localize`Information`;

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
			this.flowObjectTypes.push({ value: 'Activity', label: $localize`Activity` });
			this.flowObjectTypes.push({ value: 'Event', label: $localize`Event` });
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
			name: [null, { validators: [Validators.required] }],
			displayFormat: [null, { validators: [Validators.required] }],
			description: [null],
			isDescriptionEnabled: [false],
			descriptionButtonName: [null],
			isDescriptionCollapsedByDefault: [true],
			backgroundColor: [null],
			backgroundColorTextValue: [null, { validators: [Validators.required] }],
			icon: [null],
			useAsTransformation: [null],
			predicateUid: [null],
			autoDisplayParent: [null],
			canEditParent: [null],
			flowObjectType: [null],
			maxDepth: [null]
		});

		this.setDefaultFormValues();
	}

	setDefaultFormValues() {
		if (!this.assetTypeForm) {
			return;
		}
		this.assetTypeForm.reset();
		this.assetTypeForm.controls["displayFormat"].setValue('{Name}');
		this.assetTypeForm.controls["descriptionButtonName"].setValue(this.defaultDescriptionButtonTextValue);
		this.assetTypeForm.controls["backgroundColor"].setValue('#202020');
		this.assetTypeForm.controls['backgroundColorTextValue'].setValue('Ebony');
		this.assetTypeForm.controls['isDescriptionCollapsedByDefault'].setValue(true);

		if (this.hasPredicateUid) {
			if (this.hierarchyPredicatesSelectItem.length > 0) {
				const selected = this.hierarchyPredicatesSelectItem[0];
				this.assetTypeForm.controls["predicateUid"].setValue(selected.value);
			}
		}
	}

	updateForm() {
		if (this.uid) {
			if (this.changeFormSub) {
				this.changeFormSub.unsubscribe();
			}

			this.isLoading = true;

			forkJoin(
				this.assetTypeService.GetAssetTypeByUid(this.uid),
				this.fieldsService.getAssetTypeFields(this.uid)
			).subscribe((results) => {
				const assetType = results[0];
				this.fieldTokens = [];
				if (results[1] && results[1].length) {
					results[1].forEach((field) => {
						const keyFieldTypes = ["Text", "Date", "DateTime", "Number", "Boolean", "Decimal", "Lookup", "Counter"];
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

				if (!assetType.DescriptionButtonName) {
					this.assetTypeForm.controls["descriptionButtonName"].setValue(this.defaultDescriptionButtonTextValue);
				}

				//ui label is `Collapsed by default` so we need to revert this boolean here
				this.assetTypeForm.controls["isDescriptionCollapsedByDefault"].setValue(!assetType.IsDescriptionVisibleByDefault);
				this.assetTypeForm.controls["backgroundColor"].setValue(assetType.IconStyle.BackColor);

				const colorCode = (assetType.IconStyle.BackColor ?? '') as string;
				const defColor = this.defaultColors.find((c) => c.title.toLowerCase() === colorCode.toLowerCase());
				this.assetTypeForm.controls['backgroundColorTextValue'].setValue(defColor ? defColor.value : $localize`Custom`);

				this.assetTypeForm.controls["icon"].setValue(assetType.IconStyle.Icon);
				this.selectedIcon = assetType.IconStyle.Icon;
				this.assetTypeForm.controls["useAsTransformation"].setValue(assetType.UseAsTransformation);

				this.assetTypeForm.controls["autoDisplayParent"].setValue(assetType.AutoDisplayParent ?? false);
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

				this.isEditFormUpdated = false;
				setTimeout(() => {
					this.changeFormSub = this.assetTypeForm.valueChanges.subscribe(() => {
						this.isEditFormUpdated = true;
					});
				}, 200);

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

			if (this.parentUid) {
				this.title = $localize`Add Child Asset Type`;
				this.subTitle = this.parentTypeName;
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

		//ui label is `Collapsed by default` so we need to revert this boolean here
		model.IsDescriptionVisibleByDefault = !this.assetTypeForm.get("isDescriptionCollapsedByDefault").value;

		model.BackgroundColor = this.assetTypeForm.get("backgroundColor").value;
		model.IconStyle = new IconStyle();
		model.IconStyle.Icon = this.assetTypeForm.get("icon").value;
		model.IconStyle.BackColor = this.assetTypeForm.get("backgroundColor").value;
		model.IconStyle.ForeColor = "#FFF";
		model.UseAsTransformation = this.assetTypeForm.get("useAsTransformation").value;

		if (this.assetTypeClass === AssetTypeClass.BusinessAsset || this.assetTypeClass === AssetTypeClass.TechnicalAsset) {
			model.AutoDisplayParent = this.assetTypeForm.get("autoDisplayParent").value;
		}
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

		let saveObs = this.assetTypeService.postAssetType(model);

		if (this.uid) {
			model.Uid = this.uid;
			saveObs = this.assetTypeService.putAssetType(model);
		}

		saveObs.subscribe((res) => {
			if (res) {
				this.onUpdated.emit(res);
				this.close();
			}
			this.savingInProgress = false;
		});
	}

	onColorSelect($event) {
		if (!$event) {
			return;
		}

		const selectedValue = this.defaultColors.find((x) => x.value === $event);
		if (selectedValue.label !== 'Custom') {
			this.assetTypeForm.controls["backgroundColor"].setValue(selectedValue.title);
		}
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
		return this.savingInProgress || this.assetTypeForm.invalid || (this.uid && !this.isEditFormUpdated);
	}

	get saveButtonLabel(): string {
		if (this.uid) {
			return $localize`Save Changes`;
		}
		else if (this.parentUid) {
			return $localize`Add Child Asset Type`;
		}
		else {
			return $localize`Add Asset Type`;
		}
	}

	get closeButtonLabel(): string {
		if (this.uid && this.isEditFormUpdated) {
			return $localize`Discard Changes`;
		}

		return $localize`Cancel`;
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

		if (this.formElement) {
			this.formElement.nativeElement.scrollTop = 0;
		}

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

	onIsDescriptionEnabledChange($event: boolean) {
		//if toggled to false, we need to set default value to button name to avoid validation errors
		if (!$event) {
			this.assetTypeForm.controls["descriptionButtonName"].setValue(this.defaultDescriptionButtonTextValue);
		}
	}

	lastVisitedTabIndex: number = 0;
	@HostListener('keydown.tab', ['$event'])
	onKeyDown(event: KeyboardEvent) {
		const target = event.target as HTMLElement;
		const editor = (this.primeEditor.el.nativeElement as HTMLElement).querySelector('.ql-editor') as HTMLElement;
		editor.focus();
		console.log(this.primeEditor.el.nativeElement as HTMLElement);
		if (target.tabIndex > 9) {
			this.lastVisitedTabIndex = target.tabIndex;
			const nextInput = this.getNextInputTab(this.lastVisitedTabIndex);
			if (nextInput) {
				nextInput.focus();
			}
		}
	}

	getNextInputTab(idx: number): HTMLElement {
		const nextTabIndex = idx + 10;
		if (nextTabIndex > 250) {
			return null;
		}
		const nextElement = document.querySelectorAll(`[tabindex='${nextTabIndex}']`);
		if (nextElement.length > 0) {
			console.log(nextElement[0]["tabIndex"]);
			const parentOffset = (nextElement[0] as HTMLElement).offsetParent;
			if (parentOffset) {
				return nextElement[0] as HTMLElement;
			}
			else {
				return this.getNextInputTab(nextTabIndex);
			}
		}
		else {
			return this.getNextInputTab(nextTabIndex);
		}
	}
}
