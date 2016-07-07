///<reference path="../../../../node_modules/typings/index.d.ts"/>  

import { Input, Component, EventEmitter, Output } from '@angular/core';
import { NgForm, REACTIVE_FORM_DIRECTIVES } from '@angular/forms';
import {Button, Editor, InputText, Dropdown, SelectItem, MultiSelect} from 'primeng/primeng';
import { RelationshipsService} from '../../services/index';
import { RelationshipDetail} from '../../models/relationship.model';

import _ from 'lodash';

@Component({
    selector: 'd3s-admin-relationships-editor',
    template: ` 
                <header>{{action}} Relationship</header>
                <div *ngIf="isLoading || isLoadingItem">
                    <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                <div class="row" *ngIf="!isLoading && !isLoadingItem">
                    <form (ngSubmit)="onSubmit()" #relationshipEditorForm="ngForm">                        
                        <div class="col l6 s12">
                            <div class="FieldName">Relationship Side 1</div>
                            <div><p-dropdown required name="Side1" [options]="side1Options" [disabled]="editedRelationship.LimitedChangesOnly" (onChange)="side1Changed($event);" [(ngModel)]="editedRelationship.Side1" [style]="{width:'100%'}"  #side1="ngModel"></p-dropdown></div>
                            <div [hidden]="side1.valid || side1.pristine">Relationship Side 1 is required</div>
                        </div>                        
                        <div *ngIf="isLoadingSide2" class="col l6 s12">
                            <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                        </div>
                        <div class="col l6 s12" *ngIf="!isLoadingSide2">
                            <div class="FieldName">Relationship Side 2</div>
                            <div><p-dropdown required name="Side2" [options]="side2Options" [disabled]="editedRelationship.LimitedChangesOnly" [(ngModel)]="editedRelationship.Side2" [style]="{width:'100%'}"  #side2="ngModel"></p-dropdown></div>
                            <div [hidden]="side2.valid || side2.pristine">Relationship Side 2 is required</div>
                        </div>
                        <div class="col l6 s12">
                            <div class="FieldName">Friendly Name</div>
                            <div><input style="width: 100%;" name="side1displaytext" [type]="'string'" [(ngModel)]="editedRelationship.Side1DisplayText"></div>                                                        
                        </div>  
                        <div class="col l6 s12">
                            <div class="FieldName">Friendly Name</div>
                            <div><input style="width: 100%;" name="side2displaytext" [type]="'string'" [(ngModel)]="editedRelationship.Side2DisplayText"></div>                                                        
                        </div>  
                        <div class="col l6 s12">                                                        
                            <div class="FieldName">Predicates</div>
                            <div><p-multiSelect name="predicates" [options]="predicates" [(ngModel)]="editedRelationship.Predicates" [style]="{width:'100%'}"></p-multiSelect></div>
                        </div>
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton type="submit" [disabled]="!relationshipEditorForm.form.valid" style="width: '150px';" label="Save"></button>                            
                            <button pButton type="button" (click)="closeClick.emit();" label="Close" style="width: '150px';"></button>
                        </div>                    
                    </form>                           
                </div>
                `,
    providers: [RelationshipsService],
    directives: [Button, Editor, InputText, Dropdown, REACTIVE_FORM_DIRECTIVES, MultiSelect]
})

export class AdminRelationshipsEditor {
    @Input() relationshipID: number = 0;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    error: any;
    editedRelationship: RelationshipDetail;
    side1Options: SelectItem[] = [];
    side2Options: SelectItem[] = [];
    predicates: SelectItem[] = [];
    isLoading: boolean = false;
    isLoadingSide2: boolean = false;
    isLoadingItem: boolean = false;
    
    constructor(private relationshipsService: RelationshipsService) { }

    ngOnInit() {        
        this.loadPredicates();   
        this.loadSide1Options(); 

        if (this.relationshipID > 0) {
            this.loadItem(this.relationshipID);
        }
        else {
            this.editedRelationship = new RelationshipDetail();
            this.action = 'New';
        }
    }

    private loadItem(id: number) { 
        this.isLoadingItem = true;       
        this.relationshipsService.getRelation(id).then(result => {
            this.editedRelationship = result;
            this.isLoadingItem = false;
            if (this.editedRelationship.Side1) {
                let info = this.editedRelationship.Side1.split('|');
                let side2Info = this.editedRelationship.Side2.split('|');
                if (info.length >= 2 && side2Info.length >= 2)
                    this.loadSide2Options(Number(info[1]), info[0], Number(side2Info[1]), side2Info[0]);
            }                        
        });
    }

    private side1Changed(event) {
        if (!event.value) return;
        let info = event.value.split('|');
        if (info.length < 2) return;

        this.loadSide2Options(Number(info[1]), info[0]);
    }

    private loadPredicates() {
        this.relationshipsService.getRelationshipPredicates().then(result => {
            for (let item of result) {
                this.predicates.push({ label: item.title, value: Number(item.value) });
            }               
        });
    }

    private loadSide1Options() {
        this.isLoading = true;
        this.relationshipsService.getSide1Options().then(result => {
            this.side1Options = [];
            for (let item of result) {
                this.side1Options.push({ label: item.title, value: item.value });
            }
            this.isLoading = false;
        });
    }

    private loadSide2Options(id: number, type: string, side2Id?: number, side2Type?: string) {
        this.isLoadingSide2 = true;
        this.relationshipsService.getSide2Options(id, type, side2Id, side2Type).then(result => {
            this.side2Options = [];
            for (let item of result) {                
                this.side2Options.push({ label: item.title, value: item.value });
            }
            this.isLoadingSide2 = false;
        });
    }

    onSubmit() {        
        //save the item back to the save or edit url        
        this.saveClick.emit({ relationship: this.editedRelationship, action: this.relationshipID > 0 ? "new" : "edit" });
    }
};