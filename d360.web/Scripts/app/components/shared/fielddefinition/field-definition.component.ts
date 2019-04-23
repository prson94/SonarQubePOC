import { Input, Output, Component, OnChanges, SimpleChange, EventEmitter } from '@angular/core';

import { FieldDefinition, IFieldsService } from '../../../models/fields.model';

import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { MessagesService } from '../../../services/messages.service';

import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-field-definition-tile',
    templateUrl: './field-definition.component.html',
    providers: [ FieldsObservableService ]
})

export class FieldDefinitionComponent extends BaseComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() title: string = 'Field Definition';

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

    private fieldDefinitions = new Array<FieldDefinition>();
    private selectedRow = new FieldDefinition();

    private theDeleteCallback: Function;
    public hasKeyFields: boolean = false;

    constructor(private fieldsService: FieldsObservableService, private messagesService: MessagesService) {
        super();
        this.theDeleteCallback = this.deleteFieldType.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'objectType') {
                this.objectType = changes['objectType'].currentValue;
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
        }
        this.load();
    }

    load(): void {
        if (this.objectType == null || this.objectID == null)
            return;

        if (this.objectType == "IntersectType")
            this.showIsPartOfKey = false;
        this.isLoading = true;
        this.hasKeyFields = false
        this.fieldsService.getFields(this.objectID, this.objectType).subscribe(
            data => {
                this.fieldDefinitions = data;

                this.fieldDefinitions.forEach(d => {
                    if (d.Type == 'ComplexRelationLookup') {
                        d.Type = 'Relation Lookup';
                    }
                    if (d.Type == 'RelationLookup') {
                        d.Type = 'Relation Lookup';
                    }
                    if (d.Type == 'FusionLookup') {
                        d.Type = 'Fusion Lookup';
                    }
                    if (d.Type == 'DateTime') {
                        d.Type = 'Date Time';
                    }
                    if (d.Type == 'FilteredLookup') {
                        d.Type = 'Filtered Lookup';
                    }
                    if (d.IsPartOfKey) {
                        this.hasKeyFields = true;
                    }
                });

                this.selectedRow = null;
                this.isLoading = false;
            }
        );
    }

    edit(id: number): void {
        this.selectedRow = this.fieldDefinitions.find(f => f.ID == id);
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

    delete(id: number): void {
        this.selectedRow = this.fieldDefinitions.find(f => f.ID == id);
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

    deleteFieldType(id: number) {
        this.fieldsService.deleteFieldType(id).subscribe(
            res => {
                this.showMessageForResult(this.messagesService, res);
                if (!res.isError) {
                    let index = this.fieldDefinitions.findIndex(f => f.ID == id);

                    this.isDeleting = false;

                    if (index >= 0 && index < this.fieldDefinitions.length) {
                        this.fieldDefinitions.splice(index, 1);
                    }

                    this.onFieldsChanged.emit();
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
