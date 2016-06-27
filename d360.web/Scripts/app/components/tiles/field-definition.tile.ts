///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { DataTable, Column } from 'primeng/primeng';
import { FieldDefinition, IFieldsService } from '../../models/fields.model';
import { FieldsService } from '../../services/fields.service';
import { FieldTypeForm } from '../forms/field-type.form';
import { DeleteForm } from '../forms/delete.form';

@Component({
    selector: 'd3s-field-definition-tile',
    directives: [DataTable, Column, FieldTypeForm, DeleteForm ],
    templateUrl: 'scripts/app/components/tiles/field-definition.tile.html',
    providers: [ FieldsService ]
})

export class FieldDefinitionTile implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() title: string = 'Field Definition';

    private fieldDefinitions = new Array<FieldDefinition>();
    private selectedRow = new FieldDefinition();
    private isLoading = false;
    private isEditing = false; 
    private isAdding = false;
    private isDeleting = false;

    
    constructor(private fieldsService: FieldsService) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'objectType') {
                this.objectType = changes['objectType'].currentValue;
            }
            if (p == 'objectID') {
                this.objectID = changes['objectID'].currentValue;
            }
        }

        this.load();
    }

    load(): void {

        if (this.objectType == null || this.objectID == null)
            return;

        this.isLoading = true;

        this.fieldsService.getFields(this.objectID, this.objectType)
            .then(data => {
                this.fieldDefinitions = data;
                this.selectedRow = null;
                this.isLoading = false;
            });
    }

    edit(id: number): void {
        this.selectedRow = this.fieldDefinitions.find(f => f.ID == id);
        this.isEditing = true;
        this.isDeleting = false;
        this.isAdding = false;
        //this.fieldsService.getFieldTypeEditor(id)
        //    .then(data => {
        //        console.log(data);
        //    });
    }

    add(): void {
        this.isEditing = false;
        this.isDeleting = false;
        this.isAdding = true;
    }

    delete(id: number): void {
        this.selectedRow = this.fieldDefinitions.find(f => f.ID == id);
        this.isEditing = false;
        this.isDeleting = true;
        this.isAdding = false;
    }
}


