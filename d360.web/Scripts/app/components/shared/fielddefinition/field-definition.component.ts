import { Input, Output, Component, OnChanges, SimpleChange, EventEmitter } from '@angular/core';

import { FieldDefinition, IFieldsService, FieldType } from '../../../models/fields.model';

import { FieldsObservableService } from '../../../services/fieldsObservable.service';

import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { FieldTypeAPIModel, FieldTypeAPIModelField } from '../../../models/fieldtype-api.model';
import { type } from 'os';

@Component({
    selector: 'd3s-field-definition-tile',
    templateUrl: './field-definition.component.html',
    providers: [ FieldsObservableService ]
})

export class FieldDefinitionComponent extends BaseComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() currentUid: string;
    @Input() title: string = 'Field Definition';

    @Input() actionTypeUid: string;
    @Input() assetTypeUid: string;
    @Input() relationshipTypeUid: string;

    @Input() showAddButton: boolean = true;
    @Input() showEditButton: boolean = true;
    @Input() showDeleteButton: boolean = true;

    @Input() showIsListable: boolean = true;
    @Input() showIsPartOfKey: boolean = true;

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

    private fieldDefinitions = new Array<FieldTypeAPIModelField>();
    private selectedRow = new FieldTypeAPIModelField();

    private theDeleteCallback: Function;
    public hasKeyFields: boolean = false;

    constructor(private fieldsService: FieldsObservableService, private messagesService: MessagesObservableService) {
        super();
        this.theDeleteCallback = this.deleteFieldType.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'currentUid') {
                this.currentUid = changes['currentUid'].currentValue;
                this.isEditing = false;
                this.isAdding = false;
                this.isDeleting = false;
            }
            if (p == 'objectID') {
                this.objectID = changes['objectID'].currentValue;
                this.isEditing = false;
                this.isAdding = false;
                this.isDeleting = false;
            }
            if (p == 'objectType') {
                this.objectType = changes['objectType'].currentValue;
                this.isEditing = false;
                this.isAdding = false;
                this.isDeleting = false;
            }
        }
        this.load();
    }

    private GetCurrentUid() {
        if (this.assetTypeUid != null)
            return this.assetTypeUid;
        else if (this.actionTypeUid != null)
            return this.actionTypeUid;
        else if (this.relationshipTypeUid != null)
            return this.relationshipTypeUid;
    }

    load(): void {
        this.currentUid = this.GetCurrentUid();
        if (this.currentUid == null) {
            console.error("No Asset, Action or Relationship type Uid provided.")
            return;
        }

        if (this.objectType == "IntersectType")
            this.showIsPartOfKey = false;
        this.isLoading = true;
        this.hasKeyFields = false
        this.fieldsService.getFieldsV2(this.currentUid).subscribe(
            data => {
                this.fieldDefinitions = data;
                this.checkKeyFields();
                this.selectedRow = null;
                this.isLoading = false;
            }
        );
    }
    private checkKeyFields() {
        let foundKeyField = false;
        if (this.fieldDefinitions && this.fieldDefinitions.length > 0) {
            this.fieldDefinitions.forEach(d => {
                let type = this.currentFieldType(d);
                if (!d.Type[type].SortOrder)
                    d.Type[type].SortOrder = 0;
                if (this.IsPartyOfKey(d.Type)) {
                    foundKeyField = true;
                }
            });
            this.sortFields();
            this.hasKeyFields = foundKeyField;
        }
    }

    currentFieldType(item: FieldTypeAPIModelField): string {
        return Object.keys(item.Type).filter((key) => { return item.Type[key] !== null })[0];
    }
    sortFields() {
        this.fieldDefinitions.sort((x, y) => {
            let xtype = this.currentFieldType(x);
            let ytype = this.currentFieldType(y);
            return x.Type[xtype].ColumnOrder - y.Type[ytype].ColumnOrder;
        });
    }
    IsPartyOfKey(itemType): boolean {
        let partOfKey = false;
        if (itemType.Boolean != null) partOfKey = itemType.Boolean.IsPartOfKey;
        if (itemType.Date != null) partOfKey = itemType.Date.IsPartOfKey;
        if (itemType.DateTime != null) partOfKey = itemType.DateTime.IsPartOfKey;
        if (itemType.Decimal != null) partOfKey = itemType.Decimal.IsPartOfKey;
        if (itemType.Html != null) partOfKey = itemType.Html.IsPartOfKey;
        if (itemType.Link != null) partOfKey = itemType.Link.IsPartOfKey;
        if (itemType.Lookup != null) partOfKey = itemType.Lookup.IsPartOfKey;
        if (itemType.Number != null) partOfKey = itemType.Number.IsPartOfKey;
        if (itemType.Text != null) partOfKey = itemType.Text.IsPartOfKey;

        return partOfKey;
    }

    getDisplayTypeName(item: FieldTypeAPIModelField): string {
        switch (this.currentFieldType(item)) {
            case "ComputedRelationshipField":
                return "Field from Relationship";
            case "ComputedFusionLookup":
                return "Fusion Lookup";
            case "DateTime":
                return "Date Time";
            case "ComputedOwnershipLookup":
                return "Ownership Lookup";
            case "ComputedRelationshipLookup":
                return "Relation Lookup";
            case "Text":
                return "Simple Text";
            case "Lookup":
                return "List";
            default:
                return this.currentFieldType(item);
        }
    }
    edit(name: string): void {
        this.selectedRow = this.fieldDefinitions.find(f => f.Name == name);
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
        this.selectedRow = this.fieldDefinitions.find(f => f.Name == name);
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
                    this.messagesService.showInfoMessage('Success', 'Field definition successfully removed.');
                    let index = this.fieldDefinitions.findIndex(f => f.Name == this.selectedRow.Name);

                    this.isDeleting = false;
                    this.checkKeyFields();
                    if (index >= 0 && index < this.fieldDefinitions.length) {
                        this.fieldDefinitions.splice(index, 1);
                    }

                    this.onFieldsChanged.emit();
                } else {                       
                    this.isDeleting = false;
                }                    
            }
        );
       
    }

    moveUp(field: FieldDefinition) {
        this.isLoading = true;

        this.fieldsService.moveUp(this.currentUid, field.Name).subscribe(
            r => {
                this.load();
                this.onFieldsChanged.emit();
            }
        );
    }

    moveDown(field: FieldDefinition) {
        this.isLoading = true;
        this.fieldsService.moveDown(this.currentUid, field.Name).subscribe(
            r => {
                this.load();
                this.onFieldsChanged.emit();
            }
        );
    }
}
