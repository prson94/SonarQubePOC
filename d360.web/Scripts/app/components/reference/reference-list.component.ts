import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PermissionsService } from '../../services/permissions.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { ReferenceItemType } from '../../models/reference.model';
import { SecondaryNavCurrentObject } from '../../models/secondaryNav.model';
import { ReferenceService } from '../../services/reference.service';
import { UriBasedService } from '../../services/uri-based.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { AuthenticationService } from '../../services/authentication.service';
import { FormMode } from '../../models/form.model';
import { AssetTypeService } from '../../services/asset-type.service';
import { Subscription } from 'rxjs';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-reference-list',

    template: `                 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading">
                    <div [ngClass]="showDefault ? 'col s12 l3' : 'col s12 l8'">
                        <d3s-reference-item-type-list [initialSelectedListUid]="selectedReferenceListUid" [selected]="selectedReferenceItemType" (formModeChange)="changeFormMode($event)"  (selectedChange)="changeType($event, replaceUrl)"></d3s-reference-item-type-list>
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
                                    <d3s-reference-item-list [hasAdd]="canAddReferenceItem" [assetTypeUid]="selectedReferenceItemType?.uid" [typeName]="selectedReferenceItemType?.Name"></d3s-reference-item-list>                                                                       
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
               `,
    providers: [PermissionsService, ReferenceService, UriBasedService, AssetTypeService],
})

