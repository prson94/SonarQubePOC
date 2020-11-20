import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { AssetService } from '../../../services/asset.service';

@Component({
    selector: 'd3s-workflow-monitor',
    providers: [AssetService],
    template: ` 
               <div>
                    <d3s-monitor [objectType]="objectType" [objectId]="objectID"></d3s-monitor>
                </div>
                `
})

export class MonitorWorkflowComponent extends BaseComponent implements OnInit {
    sub: any;
    objectType: string;
    objectID: number;

    constructor(
        private route: ActivatedRoute,
        private assetService: AssetService,
        breadcrumbService: HeaderBreadcrumbService,
        secondaryNavService: SecondaryNavService
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            let reloadNav = params['isAdminPage'] && params['isAdminPage'] == 'false' ? false : true;
            let assetUid = params['assetUid'];
            if (assetUid == null || assetUid == undefined) {
                this.objectID = +params['objectId'];
                this.objectType = params['objectType'];
                if (reloadNav)
                    this.buildSecondaryNavigationForObject(this.objectID, this.objectType);
            }
            else
            {
                this.assetService.getUIDetailsForAssetUID(assetUid)
                .subscribe(res => {
                    this.objectID = +res.ObjectId;
                    this.objectType = res.Object;
                    if (reloadNav)
                        this.buildSecondaryNavigationForObject(this.objectID, this.objectType);
                });
            }
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
};