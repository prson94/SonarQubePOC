///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output } from '@angular/core';
import { NgForm } from '@angular/forms';
import { SelectItem } from 'primeng/primeng';
import { RelationshipsService } from '../../services/index';
import { RelationshipDetail } from '../../models/relationship.model';
import { DropdownOption } from '../../models/dropdown.model';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-relationships-editor',
    template: ` 
                <header>{{action}} Relationship</header>
                <div *ngIf="isLoading || isLoadingItem">
                    <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                <div class="row" *ngIf="!isLoading && !isLoadingItem">
                    <div class="form-instructions">When creating a relationship, Side 1 should always be the higher-level item in the relationship, while Side 2 is the lower-level, or atomic, item in the relationship.  For example, when defining a relationships between Application and Business Term you would set Application as Side 1 and Business Term as Side 2.  This will impact how sourcing and synonym inheritance works, as Side 2 is what you are sourcing as well as where synonyms defined on the relationship will also appear.</div>            
                    <form (ngSubmit)="onSubmit()" #relationshipEditorForm="ngForm">                        
                        <div class="col l12 s12">
                            <div class="FieldName">Relationship Side 1</div>
                            <div>                                 
                                <select (change)="side1Changed($event.target);" [disabled]="editedRelationship.LimitedChangesOnly"  required [(ngModel)]="editedRelationship.Side1" name="Side1" #side1="ngModel" style="width:100%;">
                                  <option></option>
                                  <option *ngFor="let p of side1Options" [value]="p.value">{{p.title}}</option>
                                </select>
                            </div>
                            <div [hidden]="side1.valid || side1.pristine">Relationship Side 1 is required</div>
                        </div>                        
                        <div *ngIf="isLoadingSide2" class="col l12 s12">
                            <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                        </div>
                        <div class="col l12 s12" *ngIf="!isLoadingSide2">
                            <div class="FieldName">Relationship Side 2</div>
                            <div>                                
                                <select required [disabled]="editedRelationship.LimitedChangesOnly"  [(ngModel)]="editedRelationship.Side2" name="Side2" #side2="ngModel" style="width:100%;">
                                  <option></option>
                                  <option *ngFor="let p of side2Options" [value]="p.value">{{p.title}}</option>
                                </select>
                            </div>
                            <div [hidden]="side2.valid || side2.pristine">Relationship Side 2 is required</div>
                        </div>                        
                        <div class="col l12 s12">                                                        
                            <div class="FieldName">Predicates</div>
                            <div>
                                <select name="predicates" [(ngModel)]="editedRelationship.Predicate" #predicate="ngModel" style="width:100%;">
                                    <option *ngFor="let p of predicates" [value]="p.value" [innerHtml]="p.title"></option>
                                </select>
                            </div>
                            <div [hidden]="predicate.valid || predicate.pristine">A predicate is required</div>
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
})

export class AdminRelationshipsEditor {
    @Input() relationshipID: number = 0;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    error: any;
    editedRelationship: RelationshipDetail;
    side1Options: DropdownOption[] = [];
    side2Options: DropdownOption[] = [];
    predicates: DropdownOption[] = [];
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
        console.log(event);
        if (!event.value) return;
        let info = event.value.split('|');
        if (info.length < 2) return;

        this.loadSide2Options(Number(info[1]), info[0]);
    }

    private loadPredicates() {
        this.relationshipsService.getRelationshipPredicates().then(result => {
            this.predicates = result;            
        });
    }

    private loadSide1Options() {
        this.isLoading = true;
        this.relationshipsService.getSide1Options().then(result => {            
            this.side1Options = result;
            this.isLoading = false;
        });
    }

    private loadSide2Options(id: number, type: string, side2Id?: number, side2Type?: string) {
        this.isLoadingSide2 = true;
        this.relationshipsService.getSide2Options(id, type, side2Id, side2Type).then(result => {
            this.side2Options = result;            
            this.isLoadingSide2 = false;
        });
    }

    onSubmit() {        
        //save the item back to the save or edit url        
        this.saveClick.emit({ relationship: this.editedRelationship, action: this.relationshipID > 0 ? "new" : "edit" });
    }
};