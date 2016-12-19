import { Component } from '@angular/core';
import { RuleDimension } from '../../../models/rule.model';
import { MessagesService } from '../../../services/messages.service';
import { RulesService  } from '../../../services/rules.service';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-admin-rule-dimensions',
    providers: [RulesService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Dimensions
                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
               </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span  *ngIf="!isLoading && !showDelete && !showEditor">
                    <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
                   <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="dimensions" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected" >                                                                        
                    <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                    <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>                                                            
                    <p-column field="Description" header="Description" [sortable]="true" [filter]="!showSimpleFilter">
                        <template let-col let-dimension="rowData" pTemplate type="body">
                            <div [innerHtml]="dimension?.Description"></div>
                        </template>                                                        
                    </p-column>    
                        <p-column [style]="{width:'40px'}">
                            <template let-dimension="rowData" pTemplate type="body">
                                <div class="RowTools">
                                    <a style="cursor:pointer;" (click)="selected=dimension;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                </div>
                            </template>
                        </p-column>                            
                        <p-column  [style]="{width:'40px'}">
                            <template let-dimension="rowData" pTemplate type="body">
                                <div class="RowTools">                                
                                    <a style="cursor:pointer;" (click)="selected=dimension;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                </div>
                            </template>
                        </p-column>                            
                    </p-dataTable> 
                </span>
                <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'RuleDimension'" [title]="'Rule Dimension'" [selection]="selected" (saveClick)="saveDimension($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
                <d3s-delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the dimension [' + [selected?.Name] + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></d3s-delete-form>              
                `
})

export class AdminRuleDimensionsComponent extends BaseComponent {    
    error: any;
    dimensions: RuleDimension[] = [];

    showEditor: boolean = false;
    showDelete: boolean = false;
    
    selected: RuleDimension = null;
    theDeleteCallback: Function;

    constructor(private rulesService: RulesService, private messagesService: MessagesService) {
        super();
        this.theDeleteCallback = this.deleteDimension.bind(this);
    }
    
    ngOnInit() {
        this.getDimensions();
    }

    getDimensions() {
        this.isLoading = true;
        this.rulesService
            .getRuleDimensions()
            .then(dimensions => {
                this.dimensions = dimensions
                this.isLoading = false;
            })
            .catch(error => this.error = error);
    }

    deleteDimension(id: number) {
        this.rulesService.deleteDimension(id).
            then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                this.dimensions.splice(this.findDimensionIndex(id), 1);
            });
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.dimensions.length > 0)
            this.selected = this.dimensions[0];
    }

    findDimensionIndex(id: number) {
        var index: number = -1;
        for (var dimension of this.dimensions) {
            index++;
            if (dimension.ID == id) return index;
        }
    }

    saveDimension(event) {
        this.rulesService.saveDimension(event.item)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (event.item.ID == undefined) {
                    event.item.ID = Number(result.id);
                    this.dimensions[this.dimensions.length] = event.item;
                }
                else {
                    this.dimensions[this.findDimensionIndex(event.item.ID)] = event.item;
                }
                this.selected = event.item;
                this.showEditor = false;
            });
    }
}


