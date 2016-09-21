///<reference path="../../es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { Predicate } from '../../models/predicate.model';
import { MessagesService, PredicatesService  } from '../../services/index';


@Component({
    selector: 'd3s-predicates-tile',
    providers: [PredicatesService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Predicates
                <d3s-tile-actions [hasAdd]="true" (addClick)="add()"></d3s-tile-actions>                            
               </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
               <p-dataTable *ngIf="!isLoading && !showDelete && !showEditor" [value]="predicates" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" (onRowDblclick)="selected=$event.data;showPredicateEditor();" [(selection)]="selected" >                                                                        
                    <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                                                            
                    <p-column field="Inverse" header="Inverse" [sortable]="true" [filter]="true"></p-column>
                    <p-column field="Type" header="Type" [sortable]="true" [filter]="true"></p-column>                
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

export class PredicatesTile {
    error: any;
    predicates: Predicate[] = [];

    showEditor: boolean = false;
    showDelete: boolean = false;
    isLoading: boolean = false;
    selected: Predicate = null;
    theDeleteCallback: Function;

    constructor(private predicatesService: PredicatesService) {
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
        this.predicatesService.deletePredicate(id);
        this.showDelete = false;
        this.predicates.splice(this.findPredicateIndex(id), 1);
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
                if (event.item.ID == undefined) {
                    event.item.ID = Number(result.id);
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
}