export class ReferenceListComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private selectedReferenceItemType: ReferenceItemType;
    private selectedReferenceListId: number = 0;
    private selectedReferenceListUid: string = '';
    private canReadSelectedType = true;

    private showDefault: boolean = true;

    private canAddReferenceItem: boolean = false;
    private canEditReferenceItem: boolean = false;
    private canRemoveReferenceItem: boolean = false;

    private loadPermissionSub: Subscription;
    private loadObjectDataSub: Subscription;
    private replaceUrl: boolean = true;

    constructor(
        private assetTypeService: AssetTypeService,
        protected authenticationService: AuthenticationService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        private permissionsService: PermissionsService,
        protected referenceService: ReferenceService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        protected titleService: Title,
        private uriBasedService: UriBasedService,
        private route: ActivatedRoute,
        private router: Router
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = headerBreadcrumbService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Reference');

        this.loadPermissions(this.permissionsService, "ReferenceItemType", 0);

        this.sub = this.route.params.subscribe((params) => {
            this.canReadSelectedType = false;

            //load default perms
            this.loadPermissions(this.permissionsService, "ReferenceItemType", 0);

            this.selectedReferenceListId = +params['referenceListId']; // (+) converts string 'id' to a number
            if (params['referenceListId']) {
                
                if (params['referenceListId'].toString().length == 36) {                    
                    this.selectedReferenceListUid = params['referenceListId'];
                    if (this.loadObjectDataSub) {
                        this.loadObjectDataSub.unsubscribe();
                    }
                    this.loadObjectDataSub = this.assetTypeService.getAssetTypeObjectAndID(params['referenceListId']).subscribe((res) => {
                        this.selectedReferenceListId = +res.ObjectID;
                        this.load();
                        if (this.selectedReferenceItemType && this.selectedReferenceItemType.ID != this.selectedReferenceListId) {
                            var referenceItemType: ReferenceItemType = new ReferenceItemType();
                            referenceItemType.ID = this.selectedReferenceListId;
                            referenceItemType.uid = this.selectedReferenceListUid;
                            this.changeType(referenceItemType, true)
                        }                        
                        this.replaceUrl = false;
                    })
                }
                else if (this.selectedReferenceListId != null && !isNaN(this.selectedReferenceListId)) {                    
                    this.load();
                    this.replaceUrl = true;
                }
            }
        });

    }

    private load() {
        //check if the user has permission to read the selected type
        if (this.loadPermissionSub)
            this.loadPermissionSub.unsubscribe();

        this.loadPermissionSub = this.referenceService.canReadReferenceType(this.selectedReferenceListId)
            .subscribe((r) => {
                this.canReadSelectedType = r;
                if (this.selectedReferenceListId && !isNaN( this.selectedReferenceListId)) {
                    this.loadPermissions(this.permissionsService, "ReferenceItemType", this.selectedReferenceListId).then((perms) => {
                        this.canAddReferenceItem = this.hasAddAssetPermissions();
                        this.canEditReferenceItem = this.hasModifyAssetPermissions();
                        this.canRemoveReferenceItem = this.hasDeleteAssetPermissions();
                    });
                    this.buildSecondaryNavigationForObject(this.selectedReferenceListId, 'ReferenceItemType', () => {
                        this.headerBreadcrumbService.getFolderTitle('#Reference').then((res) => {
                            this.headerBreadcrumbService.clearBreadcrumbs();
                            this.headerBreadcrumbService.clearCurrentObjectInfo();
                            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(res));
                            if (this.selectedReferenceItemType)
                                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.selectedReferenceItemType.Name));
                            if (this.auditSidebar) {
                                this.auditSidebar.url = `/sidebar/audit/${this.selectedReferenceListUid}`;
                            }
                        });
                    });
                }
            });
    }

    ngOnDestroy() {
        this.clearSidebar();
        if (this.loadPermissionSub)
            this.loadPermissionSub.unsubscribe();

        if (this.loadObjectDataSub)
            this.loadObjectDataSub.unsubscribe();

    }

    private changeFormMode(formMode: FormMode) {
        if (formMode == FormMode.Default)
            this.showDefault = true;
        else
            this.showDefault = false;
    }

    changeType(e: any, replaceUrl: boolean) {        
        const requiresRedirect = this.selectedReferenceListId !== e.ID;
        this.selectedReferenceItemType = e;
        this.selectedReferenceListId = e.ID;
        this.setSecondaryNavItems();
        if (requiresRedirect) {
            this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_REFERENCE_ROOT};referenceListId=${e.uid}`, { replaceUrl: replaceUrl });
        }
    }

    setSecondaryNavItems() {
        this.secondaryNavService.setCurrentObject(new SecondaryNavCurrentObject(null, null, null, null, true, null, null));
        if (this.auditSidebar) {
            this.auditSidebar.url = `/sidebar/audit/${this.selectedReferenceListUid}`;
        }

        if (this.impactSidebar) {
            this.impactSidebar.orderPriority = 2;
            this.impactSidebar.url = `/sidebar/visualization/impact/ReferenceItemType/${this.selectedReferenceListId}`;
        }

        if (this.relationsSidebar) {
            this.relationsSidebar.orderPriority = 3;
            this.relationsSidebar.url = `/sidebar/relationships/ReferenceItemType/${this.selectedReferenceListId}`;
        }

        if (this.monitorSidebar) {
            this.monitorSidebar.url = `/sidebar/workflowmonitor/ReferenceItemType/${this.selectedReferenceListId}`;
        }

        if (this.authenticationService.isAdmin && this.fieldNav) {

            this.fieldNav.icons = ['fa-drivers-license-o'];
            this.fieldNav.tag = 'fields'
            this.fieldNav.title = 'Field Definitions'
            this.fieldNav.url = '/sidebar/fields'
            this.fieldNav.orderPriority = 1;
            this.fieldNav.url = `/sidebar/fields/ReferenceItemType/${this.selectedReferenceListId}`;

        }

        if (this.authenticationService.isAdmin && this.ownershipSidebar) {

            this.ownershipSidebar.icons = ['fa-bars'];
            this.ownershipSidebar.tag = 'responsibilities'
            this.ownershipSidebar.title = 'Responsibilities'
            this.ownershipSidebar.url = '/sidebar/responsibilities'
            this.ownershipSidebar.orderPriority = 4;
            this.ownershipSidebar.url = `/sidebar/responsibilities/${this.selectedReferenceListUid}`;
        }
    }
};