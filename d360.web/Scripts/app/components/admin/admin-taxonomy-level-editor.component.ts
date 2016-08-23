///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output } from '@angular/core';
import { SelectItem } from 'primeng/primeng';
import { TaxonomiesService, ObjectStyleService } from '../../services/index';
import { Taxonomy, TaxonomyClassification, TaxonomyLevel } from '../../models/taxonomy.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-model-level-editor',
    template: ` 
                <header>{{action}} Level</header>
                <div class="row">
                    <div class="col l6 s12">
                        <div class="FieldName">Name</div>
                        <div><input type="text" pInputText [(ngModel)]="editedTaxonomyLevel.Name" style="width: 100%;" /></div>
                    </div>
                    <div class="col l6 s12" *ngIf="taxonomyLevel==null">
                        <div class="FieldName">Level</div>
                        <div><p-dropdown [options]="availableLevels" [(ngModel)]="editedTaxonomyLevel.Level" [style]="{width:'100%'}"></p-dropdown></div>
                    </div>                    
                    <div class="col s12">
                        <div class="FieldName">Description</div>
                        <p-editor [style]="{'height':'150px'}" [(ngModel)]="editedTaxonomyLevel.Description"></p-editor>
                    </div>
                    <div class="col s12">&nbsp;</div>
                    <div class="col s12">
                        <button pButton type="button" (click)="update()" label="Save" style="width: '150px';"></button>
                        <button pButton type="button" (click)="close()" label="Close" style="width: '150px';"></button>
                    </div>                    
                </div>
                `,
    providers: [TaxonomiesService],
})

export class AdminTaxonomyLevelEditorComponent {
    @Input() taxonomyLevel: TaxonomyLevel;
    @Input() taxonomy: Taxonomy;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    error: any;
    editedTaxonomyLevel: TaxonomyLevel;
    availableLevels: SelectItem[] = [];

    constructor(private taxonomiesService: TaxonomiesService) {

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
        this.taxonomiesService.getTaxonomyLevels(this.taxonomy)
            .then(result => {
                var levels = [];

                for (var i = 1; i <= this.taxonomy.MaximumDepth; i++) {
                    levels.push(i);
                }
                
                for (var i = 0; i < result.length; i++) {
                    //remove the used level
                    let index = levels.map(function (e) { return e; }).indexOf(result[i].Level)
                    console.log(index);
                    levels.splice(index, 1);
                }

                for (var i = 0; i < levels.length; i++) {
                    this.availableLevels.push({
                        label : levels[i].toString(), value : levels[i]
                    });
                }
            });
    }

    update() {
        this.saveClick.emit({ level: this.editedTaxonomyLevel, action: this.taxonomyLevel == null ? "new" : "edit" });
    }
    

    close() {
        this.closeClick.emit({  });
    }
};