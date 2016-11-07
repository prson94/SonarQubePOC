import { Component } from '@angular/core';
import { ModelClassification } from '../../models/model.model';
import { MessagesService, ModelsService} from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-model-classifications',
    providers: [ModelsService],
    template: `
                <div class="tile tile-detail">
                   <header *ngIf="!showEditor && !showDelete">Model Classifications
                        <d3s-tile-actions [hasAdd]="true" (addClick)="add()"></d3s-tile-actions>                            
                   </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading && !showDelete && !showEditor">
                        <input  [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                        <p-dataTable sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="classifications" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="selected=$event.data;showEditor=true;" [(selection)]="selected" >                                                                        
                            <p-column field="Name" header="Name" sortable="custom" (sortFunction)="columnSort($event)"></p-column>                                                                                    
                            <p-column [style]="{width:'40px'}">
                                <template let-classification="rowData" pTemplate type="body">
                                    <div class="RowTools">
                                        <a style="cursor:pointer;" (click)="selected=classification;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                    </div>
                                </template>
                            </p-column>                            
                            <p-column  [style]="{width:'40px'}">
                                <template let-classification="rowData" pTemplate type="body">
                                    <div class="RowTools">                                
                                        <a style="cursor:pointer;" (click)="selected=classification;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                    </div>
                                </template>
                            </p-column>                            
                        </p-dataTable> 
                    </span>
                    <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'TaxonomyTypeClass'" [title]="'Model Classification'" [selection]="selected" (saveClick)="saveClassification($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
                    <delete-form *ngIf="showDelete"
                        [callback]="theDeleteCallback"
                        [itemId]="selected?.ID"
                        [method]="'callback'"
                        [prompt]="'Are you sure you want to delete the Model Classification [' + [selected?.Name] + ']?'"                                         
                        (onCancel)="showDelete=false;"
                    ></delete-form>              
                </div>
                `
})

export class AdminModelClassificationComponent extends BaseComponent {
    
    classifications: ModelClassification[] = [];
    showEditor: boolean = false;
    showDelete: boolean = false;    
    selected: ModelClassification = null;
    theDeleteCallback: Function;

    constructor(private modelsService: ModelsService, private messagesService: MessagesService) {
        super();
        this.theDeleteCallback = this.deleteClassification.bind(this);
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.modelsService.getModelClassifications()
            .then(result => {
                this.classifications = result;
                this.selected = this.classifications.length > 0 ? this.classifications[0] : null;
            });
    }

    deleteClassification(id: number) {
        this.modelsService.deleteClassification(id)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                this.classifications = this.classifications.filter(x => x.ID != id);
            });
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.classifications.length > 0)
            this.selected = this.classifications[0];
    }
    
    saveClassification(event) {
        this.modelsService.saveClassification(event.item)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (event.item.ID == undefined) {
                    console.log(event);
                    event.item.ID = Number(result.id.split('|')[1]);
                    this.classifications[this.classifications.length] = event.item;
                }
                else {
                    let index = this.classifications.findIndex(x => x.ID == event.item.ID);

                    if (index >= 0 && index < this.classifications.length)
                        this.classifications[index] = event.item;
                }
                this.selected = event.item;
                this.showEditor = false;
            });
    }    

    private columnSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.classifications = _.orderBy(this.classifications, [item => item[event.field] ? item[event.field].toLowerCase() : item[event.field]], [event.order == -1 ? 'desc' : 'asc']);
    }
}


