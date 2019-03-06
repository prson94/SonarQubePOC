import { Component, Input, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { AuditService } from '../../../services/audit.service';
import { Audit } from '../../../models/audit.model';
import { LazyLoadEvent } from 'primeng/primeng';
import { SortOrder } from '../../../models/enums.model';
import { GridFilterExpression } from '../../../models/grid-definition.model';
import { BaseComponent } from '../../shared/base.component';
import { ObjectDetailService } from '../../../services/object-detail.service';

@Component({
    selector: 'd3s-audit',
    providers: [AuditService, ObjectDetailService],
    templateUrl: './audit.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class AuditComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;

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
        private headerBreadcrumbService: HeaderBreadcrumbService,
        private objectDetailService: ObjectDetailService,
        private changeDetectorRef: ChangeDetectorRef
    ) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId']; // (+) converts string 'id' to a number
            this.objectType = params['objectType'];

            this.objectDetailService.getObject(this.objectID, this.objectType).then(res => {
                if (res) this.objectName = res.Name ? res.Name : res.DisplayValue;
            });
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    private getData() {
        this.isLoading = true;
        this.auditService.getAuditData(this.objectID, this.objectType, this.currentPageNumber, this.rowsPerPage, this.sortOrder, this.sortField, this.filters)
            .then(result => {
                this.isLoading = false;
                this.audits = result.results;
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
            var filter = event.filters[key];

            var gridFilter = new GridFilterExpression();
            gridFilter.condition = "CONTAINS"
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
        this.auditService.exportToExcel(this.objectID, this.objectType, this.objectName, this.filters);
    }
}
