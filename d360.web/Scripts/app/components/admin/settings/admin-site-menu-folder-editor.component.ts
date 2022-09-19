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
import { FormHelper } from '../../../models/form.model';
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
import { forkJoin } from 'rxjs';
import { Subject } from 'rxjs';
import { takeUntil, tap, startWith } from 'rxjs/operators';

@Component({
	selector: 'd3s-admin-site-menu-folder-editor',
	templateUrl: './admin-site-menu-folder-editor.component.html',
	providers: [DataProfileService, SiteMenuService],
	changeDetection: ChangeDetectionStrategy.OnPush,
	encapsulation: ViewEncapsulation.None,
	styleUrls: ['admin-site-menu-folder-editor.component.less']
})

export class AdminSiteMenuFolderEditorComponent extends BaseComponent implements OnChanges, OnInit {
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
	itemsFromTarget: SiteNav[] = [];
	_tempSelectedFolderItems: any[] = [];
	selectedItemsFromTarget: SiteNav[] = [];
	folderModel: SiteNav;
	folderNameIsFocused: boolean = false;
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
	requiredCount: number = 2;
	simpleTextFilter: string = '';
	simpleTextFilterForExistingItems: string = '';

	//this contains user or groups selected (2nd table in permission property group)
	selectedPermissionAssets: any[] = [];

	permissionAssets: any[] = [];
	permissionAssetsTotalCount: number;
	_tempSelectedPermissionAssets: any[] = [];
	_selectedPermissionAsset: any[] = [];
	isAvailableFolderItemsTableLoading: boolean = false;
	isPermissionAssetTableLoading: boolean = false;
	higlightedItem: any;
	previewAssetUid: any;
	previewAssetType: any;
	categories: any[] = [];

	labelAddFolder = $localize`Add Folder`;
	labelSaveChanges = $localize`Save Changes`;

	labelCancel = $localize`Cancel`;
	labelDiscard = $localize`Discard Changes`;

	hasFolderItems: boolean = false;

	private iconImage: CompanyImage = new CompanyImage();

	@ViewChildren(PropertyGroupComponent) propertyGroups: QueryList<PropertyGroupComponent>;

	delayedRefresh = _.debounce(() => {
		this.setRequiredCount();
		this.cdRef.markForCheck();
	}, 200);

	$destroy = new Subject();

	get isFirstItemFromTargetSelected(): boolean {
		return this.selectedItemsFromTarget.findIndex((selectedItem) => selectedItem.ObjectID === this.itemsFromTarget[0].ObjectID && selectedItem.Object === this.itemsFromTarget[0].Object) > -1;
	}

