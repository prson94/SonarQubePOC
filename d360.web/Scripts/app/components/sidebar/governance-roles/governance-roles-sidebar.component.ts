import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { LazyLoadEvent } from 'primeng/api';

import { BaseComponent } from '../../shared/base.component';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { AuditService } from '../../../services/audit.service';
import { Audit } from '../../../models/audit.model';
import { SortOrder } from '../../../models/enums.model';
import { GridFilterExpression } from '../../../models/grid-definition.model';
import { SecondaryNavService } from '../../../services/right-sidebar.service';

@Component({
    selector: 'd3s-governance-roles',
    providers: [AuditService, ObjectDetailService],
    templateUrl: './governance-roles-sidebar.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GovernanceRolesComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    constructor(
        private route: ActivatedRoute,
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {
        this.sub = this
            .route
            .params
            .subscribe(params => {
                this.buildSecondaryNavigationForObject(0, 'TaskType');
            });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}
