import { Component, Input } from '@angular/core';
import { Classification } from '../../models/object-detail.model';
import { MessagesService } from '../../services/messages.service';
import { ObjectDetailService } from '../../services/object-detail.service';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-admin-classifications',
    providers: [ObjectDetailService],
    template: `
                <div class="tile tile-detail">
                   <header *ngIf="!showEditor && !showDelete">{{objectType == 'TaxonomyTypeClass' ? 'Model' : 'Policy'}} Classifications
                        <d3s-tile-actions [hasAdd]="true" (addClick)="add()"></d3s-tile-actions>                            
                   </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading && !showDelete && !showEditor">
                        <input  [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                        <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="classifications" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="selected=$event.data;showEditor=true;" [(selection)]="selected" >                                                                        
                            <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                            <p-column field="Name" header="Name" sortable="true"></p-column>
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
                    <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="objectType" [title]="objectType == 'TaxonomyTypeClass' ? 'Model Classification' : 'Policy Classification'" [selection]="selected" (saveClick)="saveClassification($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
                    <d3s-delete-form *ngIf="showDelete"
                        [callback]="theDeleteCallback"
                        [itemId]="selected?.ID"
                        [method]="'callback'"
                        [prompt]="'Are you sure you want to delete the Classification [' + [selected?.Name] + ']?'"                                         
                        (onCancel)="showDelete=false;"
                    ></d3s-delete-form>              
                </div>
                `
})

export class AdminClassificationsComponent extends BaseComponent {
    @Input() objectType: string;
    
    classifications: Classification[] = [];
    showEditor: boolean = false;
    showDelete: boolean = false;    
    selected: Classification = null;
    theDeleteCallback: Function;

    constructor(private objectDetailService: ObjectDetailService, private messagesService: MessagesService) {
        super();
        this.theDeleteCallback = this.deleteClassification.bind(this);
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.objectDetailService.getClassifications(this.objectType)
            .then(result => {
                this.classifications = result;
                this.selected = this.classifications.length > 0 ? this.classifications[0] : null;
            });
    }

    deleteClassification(id: number) {
        this.objectDetailService.deleteClassification(id, this.objectType)
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
        this.objectDetailService.saveClassification(event.item, this.objectType)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (event.item.ID == undefined) {                    
                    event.item.ID = Number(result.id);
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
}


