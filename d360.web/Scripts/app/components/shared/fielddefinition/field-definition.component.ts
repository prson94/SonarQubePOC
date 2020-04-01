import { Input, Output, Component, OnChanges, SimpleChange, EventEmitter } from '@angular/core';

import { FieldDefinition, IFieldsService, FieldType } from '../../../models/fields.model';

import { FieldsObservableService } from '../../../services/fieldsObservable.service';

import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { FieldTypeAPIModel, FieldTypeAPIModelField } from '../../../models/fieldtype-api.model';

@Component({
    selector: 'd3s-field-definition-tile',
    templateUrl: './field-definition.component.html',
    providers: [ FieldsObservableService ]
})

export class FieldDefinitionComponent extends BaseComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() Uid: string;
    @Input() title: string = 'Field Definition';

    @Input() actionTypeUid: string;
    @Input() assetTypeUid: string;
    @Input() relationshipTypeUid: string;

    @Input() showAddButton: boolean = true;
    @Input() showEditButton: boolean = true;
    @Input() showDeleteButton: boolean = true;

    @Input() showIsListable: boolean = true;
    @Input() showIsPartOfKey: boolean = true;

    @Output() onEdit = new EventEmitter();
    @Output() onAdd = new EventEmitter();
    @Output() onDelete = new EventEmitter();
    @Output() onCancel = new EventEmitter();
    @Output() onFieldsChanged = new EventEmitter();

    @Input() isEditing = false;
    @Input() isAdding = false;
    @Input() isDeleting = false;

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
            if (p == 'Uid') {
                this.objectType = changes['Uid'].currentValue;
                this.isEditing = false;
                this.isAdding = false;
                this.isDeleting = false;
            }
            if (p == 'Uid') {
                this.objectID = changes['Uid'].currentValue;
                this.isEditing = false;
                this.isAdding = false;
                this.isDeleting = false;
            }
        }
        this.load();
    }

    load(): void {
        if (this.Uid == null)
            return;

        if (this.objectType == "IntersectType")
            this.showIsPartOfKey = false;
        this.isLoading = true;
        this.hasKeyFields = false
        this.fieldsService.getFieldsV2(this.Uid).subscribe(
            data => {
                this.fieldDefinitions = data;
                
                this.fieldDefinitions.forEach(d => {
                    let foundKeyField = false;
                    this.fieldDefinitions.forEach(x => {
                        if (this.IsPartyOfKey(x.Type)) {
                            foundKeyField = true;
                        }
                    });
                    this.hasKeyFields = foundKeyField;
                });

                this.selectedRow = null;
                this.isLoading = false;
            }
        );
    }

    currentFieldType(item: FieldTypeAPIModelField): string {
        return Object.keys(item.Type).filter((key) => { return item.Type[key] !== null })[0];
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
            default:
                return this.currentFieldType(item);
        }
    }
    edit(name: string): void {
        this.selectedRow = this.fieldDefinitions.find(f => f.Name == name);
        console.log(this.fieldDefinitions);
        console.log(this.selectedRow.Name);
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

        this.fieldsService.moveUp(field.ObjectType, parseInt(field.ObjectID), field.ID).subscribe(
            r => {
                this.load();
                this.onFieldsChanged.emit();
            }
        );
    }

    moveDown(field: FieldDefinition) {
        this.isLoading = true;
        this.fieldsService.moveDown(field.ObjectType, parseInt(field.ObjectID), field.ID).subscribe(
            r => {
                this.load();
                this.onFieldsChanged.emit();
            }
        );
    }
}
