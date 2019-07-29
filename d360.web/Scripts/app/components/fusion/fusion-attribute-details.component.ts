import { Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PermissionsService } from '../../services/permissions.service';
import { StringConstants } from '../../static/string-constants';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { FusionAttributeService } from '../../services/fusion-attribute.service';
import { FusionAttributeValueDetails } from '../../models/fusion-attribute.model';
import { FormMode } from '../../models/form.model';
import { FusionService } from '../../services/fusion.service';
import { FusionConfigurationDetails } from '../../models/fusion.model';


@Component({
    selector: 'd3s-fusion-attribute-details',
    templateUrl:'./fusion-attribute-details.component.html',
    providers: [PermissionsService, FusionAttributeService, FusionService],
})


export class FusionAttributeDetailsComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private type: string = '';
    private id: number = -1;
    private name: string = '';
    private fusionAttributeDetail: FusionAttributeValueDetails;
    private fusionAttributeDetailHierarchy: FusionAttributeValueDetails[];

    treeSub: any;
    private getFusionConfiguration: any;
    private fusion: FusionConfigurationDetails;
    private formMode :FormMode = FormMode.Default;
    FormMode = FormMode;
    private dataProfileId: number = -1;
    private crumbs: Breadcrumb[] = [];
  
    constructor(        
        private route: ActivatedRoute,
        private router: Router,
        rightSidebarService: RightSidebarService,
        private titleService: Title,
        private headerBreadcrumbService: HeaderBreadcrumbService,
        private permissionsService: PermissionsService,
        private fusionService: FusionService,
        private fusionAttributeService: FusionAttributeService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;
    }

    ngOnInit() {        
        this.setCommonRightSideBar(true, true, false, true, true, true, false);
        this.sub = this.route.params.subscribe(params => {
            this.type = decodeURIComponent(params['type']);
            this.id = +params['id'];
            this.name = decodeURIComponent(params['name'] ? params['name'] : 'Details');
            this.dataProfileId = params['dataProfileId'] ? +params['dataProfileId'] : -1;
            this.setBrowserTitle(this.titleService, this.name);
            this.loadPermissions(this.permissionsService, StringConstants.ObjectFusionAttribute, this.id);
        });

        this.fusionAttributeService.getFusionAttributeDetails(this.type, this.id).subscribe(
            item => {
                this.fusionAttributeDetail = item;
                //get the fusion details for initial breadcrumb
                this.getFusionConfiguration = this.fusionService.getFusionConfiguration(item.FusionID).subscribe(
                    result => {
                        this.fusion = result;
                        this.buildBreadcrumb();
                        this.isLoading = false;
                    }
                );
                this.setObjectInfo(this.type, this.id, undefined, this.fusionAttributeDetail.AssetID);
                
            }
        );
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
        this.getFusionConfiguration.unsubscribe();
    }
    private buildBreadcrumb() {
        this.headerBreadcrumbService.getFolderTitle('#Fusion').then((res) => {
            this.headerBreadcrumbService.clearBreadcrumbs();
            this.fusionAttributeDetailHierarchy = [];
            let areaBreadcrumb = new Breadcrumb(res ? res : 'Fusion');
            this.headerBreadcrumbService.showBreadcrumb(areaBreadcrumb);
            //need the current fusion details for first crumb
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.fusion.Name, SiteUrlHelpers.getObjectUrl('FUSIONTYPE', this.fusion.ID), undefined, 'Fusion', this.fusion.ID, undefined, undefined, true));
            //build hierachy after
            if (this.fusionAttributeDetail) {
                this.crumbs.unshift(new Breadcrumb(
                    this.name,
                    undefined,
                    undefined,
                    undefined,
                    undefined,
                    undefined,
                    undefined,
                    undefined,
                    false,
                    this.fusionAttributeDetail.AssetTypeName));

                this.checkParent(this.fusionAttributeDetail);
            }

            this.headerBreadcrumbService.getFolderIcon(areaBreadcrumb.text).then(icon => {
                this.rightSidebarService.setCurrentArea(areaBreadcrumb.text, icon, 'Definition');
                this.setCommonRightSideBar(true, true, false, true, true, true, false);
                this.rightSidebarService.showHeader(true);
            });

        });
       
    }

    private checkParent(item: FusionAttributeValueDetails) {
        if (item.ParentID) {
            this.fusionAttributeService.getFusionAttributeDetails(this.type, item.ParentID).subscribe(parentItem => {
                this.fusionAttributeDetailHierarchy.unshift(parentItem);
                let crumb = new Breadcrumb(parentItem.Name,
                    `/${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${parentItem.FusionID};fusionAttributeTypeId=${parentItem.FusionAttributeTypeID}`,
                    undefined,
                    undefined,
                    undefined,
                    null, undefined, false, undefined, parentItem.AssetTypeName);
                this.crumbs.unshift(crumb);
                this.checkParent(parentItem);
            });
        } else {
            this.crumbs.forEach(x => this.headerBreadcrumbService.showBreadcrumb(x));
        }
    }

    private formModeChange($event: FormMode) {
        this.formMode = $event;
    }
    private close() {
       this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('FusionTypeWithFusionAttributeType', this.fusionAttributeDetail.FusionAttributeTypeID, this.fusionAttributeDetail.FusionID ));
    }
}