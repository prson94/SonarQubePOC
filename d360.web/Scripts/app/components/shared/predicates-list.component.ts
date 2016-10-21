import { Component } from '@angular/core';
import { Predicate } from '../../models/predicate.model';
import { MessagesService, PredicatesService  } from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-predicates-list',
    providers: [PredicatesService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Predicates
                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
               </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor">
                    <input  [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">
                    <p-dataTable [globalFilter]="gb" [value]="predicates" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="selected=$event.data;showPredicateEditor();" [(selection)]="selected" >                                                                        
                        <p-column field="Name" header="Name" sortable="custom" (sortFunction)="columnSort($event)" [filter]="!showSimpleFilter"></p-column>                                                            
                        <p-column field="Inverse" header="Inverse" sortable="custom" (sortFunction)="columnSort($event)" [filter]="!showSimpleFilter"></p-column>
                        <p-column field="Type" header="Type" sortable="custom" (sortFunction)="columnSort($event)" [filter]="!showSimpleFilter"></p-column>                
                        <p-column [style]="{width:'40px'}">
                            <template let-predicate="rowData" pTemplate type="body">
                                <div class="RowTools" *ngIf="!predicate.IsSystem">
                                    <a style="cursor:pointer;" (click)="selected=predicate;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                </div>
                            </template>
                        </p-column>                            
                        <p-column  [style]="{width:'40px'}">
                            <template let-predicate="rowData" pTemplate type="body">
                                <div class="RowTools" *ngIf="!predicate.IsUsed && !predicate.IsSystem">                                
                                    <a style="cursor:pointer;" (click)="selected=predicate;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                </div>
                            </template>
                        </p-column>                            
                    </p-dataTable> 
                </span>
                <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'Predicate'" [title]="'Predicate'" [selection]="selected" (saveClick)="savePredicate($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
                <delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the predicate [' + [selected?.Name] + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></delete-form>              
                `
})

export class PredicatesListComponent extends BaseComponent {
    error: any;
    predicates: Predicate[] = [];

    showEditor: boolean = false;
    showDelete: boolean = false;    
    selected: Predicate = null;
    theDeleteCallback: Function;

    constructor(private predicatesService: PredicatesService, private messagesService: MessagesService) {
        super();
        this.theDeleteCallback = this.deletePredicate.bind(this);
    }

    ngOnInit() {
        this.getPredicates();
    }

    getPredicates() {
        this.isLoading = true;
        this.predicatesService
            .getPredicates()
            .then(predicates => {
                this.predicates = predicates
                this.isLoading = false;
            })
            .catch(error => this.error = error);
    }

    deletePredicate(id: number) {
        this.predicatesService.deletePredicate(id)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                this.predicates.splice(this.findPredicateIndex(id), 1);
            });
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.predicates.length > 0)
            this.selected = this.predicates[0];
    }

    findPredicateIndex(id: number) {
        var index: number = -1;
        for (var predicate of this.predicates) {
            index++;
            if (predicate.ID == id) return index;
        }
    }

    savePredicate(event) {
        this.predicatesService.savePredicate(event.item)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (event.item.ID == undefined) {
                    console.log(event);
                    event.item.ID = Number(result.id.split('|')[1]);                    
                    this.predicates[this.predicates.length] = event.item;
                }
                else {
                    this.predicates[this.findPredicateIndex(event.item.ID)] = event.item;
                }
                this.selected = event.item;
                this.showEditor = false;
            });
    }

    private showPredicateEditor() {
        if (this.selected.IsSystem) return; //dont allow edit of system predicates
        this.showEditor = true;
    }

    private columnSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.predicates = _.orderBy(this.predicates, [item => item[event.field] ? item[event.field].toLowerCase() : item[event.field]], [event.order == -1 ? 'desc' : 'asc']);
    }
}


