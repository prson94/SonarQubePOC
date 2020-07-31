import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';

@Component({
    selector: 'd3s-workflow-monitor',
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
        breadcrumbService: HeaderBreadcrumbService,
        secondaryNavService: SecondaryNavService
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId'];
            this.objectType = params['objectType'];

            let reloadNav = params['isAdminPage'] && params['isAdminPage'] == 'false' ? false : true;

            if (reloadNav)
                this.buildSecondaryNavigationForObject(this.objectID, this.objectType);
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
};