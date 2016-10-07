import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { FieldDefinition, IFieldsService } from '../../models/fields.model';
import { FieldsService } from '../../services/fields.service';
import { MessagesService } from '../../services/messages.service';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-field-definition-tile',
    templateUrl: './field-definition.component.html',
    providers: [ FieldsService ]
})

export class FieldDefinitionComponent extends BaseComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() title: string = 'Field Definition';

    private fieldDefinitions = new Array<FieldDefinition>();
    private selectedRow = new FieldDefinition();
    
    private isEditing = false; 
    private isAdding = false;
    private isDeleting = false;

    private theDeleteCallback: Function;

    
    constructor(private fieldsService: FieldsService, private messagesService: MessagesService) {
        super();

        this.theDeleteCallback = this.deleteFieldType.bind(this);
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
    }

    add(): void {
        this.selectedRow = null;
        this.isEditing = true;
        this.isDeleting = false;        
    }

    delete(id: number): void {
        this.selectedRow = this.fieldDefinitions.find(f => f.ID == id);
        this.isEditing = false;
        this.isDeleting = true;
        this.isAdding = false;
    }
    
    editComplete(event) {
        this.isEditing = false;
        this.load();
    }

    deleteFieldType(id: number) {
        this.fieldsService.deleteFieldType(id).then(res => {
            if (res.isError) {
                this.messagesService.showError(res.title, res.message);
            }
            else {
                this.isDeleting = false;
                this.messagesService.showInfoMessage("Success", "Field Definition Deleted");
                let index = this.fieldDefinitions.findIndex(f => f.ID == id);
                if (index >= 0 && index < this.fieldDefinitions.length)
                    this.fieldDefinitions.splice(index, 1);
            }
        });
        
    }
}


