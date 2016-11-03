import { Input, Component, EventEmitter, Output } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ReferenceService } from '../../services/index';
import { ReferenceItemType } from '../../models/reference.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-reference-item-type-editor',
    template: ` 
                <header>{{action}} Reference Item Type</header>
                <form (ngSubmit)="onSubmit()" #referenceItemTypeForm="ngForm">
                <div class="row">
                    <div class="col s12">
                        <div class="FieldName">Name</div>
                        <div><input required type="text" name="name" pInputText [(ngModel)]="editedReferenceItemType.Name" style="width: 100%;" #name="ngModel" /></div>
                        <div [hidden]="name.valid || name.pristine">Reference Item Type name is required</div>
                    </div>                    
                    <div class="col s12">
                        <div class="FieldName" pTooltip="Used to format the value used for display in tooltips, and relationships">Display Format</div>
                        <div><input required type="text" name="format" pInputText [(ngModel)]="editedReferenceItemType.DisplayFormat" style="width: 100%;" #name="ngModel" /></div>                        
                    </div>   
                    <div class="col s12">
                        <div class="FieldName">Description</div>
                        <p-editor [style]="{'height':'150px'}" name="description" [(ngModel)]="editedReferenceItemType.Description"></p-editor>
                    </div>                                        
                    <div class="col s12">&nbsp;</div>
                    <div class="col s12">
                        <button pButton type="submit" [disabled]="!referenceItemTypeForm.form.valid" label="Save"></button>
                        <button pButton type="button" (click)="closeClick.emit()" label="Close"></button>
                    </div>                    
                </div>
                </form>
                `,
    providers: [ReferenceService],
})

export class ReferenceItemTypeEditorComponent {
    @Input() referenceItemType: ReferenceItemType;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    
    editedReferenceItemType: ReferenceItemType;
    

    constructor(private referenceService: ReferenceService) { }

    ngOnInit() {
        if (this.referenceItemType != undefined)
            this.editedReferenceItemType = _.cloneDeep(this.referenceItemType);
        else {
            this.editedReferenceItemType = new ReferenceItemType();
            this.editedReferenceItemType.DisplayFormat = "{Code}";
            this.action = "New";
        }        
    }
    
    onSubmit() {
        this.saveClick.emit({ referenceItemType: this.editedReferenceItemType, action: this.editedReferenceItemType.ID == undefined ? "new" : "edit" });
    }    
};