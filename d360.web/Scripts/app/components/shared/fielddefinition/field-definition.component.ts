import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChange, ViewChild, ViewEncapsulation } from '@angular/core';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { FieldDisplayModel, FieldType, FieldTypeAPIModelField } from '../../../models/fieldtype-api.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { AssetTypeClass } from '../../../models/asset.model';
import { AssetService } from '../../../services/asset.service';
import { RelationshipsService } from '../../../services/relationships.service';
import { SidePanelService } from '../../../services/side-panel.service';
import { IOutputData } from 'angular-split';
import { AdvancedFilterFieldType, Filters, LookupValuesAPIModel } from '../../assets-grid/advanced-filtering/advanced-filtering.models';
import { Observable, of } from 'rxjs';
import { UiAdvancedFiltering } from '../../../services/ui-advanced-filtering.service';
import { PopupMenu } from '../controls/popup-menu/popup-menu.component';
import { Table } from 'primeng/table';
import { AdvancedFilteringComponent } from '../../assets-grid/advanced-filtering/advanced-filtering.component';

/*global $localize*/

@Component({
	selector: 'd3s-field-definition-tile',
	templateUrl: './field-definition.component.html',
	styleUrls: ['field-definition.component.less'],
	providers: [FieldsObservableService],
	encapsulation: ViewEncapsulation.None,
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class FieldDefinitionComponent extends BaseComponent implements OnChanges {
	@Input() title: string = $localize`Field Definition`;

	@Input() showTitle = true;
	@Input() actionTypeUid: string;
	@Input() assetTypeUid: string;
	@Input() relationshipTypeUid: string;

	@Input() showAddButton: boolean = true;
	@Input() showEditButton: boolean = true;
	@Input() showDeleteButton: boolean = true;

	@Input() showIsListable: boolean = true;
	@Input() showIsPartOfKey: boolean = true;
	@Input() showShowInDetailTile: boolean = true;
	@Input() showPersistInFilters: boolean = true;
	@Input() showAddToSearch: boolean = false;

	@Input() showDisplayInColumn: boolean = false;

	@Input() objectName: string = "";

	@Output() onEdit = new EventEmitter();
	@Output() onAdd = new EventEmitter();
	@Output() onDelete = new EventEmitter();
	@Output() onCancel = new EventEmitter();
	@Output() onFieldsChanged = new EventEmitter();

	@Input() isEditing = false;
	@Input() isAdding = false;
	@Input() isDeleting = false;

	@Input() supportsPrimaryFilterOption: boolean = false;
	@Input() allowSingleSegmentPath: boolean = true;

	public dataCyPrefix: string = 'FieldType_';
	fieldDefinitions = new Array<FieldTypeAPIModelField>();
	private fieldDisplayModel = new Array<FieldDisplayModel>();
	private nonFilteredFieldDisplayModel = new Array<FieldDisplayModel>();
	selectedRow = new FieldDisplayModel();
	assetTypeClass: AssetTypeClass;

	private theDeleteCallback: Function;
	public hasKeyFields: boolean = false;

	ascendingLabel: string = $localize`Ascending`;
	descendingLabel: string = $localize`Descending`;


	sidePanelStorageKey: string = '';
	selectedItem: Record<string, object>;

	sidePanelOpen = false;
	selectedForInfoPanel: unknown;
	columnWidthMinSize = 150;
	tableWidth = 0;

	advancedFilters: Filters;
	simpleFilter: string;

	isReorderingLocked: boolean = false;
	reorderingLockedText: string = $localize`Items can be rearranged only in the default view. Use the reset button to clear any search, filter, or sorting applied.`;

	@ViewChild('dt', { static: false }) tableEl: Table;
	@ViewChild('advancedFilter', { static: false }) advFilter: AdvancedFilteringComponent;

	constructor(
		private fieldsService: FieldsObservableService,
		private relationshipService: RelationshipsService,
		private assetService: AssetService,
		private messagesService: MessagesObservableService,
		protected settingsService: CompanySettingsService,
		public sidePanelService: SidePanelService,
		private uiAdvancedFiltering: UiAdvancedFiltering,
		private cdRef: ChangeDetectorRef
	) {
		super(settingsService);
		this.theDeleteCallback = this.deleteFieldType.bind(this);
	}

	ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		for (const p in changes) {
			if (p === 'actionTypeUid' || p === 'assetTypeUid' || p === 'relationshipTypeUid') {
				this.isEditing = false;
				this.isAdding = false;
				this.isDeleting = false;
				this.sidePanelStorageKey = "field_type_side_panel_" + this.assetTypeUid + this.actionTypeUid + this.relationshipTypeUid;
			}
		}
		if (this.assetTypeUid) {
			this.isLoading = true;
			this.assetService.getAssetTypeClassForAsset(this.assetTypeUid)
				.subscribe((res) => {
					this.isLoading = false;
					this.assetTypeClass = res;
					this.load();
				});
		}
		else {
			this.assetTypeClass = null;
			this.load();
		}
	}

	load(): void {
		if (this.relationshipTypeUid === "IntersectType") {
			this.showIsPartOfKey = false;
		}
		if (this.assetTypeClass === AssetTypeClass.Group) {
			this.showIsPartOfKey = false;
			this.showShowInDetailTile = false;
			this.showAddToSearch = true;
			this.showPersistInFilters = false;
		}
		this.isLoading = true;
		this.hasKeyFields = false;

		if (this.relationshipTypeUid) {
			this.relationshipService.getRelationshipType(this.relationshipTypeUid)
				.subscribe((res) => {
					if (res.length > 0) {
						var type = res[0];
						var relationshipName = type.Subject.Name + ' [' + type.Predicate.Name + '] ' + type.Object.Name;
						this.title = $localize`Field Definition for ${relationshipName}`;
					}
				});
		}

		this.fieldsService.getFieldsV2(this.assetTypeUid, this.actionTypeUid, this.relationshipTypeUid).subscribe(
			(data) => {
				this.fieldDefinitions = data;
				this.fieldDisplayModel = [];
				if (data) {
					this.fieldDisplayModel = data.map((field) => {
						const displayField = new FieldDisplayModel();
						const type = this.currentFieldType(field);
						displayField.AssetTypeUid = field["AssetTypeUid"];
						displayField.Name = field.Name;
						displayField.FriendlyName = field.FriendlyName;
						displayField.Category = field.Category ?? $localize`General`;
						displayField.FieldType = this.getDisplayTypeName(type);
						displayField.FieldTypeValue = type;
						displayField.DisplayInColumn = field.Type[`${type}`].DisplayInColumn ?? false;
						displayField.IsListable = field.Type[`${type}`].IsListable;
						displayField.IsPartOfKey = field.Type[`${type}`].IsPartOfKey ?? false;
						displayField.SortOrder = field.Type[`${type}`].SortOrder;
						displayField.SortByAscending = field.Type[`${type}`].SortByAscending;
						displayField.ColumnOrder = field.Type[`${type}`].ColumnOrder;
						displayField.ShowIfEmpty = field.Type[`${type}`].ShowIfEmpty ?? false;
						displayField.IsRequired = field.Type[`${type}`].Validation != null ? field.Type[`${type}`].Validation.IsRequired : false;

						displayField.DisplayDescription = field.Type[`${type}`]?.Description?.Display ?? "";
						displayField.FormDescription = field.Type[`${type}`]?.Description?.Form ?? "";
						displayField.AddToSearchResults = field.Type[`${type}`]?.Search?.AddToResult ?? false;
						displayField.AllowMultipleItems = field.Type[`${type}`]?.List?.AllowMultipleValues ?? false;
						displayField.EditableOnUI = field.Type[`${type}`]?.IsEditable ?? false;
						displayField.ShowInDetailsTab = field.Type[`${type}`]?.IsDisplayable ?? false;
						displayField.PersistInFilters = field.Type[`${type}`]?.IsPrimaryFilter ?? false;

						displayField.ColumnWidth = field.Type[`${type}`]?.ColumnWidth;

						if (type === 'Lookup') {
							displayField.LookupTypeName = field.Type[`${type}`].List.Class + ": " + field.Type[`${type}`].List.TypeName;
							displayField.LookupDisplayFormat = field.Type[`${type}`].Format.Display;
							displayField.LookupEditFormat = field.Type[`${type}`].Format.Edit;
						}
						displayField.FieldTypeREF = field;
						return displayField;
					});

					this.updateMenuItems();
					this.nonFilteredFieldDisplayModel = JSON.parse(JSON.stringify(this.fieldDisplayModel));

					this.tableWidth = this.tableEl.el.nativeElement.getBoundingClientRect().width;
				}

				this.checkKeyFields();
				this.selectedRow = null;
				this.isLoading = false;
				this.cdRef.markForCheck();
			}
		);
	}

	// ignore complexity codacy issue
	// eslint-disable-next-line
	onMenuItemSelect(item: FieldDisplayModel, $event) {
		switch ($event.action) {
			case 'info':
				this.selectedRow = item;
				this.sidePanelService.setSidePanelState({ expanded: true });
				break;
			case 'edit':
				this.edit(item);
				break;
			case 'delete':
				this.delete(item.Name);
				break;
			case 'movetop':
				this.moveToPosition(item, 0);
				break;
			case 'moveup':
				this.moveUp(item);
				break;
			case 'movedown':
				this.moveDown(item);
				break;
			case 'movebottom':
				this.moveToPosition(item, this.nonFilteredFieldDisplayModel.length);
				break;
		}
	}

	reset() {
		this.simpleFilter = "";
		this.advFilter.clearFilters();
		this.tableEl.reset();
		this.load();
	}

	private updateMenuItems() {
		let position = 0;
		const keyFieldsCount = this.fieldDisplayModel.filter((x) => x.IsPartOfKey).length;

		this.isReorderingLocked = (typeof this.simpleFilter !== 'undefined' && this.simpleFilter !== '')
			|| (this.advancedFilters && this.advancedFilters.filter !== '');

		this.isReorderingLocked = this.isReorderingLocked || (typeof this.tableEl.sortField !== 'undefined' && this.tableEl.sortField !== null);

		// ignore complexity codacy issue
		// eslint-disable-next-line
		this.fieldDisplayModel.forEach((item) => {
			position++;
			const menuItems = [];
			menuItems.push({ title: $localize`View Information`, action: 'info' });
			menuItems.push({ title: $localize`Edit`, action: 'edit' });

			const isDiagramAssetPage = this.assetTypeClass === AssetTypeClass.DiagramAsset;

			if (this.fieldDisplayModel.length > 1) {
				if (keyFieldsCount === 1 && item.IsPartOfKey) {
					menuItems.push({ title: $localize`Delete`, disabled: true, tooltip: $localize`You cannot delete this field. There must be at least one key field defined.` });
				}
				else if (isDiagramAssetPage && ['Name', 'StepNo', 'GovernanceRole'].indexOf(item.Name) > -1) {
					menuItems.push({ title: $localize`Delete`, disabled: true, tooltip: $localize`Default fields cannot be deleted.` });
				}
				else {
					menuItems.push({ title: $localize`Delete`, action: 'delete' });
				}

				let positionDisabled = false;
				let positionTooltip = '';

				if (this.isReorderingLocked) {
					positionDisabled = true;
					positionTooltip = this.reorderingLockedText;
				}

				if (position !== 1) {
					menuItems.push({ title: $localize`Move To Top`, disabled: positionDisabled, tooltip: positionTooltip, action: 'movetop' });
					menuItems.push({ title: $localize`Move Up`, disabled: positionDisabled, tooltip: positionTooltip, action: 'moveup' });
				}
				if (position !== this.fieldDisplayModel.length) {
					menuItems.push({ title: $localize`Move Down`, disabled: positionDisabled, tooltip: positionTooltip, action: 'movedown' });
					menuItems.push({ title: $localize`Move To Bottom`, disabled: positionDisabled, tooltip: positionTooltip, action: 'movebottom' });
				}
			}

			item.MenuItems = menuItems;
		});
	}

	get currentUid(): string {
		return this.actionTypeUid || this.assetTypeUid || this.relationshipTypeUid;
	}

	private checkKeyFields() {
		let foundKeyField = false;
		if (this.fieldDisplayModel && this.fieldDisplayModel.length > 0) {
			this.fieldDisplayModel.forEach((d) => {
				if (!d.SortOrder) { d.SortOrder = 0; }
				if (d.IsPartOfKey) {
					foundKeyField = true;
				}
			});
			this.hasKeyFields = foundKeyField;
		}
		else {
			this.hasKeyFields = false;
		}
	}

	currentFieldType(item: FieldTypeAPIModelField): string {
		return Object.keys(item.Type).filter((key) => { return item.Type[key] !== null; })[0];
	}

	CheckObjectType() {
		if (this.assetTypeClass) {
			return [AssetTypeClass.BusinessAsset,
			AssetTypeClass.TechnicalAsset,
			AssetTypeClass.Policy,
			AssetTypeClass.Model,
			AssetTypeClass.Rule].indexOf(this.assetTypeClass) !== -1;
		}
	}
	getDisplayTypeName(name: string): string {
		switch (name) {
			case "Boolean": return $localize`True/False`;
			case "ComputedRelationshipLookup":
			case "ComplexRelationLookup": return $localize`Relation Lookup`;
			case "Counter": return $localize`Counter`;
			case "Date": return $localize`Date`;
			case "DateTime": return $localize`Date Time`;
			case "Decimal": return $localize`Decimal`;
			case "ComputedRelationshipField":
			case "FieldFromRelationship": return $localize`Field from Relationship`;
			case "Html": return $localize`Html`;
			case "JSON": return $localize`JSON`;
			case "JsonElement": return $localize`JsonElement`;
			case "Link": return $localize`Link`;
			case "Lookup": return $localize`List`;
			case "Number": return $localize`Number`;
			case "ComputedOwnershipLookup":
			case "OwnershipLookup": return $localize`Ownership Lookup`;
			case "Path": return $localize`Asset Path`;
			case "ComputedRelationshipReferenceList":
			case "RefListRelationship": return $localize`Reference Item List from Relationship`;
			case "Relationship": return $localize`Relationship`;
			case "Score": return $localize`Score`;
			case "Tag": return $localize`Tag`;
			case "Text": return $localize`Simple Text`;
			case "System": return $localize`System`;
		}
	}


	add(): void {
		this.selectedRow = null;
		this.isEditing = true;
		this.isDeleting = false;
		this.onAdd.emit();
	}

	delete(name: string): void {
		this.selectedRow = this.fieldDisplayModel.find((f) => f.Name === name);
		this.isEditing = false;
		this.isDeleting = true;
		this.isAdding = false;
		this.onDelete.emit();
	}

	editComplete(event) {
		this.isEditing = false;
		this.onCancel.emit();
		this.load();
		this.onFieldsChanged.emit();
	}

	deleteFieldType(name: string) {

		this.fieldsService.deleteFieldType(this.selectedRow.Name, this.assetTypeUid, this.actionTypeUid, this.relationshipTypeUid).subscribe(
			(res) => {
				if (res != null && res.Success === true) {
					this.messagesService.showInfoMessage($localize`Success`, $localize`Field definition successfully removed.`);
					const index = this.fieldDisplayModel.findIndex((f) => f.Name === this.selectedRow.Name);

					this.isDeleting = false;

					if (this.fieldDefinitions != null && this.fieldDefinitions.length > 0) {
						const ix = this.fieldDefinitions.findIndex((f) => f.Name === this.selectedRow.Name);
						if (ix > -1) {
							this.fieldDefinitions.splice(ix, 1);
							this.fieldDefinitions = this.fieldDefinitions.slice();
						}
					}

					this.checkKeyFields();
					if (index >= 0 && index < this.fieldDisplayModel.length) {
						this.fieldDisplayModel.splice(index, 1);
					}
					this.load();
					this.onFieldsChanged.emit();
				} else {
					this.isDeleting = false;
					this.checkKeyFields();
				}
				this.cdRef.markForCheck();
			}
		);

	}
	onRowReorder($event) {
		const dropIndex = $event.dropIndex;
		const dragIndex = $event.dragIndex;
		const moveField = this.fieldDisplayModel[`${dragIndex}`];
		if (moveField) {
			this.moveToPosition(moveField, dropIndex + 1);
		}
	}

	moveUp(field: FieldDisplayModel) {
		this.isLoading = true;
		this.fieldsService.moveUp(this.currentUid, field.Name).subscribe(
			(orderedColumns) => {
				orderedColumns.forEach((ft) => {
					this.fieldDisplayModel.find((f) => f.Name === ft.Name).ColumnOrder = ft.ColumnOrder;
				});
				this.tableEl.sortField = "ColumnOrder";
				this.tableEl.sortSingle();
				this.isLoading = false;
				this.cdRef.markForCheck();
			}
		);
	}

	moveDown(field: FieldDisplayModel) {
		this.isLoading = true;
		this.fieldsService.moveDown(this.currentUid, field.Name).subscribe(
			(orderedColumns) => {
				orderedColumns.forEach((ft) => {
					this.fieldDisplayModel.find((f) => f.Name === ft.Name).ColumnOrder = ft.ColumnOrder;
				});
				this.tableEl.sortField = "ColumnOrder";
				this.tableEl.sortSingle();
				this.isLoading = false;
				this.cdRef.markForCheck();
			}
		);
	}

	moveToPosition(field: FieldDisplayModel, position: number) {
		this.isLoading = true;
		this.fieldsService.moveToPosition(this.currentUid, field.Name, position).subscribe(
			(orderedColumns) => {
				orderedColumns.forEach((ft) => {
					this.fieldDisplayModel.find((f) => f.Name === ft.Name).ColumnOrder = ft.ColumnOrder;
				});
				this.tableEl.sortField = "ColumnOrder";
				this.tableEl.sortSingle();
				this.isLoading = false;
				this.cdRef.markForCheck();
			}
		);
	}


	edit(field: FieldDisplayModel): void {
		this.selectedRow = this.fieldDisplayModel.find((f) => f.Name === field.Name);
		this.isEditing = true;
		this.isDeleting = false;
		this.isAdding = false;
		this.onEdit.emit();
	}

	cancel() {
		this.isEditing = false;
		this.onCancel.emit();
	}

	showDeleteButtonByFieldType(fdm: FieldDisplayModel) {
		if (this.assetTypeClass === AssetTypeClass.DiagramAsset) {
			if (fdm.Name === 'Name' || fdm.Name === 'StepNo' || fdm.Name === 'GovernanceRole') { return false; }
		}

		if (fdm.FieldType === "System") {
			return false;
		}
		return true;
	}

	getSidePanelWidth(): number {
		return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.sidePanelStorageKey);
	}

	getSidePanelMaxWidth(): number {
		return this.sidePanelService.getSidePanelMaxWidth(this.sidePanelOpen);
	}

	getSidePanelMinWidth(): number {
		return this.sidePanelService.getSidePanelMinWidth(this.sidePanelOpen);
	}

	onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData): void {
		this.sidePanelService.onSidePanelDragEnd(sidePanelStorageKey, event);
	}

	filterFieldList$: Observable<AdvancedFilterFieldType[]> = of([
		{
			Name: 'FriendlyName',
			FriendlyName: $localize`Field Name`,
			Type: new FieldType("Text"),
			Category: "",
			RemovePopulatedOperator: true
		},
		{
			Name: 'Name',
			FriendlyName: $localize`API Name`,
			Type: new FieldType("Text"),
			Category: "",
			RemovePopulatedOperator: true
		},
		{
			Name: 'Category',
			FriendlyName: $localize`Category`,
			Type: new FieldType("Text"),
			Category: "",
			RemovePopulatedOperator: true
		},
		{
			Name: 'DisplayDescription',
			FriendlyName: $localize`Display Description`,
			Type: new FieldType("Text"),
			Category: ""
		},
		{
			Name: 'FormDescription',
			FriendlyName: $localize`Form Description`,
			Type: new FieldType("Text"),
			Category: ""
		},
		{
			Name: 'IsListable',
			FriendlyName: $localize`Listable`,
			Type: new FieldType("Boolean"),
			Category: "",
			RemovePopulatedOperator: true
		},
		{
			Name: 'AddToSearchResults',
			FriendlyName: $localize`Add to Search Results`,
			Type: new FieldType("Boolean"),
			Category: "",
			RemovePopulatedOperator: true
		},
		{
			Name: 'AllowMultipleItems',
			FriendlyName: $localize`Allow Multiple Items`,
			Type: new FieldType("Boolean"),
			Category: "",
			RemovePopulatedOperator: true
		},
		{
			Name: 'EditableOnUI',
			FriendlyName: $localize`Editable On UI`,
			Type: new FieldType("Boolean"),
			Category: "",
			RemovePopulatedOperator: true
		},
		{
			Name: 'IsPartOfKey',
			FriendlyName: $localize`Key Field`,
			Type: new FieldType("Boolean"),
			Category: "",
			RemovePopulatedOperator: true
		},
		{
			Name: 'IsRequired',
			FriendlyName: $localize`Required`,
			Type: new FieldType("Boolean"),
			Category: "",
			RemovePopulatedOperator: true
		},
		{
			Name: 'ShowInDetailsTab',
			FriendlyName: $localize`Show In Details Tab`,
			Type: new FieldType("Boolean"),
			Category: "",
			RemovePopulatedOperator: true
		},
		{
			Name: 'DisplayInColumn',
			FriendlyName: $localize`Display In Column`,
			Type: new FieldType("Boolean"),
			Category: "",
			RemovePopulatedOperator: true
		},
		{
			Name: 'ShowIfEmpty',
			FriendlyName: $localize`Show If Empty`,
			Type: new FieldType("Boolean"),
			Category: "",
			RemovePopulatedOperator: true
		},
		{
			Name: 'PersistInFilters',
			FriendlyName: $localize`Persist In Filters`,
			Type: new FieldType("Boolean"),
			Category: "",
			RemovePopulatedOperator: true
		},
		{
			Name: 'FieldType',
			Type: new FieldType("Lookup"),
			FriendlyName: $localize`Type`,
			Category: "",
			ValueLoader: this.getFilterValuesForFieldType.bind(this),
			RemovePopulatedOperator: true
		},
	]);

	getFilterValuesForFieldType(): Observable<LookupValuesAPIModel> {
		const types: string[] = [
			$localize`True/False`,
			$localize`Relation Lookup`,
			$localize`Counter`,
			$localize`Date`,
			$localize`Date Time`,
			$localize`Decimal`,
			$localize`Field from Relationship`,
			$localize`Html`,
			$localize`JSON`,
			$localize`JsonElement`,
			$localize`Link`,
			$localize`List`,
			$localize`Number`,
			$localize`Ownership Lookup`,
			$localize`Asset Path`,
			$localize`Reference Item List from Relationship`,
			$localize`Relationship`,
			$localize`Score`,
			$localize`Tag`,
			$localize`Simple Text`,
			$localize`System`];

		if (types.length === 1 && types[0] === '') {
			return of({
				items: [],
				count: 0
			});
		} else {
			return of({
				items: types,
				count: types.length
			});
		}
	}

	advancedFiltersChanged(event: Filters): void {
		this.advancedFilters = event;
		this.fieldDisplayModel = this.uiAdvancedFiltering.runFiltering(this.nonFilteredFieldDisplayModel, event);
		this.updateMenuItems();
	}


	positionContextMenu(
		$event: MouseEvent, container: HTMLElement, floatMenu: PopupMenu, assetGridTools: HTMLElement
	): void {
		if (!assetGridTools.contains(<Node>$event.target) && !this.isElementLink(<HTMLElement>$event.target)) {
			container.style.top = `${$event['layerY']}px`;
			container.style.left = `${$event['layerX']}px`;
			floatMenu.toggle($event);
			$event.preventDefault();
		}
	}

	private isElementLink(element: HTMLElement): boolean {
		while (element.parentElement) {
			if (element.tagName === 'A') { return true; }
			element = element.parentElement;
		}
		return false;
	}

	hasPartOfKey(field: FieldDisplayModel) {
		const excludeTypes: string[] = ['Path', 'ComputedRelationshipField', 'Json', 'Link', 'ComputedOwnershipLookup', 'ComputedRelationshipReferenceList', 'ComputedRelationshipLookup', 'Relationship', 'Score', 'Tag'];
		if (excludeTypes.indexOf(field.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}
	hasRequired(field: FieldDisplayModel) {
		const excludeTypes: string[] = ['Path', 'Counter', 'ComputedRelationshipField', 'Json', 'ComputedOwnershipLookup', 'ComputedRelationshipReferenceList', 'ComputedRelationshipLookup', 'Relationship', 'Score', 'Tag'];
		if (excludeTypes.indexOf(field.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}
	hasDisplayInColumn(field: FieldDisplayModel) {
		const excludeTypes: string[] = ['Json', 'ComputedOwnershipLookup', 'ComputedRelationshipReferenceList', 'ComputedRelationshipLookup', 'Tag'];
		if (excludeTypes.indexOf(field.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}
	hasShowIfEmpty(field: FieldDisplayModel) {
		const excludeTypes: string[] = ['Path', 'Json', 'Tag'];
		if (excludeTypes.indexOf(field.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}
	hasIsListable(field: FieldDisplayModel) {
		const excludeTypes: string[] = ['ComputedRelationshipReferenceList', 'ComputedRelationshipLookup', 'Relationship'];
		if (excludeTypes.indexOf(field.FieldTypeValue) > -1) {
			return false;
		}
		return true;
	}
}
