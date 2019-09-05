import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PermissionsService } from '../../services/permissions.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { ReferenceItemType } from '../../models/reference.model';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { ReferenceService } from '../../services/reference.service';
import { UriBasedService } from '../../services/uri-based.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { AuthenticationService } from '../../services/authentication.service';
import { FormMode } from '../../models/form.model';
import { StringConstants } from '../../static/string-constants';
import { ResponsibilityTypeRelationPermission, Permission } from '../../models/responsibility-type.model';

@Component({
    selector: 'd3s-reference-list',

    template: `                 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading">
                    <div [ngClass]="showDefault ? 'col s12 l3' : 'col s12 l8'">
                        <d3s-reference-item-type-list [initialSelectedListId]="selectedReferenceListId" [selected]="selectedReferenceItemType" (formModeChange)="changeFormMode($event)"  (selectedChange)="changeType($event)"></d3s-reference-item-type-list>
                    </div>
                    <div class="col s12 l9" *ngIf="selectedReferenceItemType && showDefault">
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
                                    <d3s-dynamic-grid #itemsGrid [assetTypeUid]="selectedReferenceItemType?.AssetTypeUID" [sortField]="'Code'" [title]="'Items'" [showEditButton]="canEditReferenceItem" [showAddButton]="canAddReferenceItem && canReadSelectedType" [showDeleteButton]="canRemoveReferenceItem" [itemName]="'Reference'" [objectType]="'ReferenceItemType'" [objectID]="selectedReferenceItemType?.ID" [createUri]="'form/dynamicedit/create/referenceitem/'" [editUri]="'form/dynamicedit/edit/referenceitem/'" [dataUri]="referenceItemUri()" [showExportButton]="true" (exportClick)="exportDataToExcel()" [deleteUri]="'form/dynamicedit/delete/referenceitem/'"></d3s-dynamic-grid>                                                                       
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
               `,
    providers: [PermissionsService, ReferenceService, UriBasedService],
})

