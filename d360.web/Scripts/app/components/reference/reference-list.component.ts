import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PermissionsService } from '../../services/permissions.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { ReferenceItemType } from '../../models/reference.model';
import { ReferenceService } from '../../services/reference.service';

@Component({
    selector: 'd3s-reference-list',   
   
    template: `                 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading">
                    <div class="col s12 l3">
                        <d3s-reference-item-type-list [initialSelectedListId]="selectedReferenceListId" [(selected)]="selectedReferenceItemType"></d3s-reference-item-type-list>
                    </div>
                    <div class="col s12 l9" *ngIf="selectedReferenceItemType">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <object-detail [objectType]="'ReferenceItemType'" [objectID]="selectedReferenceItemType?.ID"></object-detail>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-field-definition-tile (onFieldsChanged)="refreshItems(itemsGrid);"  [showEditButton]="hasRootUpdatePermissions()" [showAddButton]="hasRootCreatePermissions()" [showDeleteButton]="hasRootDeletePermissions()" [objectType]="'ReferenceItemType'" [objectID]="selectedReferenceItemType?.ID" ></d3s-field-definition-tile>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">           
                                    <d3s-dynamic-grid #itemsGrid [sortField]="'Code'" [title]="'Items'" [showEditButton]="hasRootUpdatePermissions()" [showAddButton]="hasRootCreatePermissions()" [showDeleteButton]="hasRootDeletePermissions()" [itemName]="'Reference'" [objectType]="'ReferenceItemType'" [objectID]="selectedReferenceItemType?.ID" [createUri]="'form/dynamicedit/create/referenceitem/'" [editUri]="'form/dynamicedit/edit/referenceitem/'" [dataUri]="referenceItemUri()" [showExportButton]="true" (exportClick)="exportDataToExcel()" [deleteUri]="'form/dynamicedit/delete/referenceitem/'"></d3s-dynamic-grid>                                                                       
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
               `,
    providers: [PermissionsService, ReferenceService],
})

export class ReferenceListComponent extends BaseComponent implements OnInit, OnDestroy {    
    private sub: any;
    private selectedReferenceItemType: ReferenceItemType;
    private selectedReferenceListId: number = 0;

    constructor(
        rightSidebarService: RightSidebarService,
        private permissionsService: PermissionsService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        private route: ActivatedRoute,
        protected referenceService: ReferenceService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;
        this.setCommonRightSideBar(true, true, false, true, true, true);
        this.auditSidebar.hasDynamicUrl = true;
        this.auditSidebar.dynamicUrlCallback = (() => {
            return `/sidebar/audit/ReferenceItemType/${this.selectedReferenceItemType.ID}`
        });
        this.ownershipSidebar.hasDynamicUrl = true;
        this.ownershipSidebar.dynamicUrlCallback = (() => {
            return `/sidebar/ownership/ReferenceItemType/${this.selectedReferenceItemType.ID}`
        });
        this.impactSidebar.hasDynamicUrl = true;
        this.impactSidebar.dynamicUrlCallback = (() => {
            return `/sidebar/visualization/impact/ReferenceItemType/${this.selectedReferenceItemType.ID}`
        });
        this.lineageSidebar.hasDynamicUrl = true;
        this.lineageSidebar.dynamicUrlCallback = (() => {
            return `/sidebar/visualization/lineage/ReferenceItemType/${this.selectedReferenceItemType.ID}`
        });
        this.relationsSidebar.hasDynamicUrl = true;
        this.relationsSidebar.dynamicUrlCallback = (() => {
            return `/sidebar/relationships/ReferenceItemType/${this.selectedReferenceItemType.ID}`
        });
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Reference');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Reference'));

        this.loadPermissions(this.permissionsService, "ReferenceItemType", 0);

        this.sub = this.route.params.subscribe(params => {
            this.selectedReferenceListId = +params['referenceListId']; // (+) converts string 'id' to a number
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    referenceItemUri() {
        if (this.selectedReferenceItemType == null) return "";

        return `api/referenceItems/${this.selectedReferenceItemType.ID}/items.json`;
    }  

    exportDataToExcel(): void{
        if (!this.selectedReferenceItemType) return;

        this.referenceService.exportReferenceItems(this.selectedReferenceItemType.ID, this.selectedReferenceItemType.Name);
    }

    private refreshItems(itemsGrid) {
        itemsGrid.load();
    }
};