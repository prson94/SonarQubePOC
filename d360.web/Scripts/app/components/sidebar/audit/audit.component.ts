import {ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnDestroy, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import { LazyLoadEvent } from 'primeng/api';

import {BaseComponent} from '../../shared/base.component';
import {HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import {ObjectDetailService} from '../../../services/object-detail.service';
import {AuditService} from '../../../services/audit.service';
import {Audit} from '../../../models/audit.model';
import {SortOrder} from '../../../models/enums.model';
import {GridFilterExpression} from '../../../models/grid-definition.model';

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
        this.sub = this
            .route
            .params
            .subscribe(params => {
                this.objectID = +params['objectId']; // (+) converts string 'id' to a number
                if (params['objectId'].length == 36) {
                    this.objectID = params['objectId'];
                }

                this.objectType = params['objectType'];

                this
                    .objectDetailService
                    .getObject(
                        this.objectID,
                        this.objectType
                    )
                    .subscribe(
                        res => {
                            if (res) {
                                this.objectName = res.Name ? res.Name : res.DisplayValue;
                            }
                        }
                    );
            });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    private getData() {
        this.isLoading = true;

        this
            .auditService
            .getAuditData(
                this.objectID,
                this.objectType,
                this.currentPageNumber,
                this.rowsPerPage,
                this.sortOrder,
                this.sortField,
                this.filters
            )
            .subscribe(result => {
                this.isLoading = false;
                result.results.forEach(function (object) {
                    if (object.ActionObject == "ArtifactType" && object.Class == 1) {
                        object.ActionObject = "Business Asset";
                        object.ActionObjectTypeName = "Business Asset";
                    }
                    if (object.ActionObject == "ArtifactType" && object.Class == 8) {
                        object.ActionObject = "Technical Asset";
                        object.ActionObjectTypeName = "Technical Asset";
                    }
                });
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
                this.objectID,
                this.objectType,
                fileName,
                this.filters
            );
    }
}
