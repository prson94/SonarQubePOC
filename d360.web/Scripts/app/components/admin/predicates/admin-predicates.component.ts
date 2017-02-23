import { Component, OnDestroy } from '@angular/core';
import { Predicate } from '../../../models/predicate.model';
import { PredicatesService } from '../../../services/predicates.service';
import { MessagesService } from '../../../services/messages.service';
import { AdminBaseComponent } from '../admin-base.component';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-admin-predicates-component',
    providers: [PredicatesService],
    template: `
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
               <header *ngIf="!showEditor && !showDelete">Predicates
                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
               </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor">
                    <input  [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                    <p-dataTable #dt [globalFilter]="gb" [value]="predicates" selectionMode="single" rows="20" paginator="true" pageLinks="3" (onRowDblclick)="selected=$event.data;showPredicateEditor();" [(selection)]="selected" >                                                                        
                        <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                        <p-column field="Name" header="Name" sortable="true" [filter]="!showSimpleFilter"></p-column>                                                            
                        <p-column field="Inverse" header="Inverse" sortable="true" [filter]="!showSimpleFilter"></p-column>
                        <p-column field="Type" header="Functional Type" sortable="true" [filter]="!showSimpleFilter"></p-column>                
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
                <d3s-delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the predicate [' + [selected?.Name] + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></d3s-delete-form> 
                </div>
                </div>
                </div>             
                `
})

export class AdminPredicatesComponent extends AdminBaseComponent implements OnDestroy {
    predicates: Predicate[] = [];

    showEditor: boolean = false;
    showDelete: boolean = false;
    selected: Predicate = null;
    theDeleteCallback: Function;

    constructor(private predicatesService: PredicatesService,
        private messagesService: MessagesService,
        rightSidebarService: RightSidebarService,        
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title
    ) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.theDeleteCallback = this.deletePredicate.bind(this);        
        this.areaName = "Predicates";
        this.setCommonItems();        
    }

    ngOnInit() {
        this.getPredicates();
    }

    getPredicates() {
        this.isLoading = true;
        this.predicatesService.getPredicates()
            .then(predicates => {
                this.predicates = predicates
                this.isLoading = false;
            })
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    deletePredicate(id: number) {
        this.predicatesService.deletePredicate(id)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                if (result.type != 'error') {
                    this.predicates = this.predicates.filter(x => x.ID != id);
                }
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

    savePredicate(event) {
        this.predicatesService.savePredicate(event.item)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.getPredicates();
                this.showEditor = false;
            });
    }

    private showPredicateEditor() {
        if (this.selected.IsSystem) return; //dont allow edit of system predicates
        this.showEditor = true;
    }    
}