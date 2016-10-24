///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output } from '@angular/core';
import { SelectItem } from 'primeng/primeng';
import { TaxonomiesService, ObjectStyleService } from '../../services/index';
import { Taxonomy, TaxonomyClassification, TaxonomyLevel } from '../../models/taxonomy.model';
import { BaseComponent } from '../shared/base.component';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-model-level-editor',
    template: ` 
                <header>{{action}} Level</header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <form (ngSubmit)="onSubmit()" #levelForm="ngForm">
                    <div class="row" *ngIf="!isLoading && levels.length > 0">
                        <div class="col l6 s12">
                            <div class="FieldName">Name</div>
                            <div><input required type="text" name="name" pInputText [(ngModel)]="editedTaxonomyLevel.Name" style="width: 100%;" #name="ngModel" maxlength="250" /></div>
                            <div [hidden]="name.valid || name.pristine">Level name is required</div>
                        </div>
                        <div class="col l6 s12" *ngIf="taxonomyLevel==null">
                            <div class="FieldName">Level</div>
                            <div>
                                    <select required name="availableLevels" style="width:100%;" placeholder="Choose a value" [(ngModel)]="editedTaxonomyLevel.Level" #level="ngModel">                                            
                                          <option></option>
                                          <option *ngFor="let p of levels" [value]="p">{{p}}</option>
                                    </select>                                
                            </div>
                            <div [hidden]="level.valid || level.pristine">Level value is required</div>
                        </div>                    
                        <div class="col s12">
                            <div class="FieldName">Description</div>
                            <p-editor name="description" [style]="{'height':'150px'}" [(ngModel)]="editedTaxonomyLevel.Description"></p-editor>
                        </div>
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton [disabled]="!levelForm.form.valid" type="submit" label="Save"></button>
                            <button pButton type="button" (click)="close()" label="Close"></button>
                        </div>                    
                    </div>
                    <div class="row" *ngIf="!isLoading && levels.length == 0">
                        <div class="center">The maximum number of levels available for this model have already been allocated.  In order to define new levels you can either increase the maximum available levels for this model or delete an existing level for this model.</div>
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">                        
                            <button pButton type="button" (click)="close()" label="Close"></button>
                        </div>                    
                    </div>
                </form>
                `,
    providers: [TaxonomiesService],
})

export class AdminTaxonomyLevelEditorComponent extends BaseComponent {
    @Input() taxonomyLevel: TaxonomyLevel;
    @Input() taxonomy: Taxonomy;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    error: any;
    editedTaxonomyLevel: TaxonomyLevel;
    levels: number[] = [];

    constructor(private taxonomiesService: TaxonomiesService) {
        super();
    }

    ngOnInit() {        
        if (this.taxonomyLevel != undefined)
            this.editedTaxonomyLevel = _.cloneDeep(this.taxonomyLevel);
        else {
            this.editedTaxonomyLevel = new TaxonomyLevel();            
            this.action = "New";
        }   
        this.editedTaxonomyLevel.TaxonomyTypeID = this.taxonomy.ID;
        this.getUnusedLevels();
    }
    
    getUnusedLevels() {
        this.isLoading = true;
        this.taxonomiesService.getTaxonomyLevels(this.taxonomy)
            .then(result => {
                this.isLoading = false;
                for (var i = 1; i <= this.taxonomy.MaximumDepth; i++) {
                    this.levels.push(i);
                }
                
                for (var i = 0; i < result.length; i++) {
                    //remove the used level
                    let index = this.levels.map(function (e) { return e; }).indexOf(result[i].Level)                    
                    this.levels.splice(index, 1);
                }

                console.log(this.levels);
            });
    }

    onSubmit() {
        this.saveClick.emit({ level: this.editedTaxonomyLevel, action: this.taxonomyLevel == null ? "new" : "edit" });
    }
    
    close() {
        this.closeClick.emit({  });
    }
};