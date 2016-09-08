///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, OnChanges, SimpleChange} from '@angular/core';
import { Taxonomy, TaxonomyLevel } from '../../models/taxonomy.model';
import { MessagesService, TaxonomiesService  } from '../../services/index';


@Component({
    selector: 'd3s-model-level-tile',
    providers: [TaxonomiesService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Levels
                <d3s-tile-actions [hasAdd]="true" (addClick)="add()"></d3s-tile-actions>                            
               </header>
                <div *ngIf="isLoading" style="width:100%; text-align:center;">
                    <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
               <p-dataTable *ngIf="!isLoading && !showDelete && !showEditor" [value]="levels" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" (onRowDblclick)="showEditor=true" [(selection)]="selectedLevel" >                                                        
                <p-column field="Level" header="Level" [sortable]="true" [filter]="true"></p-column>                                                            
                <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                                                            
                <p-column field="Description" header="Description" [sortable]="true" [filter]="true">
                    <template let-col let-taxonomy="rowData" pTemplate type="body">
                        <div [innerHtml]="taxonomy?.Description"></div>
                    </template>                                                        
                </p-column>    
                    <p-column [style]="{width:'40px'}">
                        <template let-template="rowData" pTemplate type="body">
                            <div class="RowTools">
                                <a style="cursor:pointer;" (click)="showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                            </div>
                        </template>
                    </p-column>                            
                    <p-column  [style]="{width:'40px'}">
                        <template let-template="rowData" pTemplate type="body">
                            <div class="RowTools">                                
                                <a style="cursor:pointer;" (click)="showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                            </div>
                        </template>
                    </p-column>                            
                </p-dataTable>      
                <delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selectedLevel?.Level"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the level [' + [selectedLevel?.Name] + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></delete-form> 
                <d3s-admin-model-level-editor *ngIf="showEditor" [taxonomyLevel]="selectedLevel" [taxonomy]="taxonomy" (closeClick)="closeEditor()" (saveClick)="saveLevel($event)"></d3s-admin-model-level-editor>                                           
                `
})

export class ModelLevelTile implements OnChanges {
    @Input() taxonomy: Taxonomy = null;
    error: any;    
    levels: TaxonomyLevel[] = [];
    showEditor: boolean = false;
    showDelete: boolean = false;
    isLoading: boolean = false;
    selectedLevel: TaxonomyLevel = null;
    theDeleteCallback: Function;

    constructor(private taxonomiesService: TaxonomiesService) {
        this.theDeleteCallback = this.deleteLevel.bind(this);  
    }
    
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {    
        if (this.taxonomy != null) this.getLevels();
    }
        
    getLevels() {
        this.isLoading = true;
        this.taxonomiesService
            .getTaxonomyLevels(this.taxonomy)
            .then(levels => {
                this.levels = levels;
                this.isLoading = false;
            })
            .catch(error => this.error = error);
    }

    deleteLevel(id: number) {
        this.taxonomiesService.deleteTaxonomyLevel(this.taxonomy.ID, id);
        this.showDelete = false;
        this.levels.splice(this.findTaxonomyLevel(id), 1);
    }

    add() {
        this.showEditor = true;
        this.selectedLevel = null;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selectedLevel == null && this.levels.length > 0)
            this.selectedLevel = this.levels[0];
    }

    findTaxonomyLevel(level: number) {
        var index: number = -1;
        for (var taxonomyLevel of this.levels) {
            index++;
            if (taxonomyLevel.Level == level) return index;
        }
    }

    saveLevel(event) {
        if (event.action == "new") {
            this.taxonomiesService.saveTaxonomyLevel(event.level)
                .then(result => {
                    this.showEditor = false;                    
                    this.levels[this.levels.length] = event.level;
                    this.selectedLevel = event.level;
                });            
        }
        else {
            this.taxonomiesService.editTaxonomyLevel(event.level)
                .then(result => {
                    this.showEditor = false;
                    this.levels[this.findTaxonomyLevel(event.level.Level)] = event.level;
                    this.selectedLevel = event.level;
                });
            
        }        
    }
}


