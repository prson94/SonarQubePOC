import { Input, Output, Component, OnChanges, SimpleChange, EventEmitter } from '@angular/core';

import { FieldsObservableService } from '../../../services/fieldsObservable.service';

import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { FieldTypeAPIModel, FieldTypeAPIModelField, FieldDisplayModel, FieldType } from '../../../models/fieldtype-api.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { AssetTypeClass } from '../../../models/asset.model';
import { AssetService } from '../../../services/asset.service';


@Component({
	selector: 'd3s-field-definition-tile',
	templateUrl: './field-definition.component.html',
	providers: [FieldsObservableService]
})

export class FieldDefinitionComponent extends BaseComponent implements OnChanges {
	@Input() title: string = $localize`Field Definition`;

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
	private fieldDefinitions = new Array<FieldTypeAPIModelField>();
	private fieldDisplayModel = new Array<FieldDisplayModel>();
	private selectedRow = new FieldDisplayModel();
	assetTypeClass: AssetTypeClass;

	private theDeleteCallback: Function;
	public hasKeyFields: boolean = false;

	ascendingLabel: string = $localize`Ascending`;
	descendingLabel: string = $localize`Descending`;

	constructor(
		private fieldsService: FieldsObservableService,
		private assetService: AssetService,
		private messagesService: MessagesObservableService,
		protected settingsService: CompanySettingsService
	) {
		super(settingsService);
		this.theDeleteCallback = this.deleteFieldType.bind(this);
	}

	ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		for (let p in changes) {
			if (p === 'actionTypeUid' || p === 'assetTypeUid' || p === 'relationshipTypeUid') {
				this.isEditing = false;
				this.isAdding = false;
				this.isDeleting = false;
			}
		}
		if (this.assetTypeUid) {
			this.assetService.getAssetTypeClassForAsset(this.assetTypeUid)
				.subscribe((res) => {
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
		this.fieldsService.getFieldsV2(this.assetTypeUid, this.actionTypeUid, this.relationshipTypeUid).subscribe(
			data => {
				this.fieldDefinitions = data;
				this.fieldDisplayModel = [];
				if (data) {
					this.fieldDisplayModel = data.map((field) => {
						let displayField = new FieldDisplayModel();
						let type = this.currentFieldType(field);
						displayField.Name = field.Name;
						displayField.FriendlyName = field.FriendlyName;
						displayField.Category = field.Category;
						displayField.FieldType = this.getDisplayTypeName(type);
						displayField.DisplayInColumn = field.Type[type].DisplayInColumn;
						displayField.IsListable = field.Type[type].IsListable;
						displayField.IsPartOfKey = field.Type[type].IsPartOfKey;
						displayField.SortOrder = field.Type[type].SortOrder;
						displayField.SortByAscending = field.Type[type].SortByAscending;
						displayField.ColumnOrder = field.Type[type].ColumnOrder;
						displayField.ShowIfEmpty = field.Type[type].ShowIfEmpty;
						displayField.IsRequired = field.Type[type].Validation != null ? field.Type[type].Validation.IsRequired : false;
						return displayField;
					});
				}
				this.checkKeyFields();
				this.selectedRow = null;
				this.isLoading = false;
			}
		);
	}

	get currentUid(): string {
		return this.actionTypeUid || this.assetTypeUid || this.relationshipTypeUid;
	}

	private checkKeyFields() {
		let foundKeyField = false;
		if (this.fieldDisplayModel && this.fieldDisplayModel.length > 0) {
			this.fieldDisplayModel.forEach(d => {
				if (!d.SortOrder)
					d.SortOrder = 0;
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
				AssetTypeClass.Rule].indexOf(this.assetTypeClass) != -1;
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
		}
	}
	edit(name: string): void {
		this.selectedRow = this.fieldDisplayModel.find(f => f.Name == name);
		this.isEditing = true;
		this.isDeleting = false;
		this.isAdding = false;
		this.onEdit.emit();
	}

	add(): void {
		this.selectedRow = null;
		this.isEditing = true;
		this.isDeleting = false;
		this.onAdd.emit();
	}

	delete(name: string): void {
		this.selectedRow = this.fieldDisplayModel.find(f => f.Name == name);
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
			res => {
				if (res != null && res.Success === true) {
					this.messagesService.showInfoMessage($localize`Success`, $localize`Field definition successfully removed.`);
					let index = this.fieldDisplayModel.findIndex(f => f.Name == this.selectedRow.Name);

					this.isDeleting = false;

					if (this.fieldDefinitions != null && this.fieldDefinitions.length > 0) {
						let ix = this.fieldDefinitions.findIndex(f => f.Name == this.selectedRow.Name);
						if (ix > -1) {
							this.fieldDefinitions.splice(ix, 1);
							this.fieldDefinitions = this.fieldDefinitions.slice();
						}
					}

					this.checkKeyFields();
					if (index >= 0 && index < this.fieldDisplayModel.length) {
						this.fieldDisplayModel.splice(index, 1);
					}

					this.onFieldsChanged.emit();
				} else {
					this.isDeleting = false;
					this.checkKeyFields();
				}
			}
		);

	}
	moveUp(field) {

		this.fieldsService.moveUp(this.currentUid, field.Name).subscribe(
			r => {
				let items = this.fieldDisplayModel.filter(x => x.Name == field.Name);
				if (items.length == 1) {
					let index = this.fieldDisplayModel.indexOf(items[0]);
					if (index > 0 && index < this.fieldDisplayModel.length)
						[this.fieldDisplayModel[index], this.fieldDisplayModel[index - 1]] = [this.fieldDisplayModel[index - 1], this.fieldDisplayModel[index]];
				}
			}
		);
	}

	moveDown(field) {

		this.fieldsService.moveDown(this.currentUid, field.Name).subscribe(
			r => {
				let items = this.fieldDisplayModel.filter(x => x.Name == field.Name);
				if (items.length == 1) {
					let index = this.fieldDisplayModel.indexOf(items[0]);
					if (index >= 0 && index < this.fieldDisplayModel.length - 1)
						[this.fieldDisplayModel[index], this.fieldDisplayModel[index + 1]] = [this.fieldDisplayModel[index + 1], this.fieldDisplayModel[index]];
				}
			}
		);
	}

	cancel() {
		this.isEditing = false;
		this.onCancel.emit();
	}

	showDeleteButtonByFieldType(fdm: FieldDisplayModel) {
		if (this.assetTypeClass === AssetTypeClass.DiagramAsset) {
			if (fdm.Name == 'Name' || fdm.Name == 'StepNo' || fdm.Name == 'GovernanceRole')
				return false;
		}
		return true;
	}
}