	get isLastItemFromTargetSelected(): boolean {
		const lastIndex: number = this.itemsFromTarget.length - 1;
		return this.selectedItemsFromTarget.findIndex((selectedItem): boolean => {
			return selectedItem.ObjectID === this.itemsFromTarget[lastIndex].ObjectID && selectedItem.Object === this.itemsFromTarget[lastIndex].Object; // eslint-disable-line
		}) > -1;
	}

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
		this.selection = null;
		this.folderForm = this.formBuilder.group({
			name: [null, [Validators.required, this.isEmptyString()]]
		});
		setTimeout(() => {
			this.folderForm.valueChanges.subscribe((change) => {
				this.formMode = this.navigationFolder?.Title?.length > 0 ? FormMode.Editing : FormMode.Adding;
			});
			this.formMode = this.navigationFolder?.Title?.length > 0 ? FormMode.Editing : FormMode.Adding;
		}, 500);
		this.folderForm.updateValueAndValidity();
	}

	ngOnChanges(changes: SimpleChanges): void {
		let c = changes;
		if (changes.navigationFolder && changes.navigationFolder.currentValue !== changes.navigationFolder.previousValue) {
			this._initialVersion = "";
			if (!this.navigationFolder?.Name.startsWith("#")) {
				this.hasFolderItems = true;
			}
		}

		if (this.navigationFolder) {
			this.folderModel = _.cloneDeep(this.navigationFolder);
			this.isEdit = true;
		} else {
			this.isEdit = false;
			this.folderModel = new SiteNav();
			this.folderModel.IsCustom = true;
		}

		this.$destroy.next();
		this.delayedRefresh.cancel();

		if (this.folderForm) {
			this.folderForm.valueChanges
				.pipe(
					startWith(null),
					takeUntil(this.$destroy),
					tap(() => this.delayedRefresh())
				)
				.subscribe();
		}

		this.loadTableData();

		this.cdRef.markForCheck();
	}

	enrichFolderData() {
		this.isAvailableFolderItemsTableLoading = true;
		this.isPermissionAssetTableLoading = true;

		if (this.folderModel.FullURL) {
			this.categories = [{
				label: null,
				items: [{ label: 'Custom', path: this.folderModel.FullURL, img: true }]
			}, ...this.categories
			];
			this.folderModel.Icon = this.categories[0].items[0];
		}

		forkJoin(this.siteMenuService.getSiteNavPermissions(this.navigationFolder.ID),
			this.siteMenuService.getSiteNavFolderItems(this.navigationFolder.ID)
		)
			.subscribe((results) => {
				let permissions = results[0];
				let folders = results[1];

				//preselect permission assets
				var selectedPermissions = {};
				permissions.forEach((item) => {
					selectedPermissions[item.Object + "|" + item.ObjectID] = 1;
				});
				this.permissionAssets.forEach((res) => {
					if (selectedPermissions[res["Value"]]) {
						this._tempSelectedPermissionAssets.push(res);
					}
				});
				this.addPermissionAssets();

				//preselect folder items
				this.selectedItemsFromTarget = [];
				folders.forEach((folder) => {
					this.itemsFromTarget.push(folder);
				});

				this.isAvailableFolderItemsTableLoading = false;
				this.isPermissionAssetTableLoading = false;
				this.setRequiredCount();
				this._initialVersion = JSON.stringify(this.getModel());
			});
	}

	public isFormValid(): boolean {
		if (!this.folderForm) {
			return false;
		}
		return this.folderForm.valid;
	}

	private getModel(): any {
		let folder: any = {};
		folder.Id = this.folderModel.ID;
		folder.Title = this.folderModel.Title;
		folder.Icon = this.folderModel.Icon;
		if (this.folderModel.IconPayload) {
			folder.IconPayload = this.folderModel.IconPayload;
		}

		if (this.folderModel.Icon && typeof this.folderModel.Icon !== "string") {
			let path: string = this.folderModel.Icon["path"];
			if (path.indexOf(this.folderModel.ImageIconUrl) !== -1) {
				folder.ImageIconUrl = this.folderModel.ImageIconUrl;
				folder.Icon = null;
				folder.IconPayload = null;
			}
		}

		folder.Items = this.itemsFromTarget;
		folder.Permissions = [];

		this.selectedPermissionAssets.forEach((p) => {
			var legacyData = (p["Value"] as string).split('|');
			folder.Permissions.push({ Name: p.Text, Object: legacyData[0], ObjectID: +legacyData[1], SiteNavID: 0 });
		});
		return folder;
	}

	save() {
		this.savingInProgress = true;

		this.clearInvalidFields();
		switch (this.formMode) {
			case FormMode.Editing:
				var folder = this.getModel();
				this.siteMenuService.editFolder(folder)
					.subscribe((result) => {
						this.showMessageForResult(this.messagesService, result);
						if (result?.type !== "error") {
							this.stateService.reloadLeftNavMenu();
							this.formMode = FormMode.Default;
							this.handleSaveComplete(result);
						}
						this.savingInProgress = false;
						this.cdRef.markForCheck();
					});
				break;
			case FormMode.Adding:
				var model = {
					folder: this.getModel(),
					items: this.itemsFromTarget,
				};

				this.siteMenuService.addFolder(model)
					.subscribe((result) => {
						this.showMessageForResult(this.messagesService, result);
						if (result?.type !== "error") {
							this.formMode = FormMode.Default;
							this.stateService.reloadLeftNavMenu();
							this.siteMenuService.setSiteNavPermissions(this.selection);
							this.handleSaveComplete(result);
						}
						this.savingInProgress = false;
						this.cdRef.markForCheck();
					});
				break;
		}
	}

	handleSaveComplete(res: any) {
		if (!(res?.status)) {
			this.saveClick.emit({ item: this.folderModel.Title, action: `${this.isEdit ? 'edit' : 'new'}` });
		}
		else {
			this.savingInProgress = false;
			if (res?.status === 409) {
				this.isDuplicateQualifier = true;
			}
		}
		this.cdRef.markForCheck();
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
			this.folderModel.ImageIconUrl = null;
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
				this.folderModel.IconPayload = null;
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

		this.iconType = 'image';
		this.iconImage.file = files[0];
		FormHelper.getDataUrl(files[0])
			.then((dataUrl) => {
				this.folderModel.IconPayload = dataUrl;
				if (!this.categories[0].label) {
					this.categories[0].items = [{ label: 'Custom', path: dataUrl, img: true }];
				} else {
					this.categories = [{
						label: null,
						items: [{ label: 'Custom', path: dataUrl, img: true }]
					}, ...this.categories
					];
				}
				this.folderModel.Icon = this.categories[0].items[0];
				this.cdRef.markForCheck();
			});
	}

	loadTableData() {
		this.isAvailableFolderItemsTableLoading = true;
		this.isPermissionAssetTableLoading = true;

		forkJoin(
			this.siteMenuService.getSiteNavPermissionsAssets(),
			this.siteMenuService.getAvailableItems()
		).subscribe((result) => {
			let perm = result[0];
			this.availableItems = result[1];
			this.permissionAssetsTotalCount = perm["total"];
			this.permissionAssets = perm["results"];
			this.isPermissionAssetTableLoading = false;
			this.isAvailableFolderItemsTableLoading = false;
			if (this.navigationFolder) {
				this.enrichFolderData();
			}

			this.cdRef.markForCheck();
		});
	}

	menuPermissionsOnModeChange($event) {
		this.permissionMode = $event;
		this.IsMenuPermissionsAdding = ($event == FormMode.Adding);
	}

	addNewFolder(item: SiteNav) {
		let x = this.availableItems.findIndex((i) => i.ObjectID == item.ObjectID && i.Object == item.Object);
		let i = _.cloneDeep(this.availableItems.splice(x, 1)[0]);
		this.itemsFromTarget.push(i);
		this.setRequiredCount();
	}

	deleteNewFolder(item: SiteNav) {
		let x = this.availableItems.findIndex((i) => i.ObjectID == item.ObjectID && i.Object == item.Object);
		let i = _.cloneDeep(this.itemsFromTarget.splice(x, 1)[0]);
		this.availableItems.push(i);
		this.setRequiredCount();
	}

	imageUploadClick(event: any) {
		event.preventDefault();
		let el: HTMLElement = document.getElementById('imageUpload') as HTMLElement;
		el.click();
	}

	addToSelectedFolderItems() {
		if (this._tempSelectedFolderItems.length > 0) {
			for (let j = 0; j < this._tempSelectedFolderItems.length; j++) {
				if (this.itemsFromTarget.indexOf(this._tempSelectedFolderItems[j]) === -1) { // eslint-disable-line
					this.itemsFromTarget.push(this._tempSelectedFolderItems[j]); // eslint-disable-line
					this.availableItems = this.availableItems.filter((x) => x != this._tempSelectedFolderItems[j]);
				}
			}
			this.itemsFromTarget = this.itemsFromTarget.sort((a, b) => a.Title.localeCompare(b.Title));
			this._tempSelectedFolderItems = [];
			this.setRequiredCount();
		}
		this.itemsFromTarget = [...this.itemsFromTarget];
		this.cdRef.markForCheck();
	}

	removeFromSelectedFolderItems() {
		if (this.selectedItemsFromTarget.length > 0) {
			for (let j = 0; j < this.selectedItemsFromTarget.length; j++) {
				let x = this.availableItems.findIndex((i) => i.ObjectID === this.selectedItemsFromTarget[j].ObjectID && i.Object === this.selectedItemsFromTarget[j].Object); // eslint-disable-line
				let y = this.itemsFromTarget.findIndex((i) => i.ObjectID === this.selectedItemsFromTarget[j].ObjectID && i.Object === this.selectedItemsFromTarget[j].Object); // eslint-disable-line
				if (y > -1) {
					let i = _.cloneDeep(this.itemsFromTarget.splice(y, 1)[0]);
					if (x === -1) {
						this.availableItems.push(i);
					}
				}
			}
			this.availableItems = this.availableItems.sort((a, b) => a.Title.localeCompare(b.Title));
			this.selectedItemsFromTarget = [];
			this.setRequiredCount();
		}
		this.cdRef.markForCheck();
	}

	moveToTop() {
		if (this.selectedItemsFromTarget.length > 0) {
			for (let j = 0; j < this.selectedItemsFromTarget.length; j++) {
				let x = this.itemsFromTarget.findIndex((i) => i.ObjectID === this.selectedItemsFromTarget[j].ObjectID && i.Object === this.selectedItemsFromTarget[j].Object); // eslint-disable-line
				this.itemsFromTarget.splice(j, 0, this.itemsFromTarget.splice(x, 1)[0]);
			}
		}
	}

	isMoveUpPossible(): boolean {
		return this.selectedItemsFromTarget?.length && !this.isFirstItemFromTargetSelected;
	}

	setIndexes(arrayOfObjects: object[]): void {
		arrayOfObjects.forEach((object, i) => {
			object['index'] = i;
		});
	}

	sortSelectedItemsFromTargetByIndexes(array: SiteNav[]): void {
		array.sort((a, b) => a.index - b.index);
	}

	reverseSortSelectedItemsFromTargetByIndexes(array: SiteNav[]): void {
		array.sort((a, b) => b.index - a.index);
	}

	moveUp() {
		this.setIndexes(this.itemsFromTarget);
		this.sortSelectedItemsFromTargetByIndexes(this.selectedItemsFromTarget);
		this.selectedItemsFromTarget.forEach((selectedItemFromTarget: SiteNav) => {
			this.itemsFromTarget.splice(selectedItemFromTarget.index - 1, 0, this.itemsFromTarget.splice(selectedItemFromTarget.index, 1)[0]);
		});
	}

	isMoveDownPossible(): boolean {
		return this.selectedItemsFromTarget?.length && !this.isLastItemFromTargetSelected;
	}

	moveDown() {
		this.setIndexes(this.itemsFromTarget);
		this.reverseSortSelectedItemsFromTargetByIndexes(this.selectedItemsFromTarget);
		this.selectedItemsFromTarget.forEach((selectedItemFromTarget: SiteNav) => {
			this.itemsFromTarget.splice(selectedItemFromTarget.index + 1, 0, this.itemsFromTarget.splice(selectedItemFromTarget.index, 1)[0]);
		});
	}

	moveToBottom() {
		if (this.selectedItemsFromTarget.length > 0) {
			for (let j = 0; j < this.selectedItemsFromTarget.length; j++) {
				let x = this.itemsFromTarget.findIndex((i) => i.ObjectID === this.selectedItemsFromTarget[j].ObjectID && i.Object === this.selectedItemsFromTarget[j].Object); // eslint-disable-line
				let newPosition = this.itemsFromTarget.length - j;
				this.itemsFromTarget.splice(newPosition - 1, 0, this.itemsFromTarget.splice(x, 1)[0]);
			}
		}
	}

	setRequiredCount() {
		this.requiredCount = 2;
		if (this.folderModel.Title?.length > 0) {
			this.requiredCount--;
		}
		if (this.itemsFromTarget?.length > 0 || !this.hasFolderItems) {
			this.requiredCount--;
		}
	}

	setRequiredFromNameInput($event) {
		this.requiredCount = 2;
		if ($event.data?.length > 0) {
			this.requiredCount--;
		}
		if (this.itemsFromTarget?.length > 0 || !this.hasFolderItems) {
			this.requiredCount--;
		}
		this.cdRef.markForCheck();
	}

	focusRequired(event) {
		event.stopPropagation();
		if (this.requiredCount == 0) {
			return;
		}
		if (!this.folderForm.get('title')?.errors?.empty && !this.folderNameIsFocused) {
			this.elRef.nativeElement.querySelectorAll("[name = folderNameInput]")[0].focus();
			this.folderNameIsFocused = true;
		} else if (!this.itemsFromTarget || this.itemsFromTarget?.length === 0) {
			this.elRef.nativeElement.querySelector("[name = availableFolderItemsSearchField]").querySelectorAll(".ig-input")[0].focus();
			this.folderNameIsFocused = false;
		}
	}

	lastLoadedEvent: any;
	addPermissionAssets() {
		if (!this.selectedPermissionAssets) {
			this.selectedPermissionAssets = [];
		}
		this._tempSelectedPermissionAssets.forEach((pa) => {
			if (this.selectedPermissionAssets.indexOf(pa) === -1) {
				this.selectedPermissionAssets.push(pa);
				this.permissionAssets = this.permissionAssets.filter((x) => x !== pa);
				if (pa.uid === this.previewAssetUid) {
					this.previewAssetUid = '';
				}
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

	selectAllAvailableFolderItems($event, table: Table) {
		this._tempSelectedFolderItems = [];
		for (let i = table.first; i < table.first + table.rows; i++) {
			this._tempSelectedFolderItems.push(this.availableItems.find((item) => item.ID === table.selection[i]?.ID)); // eslint-disable-line
		}
	}

	selectAllSelectedFolderItems($event, table: Table) {
		this.selectedItemsFromTarget = [];
		for (let i = 0; i < table.selection.length; i++) {
			let x: number = this.itemsFromTarget.findIndex((item) => item.ObjectID === table.selection[i].ObjectID && item.Object === table.selection[i].Object); // eslint-disable-line
			this.selectedItemsFromTarget.push(_.cloneDeep(this.itemsFromTarget[x])); // eslint-disable-line
		}
	}

	headerSelectAll($event, table: Table) {
		this._tempSelectedPermissionAssets = [];
		for (let i = table.first; i < table.first + table.rows; i++) {
			this._tempSelectedPermissionAssets.push(this.permissionAssets.find((item) => item.uid == table.selection[i].uid));
		}
	}

	private _initialVersion: string = '';
	get hasChanges(): boolean {
		if (!this.isEdit) {
			return false;
		}
		return this._initialVersion !== JSON.stringify(this.getModel());
	}

	get isSaveDisabled(): boolean {
		return (this.isEdit && !this.hasChanges) || this.savingInProgress || this.folderModel === null || this.folderModel.Title === null || this.folderModel.Title === '' || ((this.itemsFromTarget === null || this.itemsFromTarget.length < 1) && this.hasFolderItems);
	}
}