export class ReferenceListComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private selectedReferenceItemType: ReferenceItemType;
    private selectedReferenceListId: number = 0;
    private canReadSelectedType = true;

    private showDefault: boolean = true;

    private canAddReferenceItem: boolean = false;
    private canEditReferenceItem: boolean = false;
    private canRemoveReferenceItem: boolean = false;

    constructor(
        rightSidebarService: RightSidebarService,
        private route: ActivatedRoute,
        private router: Router,
        private permissionsService: PermissionsService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected referenceService: ReferenceService,
        protected authenticationService: AuthenticationService,
        private uriBasedService: UriBasedService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Reference');

        this.loadPermissions(this.permissionsService, "ReferenceItemType", 0);

        this.sub = this.route.params.subscribe(params => {
            this.canReadSelectedType = false;            

            //load default perms
            this.loadPermissions(this.permissionsService, "ReferenceItemType", 0);

            this.selectedReferenceListId = +params['referenceListId']; // (+) converts string 'id' to a number
            //check if the user has permission to read the selected type
            if (this.selectedReferenceListId != null && !isNaN(this.selectedReferenceListId)) {
                this.referenceService.canReadReferenceType(this.selectedReferenceListId)
                    .subscribe(r => {
                        this.canReadSelectedType = r;

                        this.loadPermissions(this.permissionsService, "ReferenceItemType", this.selectedReferenceListId).then(perms => {
                            this.canAddReferenceItem = this.hasModifyAssetPermissions();
                            this.canEditReferenceItem = this.hasModifyAssetPermissions();
                            this.canRemoveReferenceItem = this.hasDeleteAssetPermissions();

                            this.headerBreadcrumbService.getFolderTitle('#Reference').then((res) => {
                                this.headerBreadcrumbService.clearBreadcrumbs();
                                this.headerBreadcrumbService.clearCurrentObjectInfo();
                                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(res));

                                this.headerBreadcrumbService.getFolderIcon(res).then(icon => {
                                    this.clearSidebar();
                                    this.rightSidebarService.setCurrentArea(res, icon, 'Reference Lists');
                                    this.rightSidebarService.clearCurrentObject();
                                    this.setCommonRightSideBar(true, false, false, false, true, this.hasPermission(Permission.ReadRelationships), false, true);


                                    if (this.auditSidebar) {
                                        this.auditSidebar.hasDynamicUrl = true;
                                        this.auditSidebar.dynamicUrlCallback = (() => {
                                            return `/sidebar/audit/ReferenceItemType/${this.selectedReferenceListId}`
                                        });
                                    }

                                    if (this.impactSidebar) {
                                        this.impactSidebar.hasDynamicUrl = true;
                                        this.impactSidebar.orderPriority = 2;
                                        this.impactSidebar.dynamicUrlCallback = (() => {
                                            return `/sidebar/visualization/impact/ReferenceItemType/${this.selectedReferenceListId}`
                                        });
                                    }

                                    if (this.relationsSidebar) {
                                        this.relationsSidebar.hasDynamicUrl = true;
                                        this.relationsSidebar.orderPriority = 3;
                                        this.relationsSidebar.dynamicUrlCallback = (() => {
                                            return `/sidebar/relationships/ReferenceItemType/${this.selectedReferenceListId}`
                                        });
                                    }

                                    if (this.monitorSidebar) {
                                        this.monitorSidebar.hasDynamicUrl = true;
                                        this.monitorSidebar.dynamicUrlCallback = (() => {
                                            return `/sidebar/workflowmonitor/ReferenceItemType/${this.selectedReferenceListId}`
                                        });
                                    }

                                    if (this.authenticationService.isAdmin) {
                                        let fields = new RightSidebarItem()
                                        fields.hasDynamicUrl = true;
                                        fields.icons = ['fa-drivers-license-o'];
                                        fields.tag = 'fields'
                                        fields.title = 'Field Definitions'
                                        fields.url = '/sidebar/fields'
                                        fields.orderPriority = 1;
                                        fields.dynamicUrlCallback = (() => {
                                            return `/sidebar/fields/ReferenceItemType/${this.selectedReferenceListId}`
                                        });

                                        this.rightSidebarService.showItem(fields);
                                    }

                                    if (this.authenticationService.isAdmin) {
                                        let permissions = new RightSidebarItem()
                                        permissions.hasDynamicUrl = true;
                                        permissions.icons = ['fa-bars'];
                                        permissions.tag = 'responsibilities'
                                        permissions.title = 'Responsibilities'
                                        permissions.url = '/sidebar/responsibilities'
                                        permissions.orderPriority = 4;
                                        permissions.dynamicUrlCallback = (() => {
                                            return `/sidebar/responsibilities/${this.selectedReferenceItemType.AssetTypeID}`
                                        });
                                        this.rightSidebarService.showItem(permissions);
                                    }

                                    this.rightSidebarService.showHeader(true);      
                                });
                            });
                        });
                           
                    });
            }
        });

    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    referenceItemUri() {
        if (this.selectedReferenceItemType == null) return "";

        return `api/referenceItems/${this.selectedReferenceItemType.ID}/items.json`;
    }

    exportDataToExcel(): void {
        if (!this.selectedReferenceItemType) return;

        this.referenceService.exportReferenceItems(this.selectedReferenceItemType.ID, this.selectedReferenceItemType.Name);
    }

    private refreshItems(itemsGrid) {
        itemsGrid.load();
    }

    private changeFormMode(formMode:FormMode) {
        if (formMode == FormMode.Default)
            this.showDefault = true;
        else
            this.showDefault = false;
    }

    changeType(e: any) {
        this.selectedReferenceItemType = e;
        this.selectedReferenceListId = e.ID;
        this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_REFERENCE_ROOT};referenceListId=${e.ID}`);        
    }
};