import * as _ from 'lodash';
import {
	ChangeDetectionStrategy,
	ChangeDetectorRef,
	Component,
	EventEmitter,
	Input,
	OnChanges,
	OnInit,
	Output,
	ElementRef,
	ViewEncapsulation,
	ViewChildren,
	QueryList,
	HostListener,
	AfterViewChecked,
	SimpleChanges,
	ViewChild
} from '@angular/core';
import { AbstractControl, FormBuilder, FormControl, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { CompanySettings, CompanyImage, } from '../../../models/settings.model';
import { SemanticMatchType, SemanticSource, SemanticType } from '../../../models/semantic-type.model';
import { DataProfileService } from '../../../services/dataprofile.service';
import { FormMode } from '../../../models/form.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { BaseComponent } from '../../shared/base.component';
import { LocaleService } from '../../../services/locale.service';
import { PropertyGroupComponent } from '../../shared/controls/property-group/property-group.component';
import { AppSettingsEnum } from '../../../models/settings.model';
import { SiteNav } from '../../../models/site-menu.model';
import { StateService } from '../../../services/state.service';
import { SiteMenuService } from '../../../services/site-menu.service';
import { Table } from 'primeng/table';

@Component({
	selector: 'folder-editor',
	templateUrl: './admin-site-menu-folder-editor.component.html',
	providers: [DataProfileService, SiteMenuService],
	changeDetection: ChangeDetectionStrategy.OnPush,
	encapsulation: ViewEncapsulation.None,
	styleUrls: ['folder-editor.less']
})

export class AdminSiteMenuFolderEditorComponent extends BaseComponent implements OnChanges, OnInit, AfterViewChecked {
	@Input() navigationFolder: SiteNav;
	@Input() dataProfile: any = null;
	@Output() closeClick = new EventEmitter();
	@Output() saveClick = new EventEmitter();

	availableItems: SiteNav[] = [];
	isBuiltIn: boolean = false;
	statuses: any[];
	baseTypes: any;
	baseTypeOptions: any[];
	folderItems: SiteNav[] = [];
	formMode = FormMode.Default;
	matchTypes: any[];
	savingInProgress: boolean = false;
	hasHeader: boolean = false;
	iconType = 'icon';
	isInError: boolean = false;
	newFolderItems: SiteNav[] = [];
	folderModel: SiteNav;
	selection: SiteNav = null;
	folderForm: FormGroup;
	hasFormChanged: boolean = false;
	isInErrorMessage: string = "";
	advancedJson: string = "";

	locales: any[];
	isEdit: boolean = false;
	savingInProgressWithAddNew: boolean = false;
	isDuplicateQualifier: boolean = false;
	isJsonValid: boolean = true;
	semanticHelpURL: string;
	IsMenuPermissionsAdding: boolean = false;
	permissionMode: FormMode = FormMode.Default;
	simpleTextFilter: string = '';
	simpleTextFilterForExistingItems: string = '';

	//this contains user or groups selected (2nd table in permission property group)
	selectedPermissionAssets: any[] = [];

	permissionAssets: any[] = [];
	permissionAssetsTotalCount: number;
	_tempSelectedPermissionAssets: any[] = [];
	_selectedPermissionAsset: any[] = [];
	simpleTextFilterPermissionAssets: string = '';
	isPermissionAssetTableLoading: boolean = false;
	higlightedItem: any;

	private iconImage: CompanyImage = new CompanyImage();

	@ViewChildren(PropertyGroupComponent) propertyGroups: QueryList<PropertyGroupComponent>;

	constructor(
		private cdRef: ChangeDetectorRef,
		private formBuilder: FormBuilder,
		private messagesService: MessagesObservableService,
		private dataProfileService: DataProfileService,
		protected settingsService: CompanySettingsService,
		private siteMenuService: SiteMenuService,
		private stateService: StateService,
		private localService: LocaleService,
		private elRef: ElementRef
	) {
		super(settingsService);
	}

	ngOnInit(): void {
		this.isLoading = true;
		this.selection = null;
		this.newFolderItems = new Array<SiteNav>();
		this.loadFolderItems();

		this.folderForm = this.formBuilder.group({
			name: ['', [Validators.required, this.isEmptyString()]],
			description: null,
			effectiveDate: null,
			threshold: ['', [Validators.required]],
			priority: ['', [Validators.required]],
			matchType: null,
			baseType: null,
			qualifier: null,
			headerConfidence: null,
			minSamples: null,
			validValues: null,
			invalidValues: null,
			advancedJson: null,
			statuses: null,
			minMaxPresent: null,
			minimum: null,
			maximum: null,
			headerRegExp: null,
			regExpReturned: null,
			validLocales: null
		});
		this.populateTypeLists();
		setTimeout(() => {
			this.folderForm.valueChanges.subscribe((change) => {
				this.formMode = this.navigationFolder?.Name?.length > 0 ? FormMode.Editing : FormMode.Adding;
			});
			this.formMode = this.navigationFolder?.Name?.length > 0 ? FormMode.Editing : FormMode.Adding;
		}, 500);

		this.loadPermissionAssets();
	}

	ngOnChanges(changes: SimpleChanges): void {
		let c = changes;
		if (this.navigationFolder) {
			this.folderModel = _.cloneDeep(this.navigationFolder);
			this.isEdit = true;
		} else {
			this.isEdit = false;
			this.folderModel = new SiteNav();
		}
		this.cdRef.markForCheck();

		this.populateTypeLists();
	}

	populateModelFromDataProfile() {
	}

	public isFormValid(): boolean {
		if (!this.folderForm) {
			return false;
		}
		return this.folderForm.valid;
	}

	onSubmit(addAnother: boolean = false) {
		this.savingInProgress = true;

		if (addAnother) {
			this.savingInProgressWithAddNew = true;
		}

		this.clearInvalidFields();

		this.isLoading = true;

		switch (this.formMode) {
			case FormMode.Editing:
				this.selection.IconPayload = this.iconImage.dataUrl;
				this.siteMenuService.editFolder(this.selection)
					.subscribe((result) => {
						this.showMessageForResult(this.messagesService, result);
						this.siteMenuService.setSiteNavPermissions(this.selection);
						this.stateService.reloadLeftNavMenu();
						this.isLoading = false;
						this.formMode = FormMode.Default;
					});
				break;
			case FormMode.Adding:

				this.folderModel.IconPayload = this.iconImage.dataUrl;
				var model = {
					folder: this.folderModel,
					items: this.newFolderItems
				};

				this.siteMenuService.addFolder(model)
					.subscribe((result) => {
						this.showMessageForResult(this.messagesService, result);
						this.formMode = FormMode.Default;
						this.isLoading = false;
						this.stateService.reloadLeftNavMenu();
						this.siteMenuService.setSiteNavPermissions(this.selection);
						this.handleSaveComplete(result, addAnother);
					});
				break;
		}
	}

	handleSaveComplete(res: any, addAnother: boolean = false) {
		if (!(res?.status)) {
			let msg = `Successfully ${this.isEdit ? 'updated' : 'created'}`;
			this.showMessageForResult(this.messagesService, res, msg);
			this.savingInProgress = false;
			this.savingInProgressWithAddNew = false;
			if (addAnother) {
				this.folderModel = new SiteNav();
				this.folderForm.reset();
			}
			this.saveClick.emit({ item: res[0], action: `${this.isEdit ? 'edit' : 'new'}`, addAnother });
		}
		else {
			this.savingInProgress = false;
			this.savingInProgressWithAddNew = false;
			if (res?.status === 409) {
				this.isDuplicateQualifier = true;
			}
		}
		this.cdRef.markForCheck();
	}

	populateTypeLists() {
		this.isLoading = false;
	}

	isEmptyString(): ValidatorFn {
		type NewType = AbstractControl;

		return (control: NewType): { [key: string]: any } | null => {
			if (control.value === null || (typeof control.value === 'undefined')) {
				return {};
			}
			if ((control.value as string).trim() === '' && (control.value as string) !== '') {
				return {
					empty: { value: control.value }
				};
			}
			return null;
		};
	}

	isValid(): boolean {
		return true;
	}

	getMatchTypeDescription(matchType: any) {
		return this.matchTypes.filter((m) => (m.label === matchType.label))[0].description;
	}

	@HostListener('window:resize', ['$event'])
	onResize(event) {
		this.setFormHeight();
	}

	ngAfterViewChecked() {
		this.setFormHeight();
	}

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
		}

		//this.modalFormMaxHeight = groupsHeight > maxHeight ? maxHeight : groupsHeight;
		//this.cdRef.markForCheck();
	}

	expandChanged() {
		setTimeout(() => this.setFormHeight(), 10);
	}

	get cancelButtonText(): string {
		if (!this.isEdit) {
			return "Cancel";
		}

		if (this.hasFormChanged && this.isEdit) {
			return "Discard Changes";
		}

		return "Close";
	}

	getBaseTypeOptions() {
	}

	clearInvalidFields() {
		let allowedFields = ["name", "qualifier", "description", "threshold", "priority", "status", "matchType", "baseType", "source"];
	}

	validateMinMax() {
		return true;
	}

	changeIconType(e: any) {
		if (this.formMode === FormMode.Editing) {
			if (this.iconType === 'icon') {
				this.iconType = 'image';
				this.selection.Icon = null;
			} else {
				this.iconType = 'icon';
				this.selection.ImageIconUrl = null;
				this.selection.IconPayload = null;
				this.iconImage = new CompanyImage();
			}
		} else if (this.formMode === FormMode.Adding) {
			if (this.iconType === 'icon') {
				this.iconType = 'image';
				this.folderModel.Icon = null;
			} else {
				this.iconType = 'icon';
				this.folderModel.ImageIconUrl = null;
				this.folderModel.IconPayload = null;
				this.iconImage = new CompanyImage();
			}
		}

	}

	clearIcon() {
		this.iconImage = new CompanyImage();
		if (this.formMode == FormMode.Editing) {
			this.selection.ImageIconUrl = null;
		} else if (this.formMode == FormMode.Adding) {
			this.folderModel.ImageIconUrl = null;
		}
		this.onFileChange(null);
	}

	checkIfImg(value: string) {
		if (value && value.indexOf('/Content') !== -1) {
			return true;
		}
		else {
			return false;
		}
	}

	onFileChange(event): void {
		if (this.iconImage == null) {
			this.iconImage = new CompanyImage();
		}

		if (event == null) {
			this.iconImage.file = null;
			this.iconImage.setDataUrl();

			if (this.formMode === FormMode.Editing) {
				this.selection.IconPayload = null;
			} else if (this.formMode === FormMode.Adding) {
				this.folderModel.IconPayload = null;
			}

			return;
		}

		let target = event.target || event.srcElement;
		let files = target.files;

		if (files[0] != null) {
			if (files[0].size > (1024 * 1024)) {
				this.messagesService.showError('File too large.', `Navigation icon image upload failed - the file is too large. Please choose an image file (ideally in JPG format due to smaller file size) no bigger than 1MB. `);
				target.value = null;
				return;
			}
		}


		this.iconImage.file = files[0];
		this.iconImage.setDataUrl();
		if (this.formMode == FormMode.Editing) {
			this.selection.IconPayload = this.iconImage.dataUrl;
		} else if (this.formMode == FormMode.Adding) {
			this.folderModel.IconPayload = this.iconImage.dataUrl;
		}
	}

	loadFolderItems() {
		this.isLoading = true;

		if (this.selection == null || this.selection.ID == null) {
			return this.siteMenuService.getAvailableItems()
				.subscribe((r) => {
					this.availableItems = r;
					this.isLoading = false;
				});
		} else {

			return this.siteMenuService.getAvailableItems()
				.subscribe((r) => {
					this.availableItems = r;
					this.siteMenuService.getSiteNavFolderItems(this.selection.ID)
						.subscribe(s => {
							this.folderItems = s;
							this.folderItems = _.sortBy(this.folderItems, 'SortOrder'); // sort the folderItems by SortOrder
							this.isLoading = false;
							this.siteMenuService.getSiteNavFolderItems(this.selection.ID)
								.subscribe((s) => {
									this.folderItems = s;
									this.folderItems = _.sortBy(this.folderItems, 'SortOrder'); // sort the folderItems by SortOrder
									this.isLoading = false;
									this.stateService.reloadLeftNavMenu();
								})
						})
				})
		}
	}

	loadSiteNavPermissions(item: SiteNav) {
		this.isLoading = true;
		return this.siteMenuService.getSiteNavPermissions(item.ID)
			.subscribe(r => {
				item.Permissions = r;
				this.isLoading = false;
			});
	}

	menuPermissionsOnModeChange($event) {
		this.permissionMode = $event;
		this.IsMenuPermissionsAdding = ($event == FormMode.Adding);
	}

	addNewFolder(item: SiteNav) {
		let x = this.availableItems.findIndex((i) => i.ObjectID == item.ObjectID && i.Object == item.Object);
		let i = _.cloneDeep(this.availableItems.splice(x, 1)[0]);
		this.newFolderItems.push(i);
	}

	deleteNewFolder(item: SiteNav) {
		let x = this.availableItems.findIndex((i) => i.ObjectID == item.ObjectID && i.Object == item.Object);
		let i = _.cloneDeep(this.newFolderItems.splice(x, 1)[0]);
		this.availableItems.push(i);
	}

	imageUploadClick(event: any) {
		event.preventDefault();
		let el: HTMLElement = document.getElementById('imageUpload') as HTMLElement;
		el.click();
	}

	addToSelectedFolderItems(items: SiteNav[]) {
	}

	removeFromSelectedFolderItems(items: SiteNav[]) {
	}

	moveToTop(items: SiteNav[]) {
	}

	moveUp(items: SiteNav[]) {
	}

	moveDown(items: SiteNav[]) {
	}

	moveToBottom(items: SiteNav[]) {
	}

	lastLoadedEvent: any;
	loadPermissionAssets() {
		this.siteMenuService.getSiteNavPermissionsAssets()
			.subscribe((res) => {
				this.isPermissionAssetTableLoading = false;
				this.permissionAssetsTotalCount = res["total"];
				this.permissionAssets = res["results"];
				this.cdRef.markForCheck();
			});
	}
	addPermissionAssets() {
		if (!this.selectedPermissionAssets) {
			this.selectedPermissionAssets = [];
		}
		this._tempSelectedPermissionAssets.forEach((pa) => {
			if (this.selectedPermissionAssets.indexOf(pa) === -1) {
				this.selectedPermissionAssets.push(pa);
				this.permissionAssets = this.permissionAssets.filter((x) => x !== pa);
			}
		});
		this.permissionAssetsTotalCount = this.permissionAssets.length;
		this._tempSelectedPermissionAssets = [];
		this.cdRef.markForCheck();
	}

	removePermissionAssets() {
		this._selectedPermissionAsset.forEach((pa) => {
			if (this.permissionAssets.indexOf(pa) === -1) {
				this.permissionAssets.push(pa);
				this.selectedPermissionAssets = this.selectedPermissionAssets.filter((x) => x !== pa);
			}
		});
		this.permissionAssets = this.permissionAssets.sort((a, b) => a.Text.localeCompare(b.Text));
		this.permissionAssetsTotalCount = this.permissionAssets.length;
		this._selectedPermissionAsset = [];
		this.cdRef.markForCheck();
	}
}