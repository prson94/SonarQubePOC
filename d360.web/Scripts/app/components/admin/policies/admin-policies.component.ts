import {
    Component,
    OnInit,
    OnDestroy
} from '@angular/core';
import { Title } from '@angular/platform-browser';

import { PolicyType } from '../../../models/policy.model';

import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { PoliciesService } from '../../../services/policies.service';
import { StateService } from '../../../services/state.service';
import { AssetTypeService } from "../../../services/asset-type.services";

import { AdminBaseComponent } from '../admin-base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-admin-policies-component',
    providers: [PoliciesService, AssetTypeService],
    template: `
        <div class="tile tile-detail"
             *ngIf="showEditor || showDelete">
            <d3s-asset-type-editor *ngIf="showEditor"
                                   [assetTypeClass]="'P'"
                                   [id]="selected?.AssetTypeID"
                                   [title]="(selected == null ? 'New' : 'Edit') +' Policy Type'"
                                   (onCancel)="closeEditor()"
                                   (onComplete)="savePolicyType($event)"></d3s-asset-type-editor>
            <d3s-delete-form *ngIf="showDelete"
                             [callback]="theDeleteCallback"
                             [itemId]="selected?.AssetTypeID"
                             [method]="'callback'"
                             [prompt]="'Are you sure you want to delete the policy type [' + [selected?.Name] + ']?'"
                             (onCancel)="showDelete=false;"
            ></d3s-delete-form>
        </div>
        <div class="row"
             *ngIf="!showEditor && !showDelete">
            <div class="col l4 s12">
                <div class="tile tile-detail">
                    <header>Policy Types
                        <d3s-tile-actions [hasAdd]="true"
                                          [hasFilterMode]="true"
                                          [(filterMode)]="showSimpleFilter"
                                          (addClick)="add()"></d3s-tile-actions>
                    </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading && !showEditor && !showDelete">
                                <input type="text"
                                       [hidden]="!showSimpleFilter"
                                       pInputText
                                       size="100"
                                       (input)="dt.filterGlobal($event.target.value, 'contains')"
                                       placeholder="Search..."
                                       class="grid-simple-filter">
                                <p-table #dt
                                         [value]="policyTypes"
                                         selectionMode="single"
                                         [metaKeySelection]="true"
                                         [globalFilterFields]="['Name','MaximumDepth']"
                                         sortField="Name"
                                         [sortOrder]="1"
                                         [pageLinks]="3"
                                         [paginator]="true"
                                         [rows]="20"
                                         [(selection)]="selected">
                                    <ng-template pTemplate="header">
                                        <tr>
                                            <th [pSortableColumn]="'Name'">
                                                Name
                                                <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'MaximumDepth'"
                                                style="width: 100px">
                                                Max Depth
                                                <d3s-sortIcon [field]="'MaximumDepth'"></d3s-sortIcon>
                                            </th>
                                            <th style="width: 40px"></th>
                                            <th style="width: 40px"></th>
                                        </tr>
                                        <tr [hidden]="showSimpleFilter">
                                            <th><d3s-column-filter [field]="'Name'"
                                                                   [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'MaximumDepth'"
                                                                   [datatype]="'text'"></d3s-column-filter></th>
                                            <th></th>
                                            <th></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body"
                                                 let-item>
                                        <tr (dblclick)="selected=item;showEditor=true;"
                                            [pSelectableRow]="item">
                                            <td>{{ item.Name }}</td>
                                            <td>{{ item.MaximumDepth }}</td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;"
                                                       (click)="selected=item;showEditor=true"><i class="fa fa-pencil"></i></a>
                                                </div>
                                            </td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;"
                                                       (click)="selected=item;showDelete=true"><i class="fa fa-trash-o"></i></a>
                                                </div>
                                            </td>
                                        </tr>
                                    </ng-template>
                                    <ng-template *ngIf="dt.totalRecords"
                                                 pTemplate="summary">
                                        <d3s-grid-paging-info [first]="dt.first"
                                                              [rows]="dt.rows"
                                                              [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                    </ng-template>
                                </p-table> 
                            </span>
                </div>
            </div>
            <div class="col l8 s12"
                 *ngIf="selected">
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <object-detail [objectType]="'PolicyType'"
                                           [objectID]="selected?.ID"></object-detail>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-field-definition-tile [objectType]="'PolicyType'"
                                                       [objectID]="selected?.ID"></d3s-field-definition-tile>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-admin-level-grid objectType="PolicyType"
                                                  [maxDepth]="selected?.MaximumDepth"
                                                  [objectId]="selected?.ID"></d3s-admin-level-grid>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-responsibility-relations queryType="A"
                                                          [id]="selected?.AssetTypeID"
                                                          [showAddButton]="false"></d3s-responsibility-relations>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-admin-allocation [objectType]="'PolicyType'"
                                                  [objectID]="selected?.ID"></d3s-admin-allocation>
                        </div>
                    </div>
                </div>
                <div>
                </div>
    `
})

export class AdminPoliciesComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    policyTypes: PolicyType[] = [];
    selected: PolicyType;
    showEditor = false;
    showDelete = false;
    theDeleteCallback: Function;

    protected assetTypeService: AssetTypeService = null;

    constructor(
        private stateService: StateService,
        rightSidebarService: RightSidebarService,
        private policiesService: PoliciesService,
        protected messagesService: MessagesObservableService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        assetTypeService: AssetTypeService,
        titleService: Title
    ) {
        super(headerBreadcrumbService, titleService, rightSidebarService);

        this.assetTypeService = assetTypeService;

        this.areaName = 'Policy Types';
        this.setCommonItems();
        this.theDeleteCallback = this.deletePolicyType.bind(this);
        this.setCommonRightSideBar(true);

        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;

            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/PolicyType/${this.selected.ID}`;
            });
        }
    }

    ngOnInit() {
        this.getPolicyTypes();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    getPolicyTypes() {
        this.isLoading = true;

        this.policiesService.getPolicyTypes()
            .subscribe(
                result => {
                    this.policyTypes = result.sort((a, b) => a.Name.localeCompare(b.Name));

                    if (this.policyTypes.length > 0) {
                        this.selected = this.policyTypes[0];
                    }

                    this.isLoading = false;
                }
            );
    }

    deletePolicyType(id: number) {
        this
            .assetTypeService
            .deleteAssetType(id)
            .subscribe(
                result => {
                    this.showMessageForResult(this.messagesService, result);
                    this.showDelete = false;

                    if (result.type != 'error') {
                        this.policyTypes = this.policyTypes.filter(x => x.AssetTypeID != id);
                        this.selected = this.policyTypes.length > 0 ? this.policyTypes[0] : null;
                    }

                    this.stateService.reloadLeftNavMenu();
                }
            );
    }

    savePolicyType(event) {
        this.showEditor = false;
        this.getPolicyTypes();
        this.stateService.reloadLeftNavMenu();
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null) {
            this.selected = this.policyTypes.length > 0 ? this.policyTypes[0] : null;
        }
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }
}
