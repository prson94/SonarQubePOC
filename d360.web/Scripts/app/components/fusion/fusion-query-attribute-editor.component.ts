import { Input, Component, EventEmitter, Output } from '@angular/core';
import { NgForm } from '@angular/forms';
import { FusionService } from '../../services/fusion.service';
import { FusionConfigurationDetails, FusionQueryAttributeType  } from '../../models/fusion.model';
import { BaseComponent } from '../shared/base.component';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-fusion-query-attribute-editor',
    template: ` 
                <header>{{action}} Query Attribute</header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading">
                    <form (ngSubmit)="onSubmit()" #queryForm="ngForm">                        
                        <div class="col s12">
                            <div class="FieldName">Name</div>
                            <div><input required style="width: 100%;" name="name" type="string" [(ngModel)]="editedQuery.Name" #name="ngModel" maxlength="250"></div>                            
                            <div [hidden]="name.valid || name.pristine">Name is required</div>
                        </div>   
                        <div class="col s12">
                            <div class="FieldName">SQL</div>
                            <div ace-editor
                                    [(text)]="editedQuery.Query"                                     
                                    [mode]="'sql'"                                    
                                    theme="eclipse"  
                                    style="height:400px;">
                            </div>
                        </div>                                                                            
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton type="submit" [disabled]="!queryForm.form.valid || !editedQuery?.Query" label="Save"></button>                            
                            <button pButton type="button" (click)="closeClick.emit();" label="Close"></button>
                        </div>                    
                    </form>                           
                </div>
                `,
    providers: [FusionService],
})

export class FusionQueryAttributeEditorComponent extends BaseComponent {    
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();    
    action: string = "Edit";   
    

    @Input() query: FusionQueryAttributeType;
    editedQuery: FusionQueryAttributeType;
    
    constructor(private fusionService: FusionService) { super();}

    ngOnInit() {
        if (this.query) {
            this.editedQuery = _.cloneDeep(this.query);          
        }
        else {
            this.editedQuery = new FusionQueryAttributeType();            
            this.action = "New";
        }
    }

    onSubmit() {
        //save the item back to the save or edit url        
        this.saveClick.emit({ query: this.editedQuery, action: this.query ? "new" : "edit" });
    }    
};