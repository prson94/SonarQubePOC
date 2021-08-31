import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { LazyLoadEvent } from 'primeng/api';

import { BaseComponent } from '../../shared/base.component';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { AuditService } from '../../../services/audit.service';
import { Audit, AuditApiFilters } from '../../../models/audit.model';
import { SortOrder } from '../../../models/enums.model';
import { GridFilterExpression } from '../../../models/grid-definition.model';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { AdvancedFiltersHelper } from '../../../static/advanced-filter-helpers';
import { PredicateFriendlyType } from '../../../models/predicate.model';

@Component({
    selector: 'd3s-audit',
    providers: [AuditService],
    templateUrl: './audit.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class AuditComponent extends BaseComponent implements OnInit, OnDestroy {
    totalRecords: number;
    rowsPerPage: number = 10;
    audits: Audit[] = [];
    private sub: any;

    selected: Audit;
    currentPageNumber: number = 0;
    sortField: string = undefined;
    sortOrder: SortOrder = SortOrder.None;
    filters: GridFilterExpression[] = [];

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private auditService: AuditService,
        private changeDetectorRef: ChangeDetectorRef,
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

                this.uid = params['uid'];

                this.auditService.getLegacyDetails(this.uid).subscribe(res => {
                    this.objectName = res.DisplayValue;
                    this.objectID = res.ObjectId;
                    this.objectType = res.Object;

                    if (this.objectName === "MetricAllocation") {
                        this.objectName = "Score Definition";
                    }
                    let reloadNav = params['isAdminPage'] && params['isAdminPage'] == 'false' ? false : true;

                    //do not reload 2nd navigation for audit page as both grid pages and config pages share same URL
                    if (['PolicyType', 'TaxonomyType', 'Report', 'ResponsibilityType'].indexOf(this.objectType) > -1)
                        reloadNav = false;

                    let objectID = this.objectType == 'Tag' ? params['uid'] : this.objectID;

                    if (reloadNav)
                        this.buildSecondaryNavigationForObject(objectID, this.objectType);
                });
            });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }

    getData() {
        this.isLoading = true;
        this
            .auditService
            .getAuditData(this.uid, this.getParams())
            .subscribe(result => {
                this.isLoading = false;
                result.items.forEach((object) => {
                    if ((object.actionObject == "ArtifactType" && object.class == 1) || (object.actionObject == "Artifact" && object.class == 1)) {
                        object.actionObject = "Business Asset";
                        if (object.actionDescription.includes("Artifact")) {
                            object.actionDescription = object.actionDescription.replace("ArtifactType", "Business Asset");
                            object.actionDescription = object.actionDescription.replace("Artifact", "Business Asset");
                        }
                    }
                    if ((object.actionObject == "ArtifactType" && object.class == 8) || (object.actionObject == "Artifact" && object.class == 8)) {
                        object.actionObject = "Technical Asset";
                        if (object.actionDescription.includes("Artifact")) {
                            object.actionDescription = object.actionDescription.replace("ArtifactType", "Technical Asset");
                            object.actionDescription = object.actionDescription.replace("Artifact", "Technical Asset");
                        }
                    }
                });
                this.audits = <Audit[]>result.items;
                this.totalRecords = result.total;
                this.changeDetectorRef.markForCheck();
            });
    }

    public loadAuditsLazy(event: LazyLoadEvent) {
        //event.first = First row offset
        //event.rows = Number of rows per page
        //event.sortField = Field name to sort with
        //event.sortOrder = Sort order as number, 1 for asc and -1 for dec
        //filters: FilterMetadata object having field as key and filter value, filter matchMode as value        
        this.filters.splice(0, this.filters.length);

        for (var key in event.filters) {
            const filter = event.filters[key];
            let gridFilter = new GridFilterExpression();

            gridFilter.condition = "CONTAINS";
            gridFilter.field = key;
            gridFilter.value = filter.value;

            this.filters.push(gridFilter);
        }

        this.sortOrder = event.sortOrder;
        this.sortField = event.sortField == undefined ? "" : event.sortField;
        this.rowsPerPage = event.rows;
        this.currentPageNumber = event.first / event.rows;
        this.getData();
    }

    public export() {
        var fileName = this.objectName;
        if (this.objectID === 0) {
            fileName = this.objectType;
        }

        this
            .auditService
            .exportToExcel(
                this.uid,
                this.getParams(),
                fileName
            );
    }

    private getParams() {
        var params = new AuditApiFilters();
        params._pageSize = this.rowsPerPage;
        params._pageNum = this.currentPageNumber + 1;

        if (this.sortField) {
            params._order = this.sortField;
        }
        else {
            delete params['_order'];
        }

        if (this.sortOrder != SortOrder.None)
            params._direction = this.sortOrder == SortOrder.Ascending ? "asc" : "desc";
        else {
            delete params['_direction'];
        }
        if (this.filters && this.filters.length > 0) {
            let expressions: string[] = [];
            this.filters.forEach(f => {
                let apiName = f.field;
                let val = AdvancedFiltersHelper.escapeString(f.value);
                if (apiName === 'version') {
                    //handle version (revision) as a number, not a string
                    expressions.push(`${apiName} eq ${val}`);
                }
                else {
                    expressions.push(`${apiName} ct '${val}'`);
                }
            });
            params._filter = expressions.join(' and ');
        }
        else {
            delete params['_filter'];
        }
        return params;
    }
}
