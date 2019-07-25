import { Component, OnDestroy } from '@angular/core';
import { Predicate } from '../../../models/predicate.model';
import { PredicatesService } from '../../../services/predicates.service';
import { AdminBaseComponent } from '../admin-base.component';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { Title } from '@angular/platform-browser';
import { MessagesObservableService } from '../../../services/messages-observable.service';

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
                    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                    <p-table #dt [value]="predicates" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name','Inverse','Type']" [pageLinks]="3" [paginator]="true" [rows]="20" [(selection)]="selected">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Name'">
                                    Name
                                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'Inverse'">
                                    Inverse
                                    <d3s-sortIcon [field]="'Inverse'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'Type'">
                                    Functional Type
                                    <d3s-sortIcon [field]="'Type'"></d3s-sortIcon>
                                </th>
                                <th style="width: 30px"></th>
                                <th style="width: 30px"></th>
                                <th style="width: 30px"></th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                                <th><d3s-column-filter [field]="'Inverse'" [datatype]="'text'"></d3s-column-filter></th>
                                <th><d3s-column-filter [field]="'Type'" [datatype]="'text'"></d3s-column-filter></th>
                                <th></th>
                                <th></th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr (dblclick)="selected=item;showPredicateEditor();" [pSelectableRow]="item">
                                <td>{{item.Name}}</td>
                                <td>{{item.Inverse}}</td>
                                <td>{{item.Type}}</td>
                                <td>
                                    <div class="RowTools" *ngIf="!item.IsSystem">
                                        <a style="cursor:pointer;" (click)="selected=item;showEditor=true"><i class="fa fa-pencil"></i></a>
                                    </div>
                                </td>
                                <td>
                                    <div class="RowTools" *ngIf="!item.IsUsed && !item.IsSystem">
                                        <a style="cursor:pointer;" (click)="selected=item;showDelete=true"><i class="fa fa-trash-o"></i></a>
                                    </div> 
                                </td>
                                <td>
                                    <div class="RowTools">
                                        <d3s-preview-tooltip objectType="Predicate" [objectId]="item.ID" icon="info">
                                        </d3s-preview-tooltip>
                                    </div>
                                </td>
                            </tr>
                        </ng-template>
                        <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>
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
        private messagesService: MessagesObservableService,
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
            .subscribe(predicates => {
                this.predicates = predicates;
                this.selected = predicates[0];
                this.isLoading = false;
            })
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    deletePredicate(id: number) {
        this.predicatesService.deletePredicate(id)
            .subscribe(result => {
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
            .subscribe(result => {
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