///<reference path="../../../../node_modules/typings/index.d.ts"/>  

import { Input, Component, EventEmitter, Output } from '@angular/core';
import { NgForm, REACTIVE_FORM_DIRECTIVES } from '@angular/forms';
import {Button, Editor, InputText} from 'primeng/primeng';
import { AttributeTypeService } from '../../services/index';
import { AttributeType } from '../../models/attribute-type.model';
import { DropdownOption } from '../../models/dropdown.model';

import _ from 'lodash';

@Component({
    selector: 'd3s-admin-attribute-type-editor',
    template: ` 
                <header>{{action}} Attribute Group</header>
                <div *ngIf="isLoading">
                    <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                <div class="row" *ngIf="!isLoading">
                    <div class="form-instructions">Add a report to the list of reports, which can then be exposed in other areas of this system.</div>            
                    <form (ngSubmit)="onSubmit()" #attributeForm="ngForm">                                                
                        <div class="col s12">
                            <div class="FieldName">Name</div>
                            <div><input required style="width: 100%;" name="name" [type]="'string'" [(ngModel)]="editedAttribute.Name" #name="ngModel"></div>     
                            <div [hidden]="name.valid || name.pristine">A name is required</div>                                                   
                        </div>   
                        <div class="col l6 s12">
                            <div class="FieldName">Show Name In Tree</div>
                            <div><input name="showInTree" type="checkbox" [(ngModel)]="editedAttribute.ShowNameInTree" /></div>
                        </div>                                                      
                        <div *ngIf="!(editedAttribute.ParentID > 0)" class="col l6 s12">
                            <div class="FieldName">Category</div>
                            <div>                                
                                <select required [(ngModel)]="editedAttribute.AttributeTypeCategoryID" name="category" #category="ngModel" style="width:100%;">
                                  <option *ngFor="let p of categoryTypes" [value]="p.value">{{p.title}}</option>
                                </select>
                            </div>       
                            <div [hidden]="category.valid || category.pristine">A category is required</div>                     
                        </div>                           
                        <div class="col s12">
                            <div class="FieldName">Text Format</div>
                            <div><input style="width: 100%;" name="textFormat" [type]="'string'" [(ngModel)]="editedAttribute.TextFormatString"></div>                                        
                        </div>                                                
                        <div class="col s12">
                            <div class="FieldName">Description</div>
                            <p-editor name="description" [style]="{'height':'150px'}" [(ngModel)]="editedAttribute.Description"></p-editor>
                        </div>                           
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton type="submit" [disabled]="!attributeForm.form.valid" style="width: '150px';" label="Save"></button>                            
                            <button pButton type="button" (click)="closeClick.emit();" label="Close" style="width: '150px';"></button>
                        </div>                    
                    </form>                           
                </div>
                `,
    providers: [AttributeTypeService],
    directives: [Button, Editor, REACTIVE_FORM_DIRECTIVES]
})

export class AdminAttributeTypeEditor {
    @Input() attribute: AttributeType;
    @Input() parentID: number;

    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    isLoading: boolean = false;

    editedAttribute: AttributeType;
    categoryTypes: DropdownOption[] = [];
    

    constructor(private attributeTypeService: AttributeTypeService) {
        
    }

    ngOnInit() {
        console.log(this.attribute);
        if (this.attribute != undefined) {
            this.editedAttribute = _.cloneDeep(this.attribute);
            if (this.editedAttribute.TextFormatString == null) this.editedAttribute.TextFormatString = "";          
        }
        else {
            this.editedAttribute = new AttributeType();
            this.editedAttribute.ParentID = this.parentID;
            this.editedAttribute.ShowNameInTree = true;
            this.editedAttribute.TextFormatString = "";
            this.action = "Add";
        }
        if (this.editedAttribute.ParentID <= 0 && this.editedAttribute.AttributeTypeCategoryID == null) this.editedAttribute.AttributeTypeCategoryID = 0;
        this.loadCategoryTypes(this.editedAttribute.ParentID);
    }

    onSubmit() {
        this.saveClick.emit({ attribute: this.editedAttribute, action: this.attribute ? "new" : "edit"});
    }

    private loadCategoryTypes(parentID? : number) {
        this.isLoading = true;
        this.attributeTypeService.getAttributeCategoryTypes()
            .then(result => {                
                this.categoryTypes = result;
                this.isLoading = false;
            });
    }
};