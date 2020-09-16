import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';

declare var CompanySettings: any;

@Component({
    selector: 'd3s-lineage-wrapper',
    template: `
        <ng-container>
            <d3s-lineage [objectID]="objectID" [objectType]="objectType" [readonly]="true" [usageOnly]="usageOnly"></d3s-lineage>
        </ng-container>
        `
})

export class LineageComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    usageOnly: boolean = false;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId']; // (+) converts string 'id' to a number
            this.objectType = params['objectType'];
            this.usageOnly = params['showUsageOnly'] == '1';
            this.buildSecondaryNavigationForObject(this.objectID, this.objectType);

        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}
