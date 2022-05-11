import { Component, OnDestroy } from '@angular/core';
import { Predicate, PredicateFriendlyType } from '../../../models/predicate.model';
import { PredicatesService } from '../../../services/predicates.service';
import { AdminBaseComponent } from '../admin-base.component';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { Title } from '@angular/platform-browser';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';

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
                    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="{{searchText}}" class="grid-simple-filter">
                    <p-table #dt [value]="predicates" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name','Inverse','FriendlyTypeName']" [pageLinks]="3" [paginator]="true" [rows]="20" [(selection)]="selected">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Name'">
                                    <ng-container i18n>Name</ng-container>
                                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'Inverse'">
                                    <ng-container i18n>Inverse</ng-container>
                                    <d3s-sortIcon [field]="'Inverse'"></d3s-sortIcon>
                                </th>                                
                                <th [pSortableColumn]="'FriendlyTypeName'">
                                    <ng-container i18n>Functional Type</ng-container>
                                    <d3s-sortIcon [field]="'FriendlyTypeName'"></d3s-sortIcon>
                                </th>
                                <th style="width: 30px"></th>
                                <th style="width: 30px"></th>
                                <th style="width: 30px"></th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                                <th><d3s-column-filter [field]="'Inverse'" [datatype]="'text'"></d3s-column-filter></th>
                                <th><d3s-column-filter [field]="'FriendlyTypeName'" [datatype]="'text'"></d3s-column-filter></th>
                                <th></th>
                                <th></th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr (dblclick)="selected=item;showPredicateEditor();" [pSelectableRow]="item">
                                <td>{{item.Name}}</td>
                                <td>{{item.Inverse}}</td>
                                <td>{{item.FriendlyTypeName}}</td>
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
                                        <d3s-preview-tooltip objectType="Predicate" [objectId]="item.Uid" icon="info">
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
                <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.Uid" [objectType]="'Predicate'" [title]="'Predicate'" [selection]="selected" (saveClick)="savePredicate($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
                <d3s-delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.Uid"
                    [method]="'callback'"
                    [prompt]="deletePromptText"                                         
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

    searchText = $localize`Search...`;
    get deletePromptText(): string {
        return $localize`Are you sure you want to delete the predicate [${this.selected?.Name}]?`;
    }

    constructor(
        private predicatesService: PredicatesService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        secondaryNavService: SecondaryNavService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title
    ) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.theDeleteCallback = this.deletePredicate.bind(this);
        this.areaName = StringConstants.Section_Predicates;
        this.setCommonItems();
        this.buildSecondaryNavigationForObject(0, 'Predicate');
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
                this.predicates.forEach(p => p.FriendlyTypeName = PredicateFriendlyType[p.Type] ? PredicateFriendlyType[p.Type] : p.Type)
                this.isLoading = false;
            })
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    deletePredicate(uid: string) {
        this.predicatesService.deletePredicate(uid)
            .subscribe(result => {
                this.showMessageForApiResults(this.messagesService, result, $localize`Predicate deleted`, true);
                this.showDelete = false;
                if (!result.some(x => x.Success == false)) {
                    this.predicates = this.predicates.filter(x => x.Uid != uid);
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
        let predicate: Predicate = event.item;

        if (this.selected) {
            predicate.Uid = this.selected.Uid;
            if (this.selected.IsInUse) {
                predicate.Type = this.selected.Type;
            }
        }

        this.predicatesService.savePredicate(event.item)
            .subscribe(result => {

                if (event.action == 'new') {
                    this.showMessageForApiResults(this.messagesService, result, $localize`Predicate succesfully added!`, true);
                }
                else {
                    this.showMessageForApiResults(this.messagesService, result, $localize`Predicate succesfully updated!`, true);
                }
                this.getPredicates();
                this.showEditor = false;
            });
    }

    private showPredicateEditor() {
        if (this.selected.IsSystem) return; //dont allow edit of system predicates
        this.showEditor = true;
    }
}