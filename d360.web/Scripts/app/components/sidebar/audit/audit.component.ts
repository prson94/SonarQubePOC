import {ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnDestroy, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import { LazyLoadEvent } from 'primeng/api';

import {BaseComponent} from '../../shared/base.component';
import {HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import {AuditService} from '../../../services/audit.service';
import {Audit, AuditApiFilters} from '../../../models/audit.model';
import {SortOrder} from '../../../models/enums.model';
import {GridColumn, GridFilterExpression} from "../../../models/grid-definition.model";
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { FieldType } from "../../../models/fieldtype-api.model";
import { AdvancedFilterFieldType, Filters } from "../../assets-grid/advanced-filtering/advanced-filtering.models";
import { Observable, of, ReplaySubject } from "rxjs";

@Component({
    selector: 'd3s-audit',
    providers: [AuditService],
    templateUrl: './audit.component.html',
    styleUrls: ["./audit.component.less"],
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

    filterFields$: Observable<AdvancedFilterFieldType[]>;
    private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);

    advancedFilter: string = "";
    isFiltersReady: boolean = false;
    columns: GridColumn[] = [];
    isExportInProgress: boolean = false;

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

        this.columns = [];
        this.columns.push({ text: "User", datafield: "resourceName", columnWidth: 150, fieldType: "Text", type: "", cellsformat: "", description: "" });
        this.columns.push({ text: "Date", datafield: "date", columnWidth: 200, fieldType: "DateTime", type: "", cellsformat: "", description: "" });
        this.columns.push({ text: "Action", datafield: "action", columnWidth: 100, fieldType: "Text", type: "", cellsformat: "", description: "" });
        this.columns.push({ text: "Field", datafield: "field", columnWidth: 200, fieldType: "Text", type: "", cellsformat: "", description: "" });
        this.columns.push({ text: "New Value", datafield: "newValue", columnWidth: 250, fieldType: "Text", type: "", cellsformat: "", description: "" });
        this.columns.push({ text: "Previous Value", datafield: "previousValue", columnWidth: 250, fieldType: "Text", type: "", cellsformat: "", description: "" });
        this.columns.push({ text: "Object", datafield: "actionObject", columnWidth: 130, fieldType: "Text", type: "", cellsformat: "", description: "" });
        this.columns.push({ text: "Type", datafield: "actionObjectTypeName", columnWidth: 130, fieldType: "Text", type: "", cellsformat: "", description: "" });
        this.columns.push({ text: "Item", datafield: "actionObjectName", columnWidth: 100, fieldType: "Text", type: "", cellsformat: "", description: "" });
        this.columns.push({ text: "Audit Description", datafield: "actionDescription", columnWidth: 250, fieldType: "Text", type: "", cellsformat: "", description: "" });
        this.columns.push({ text: "Revision", datafield: "version", columnWidth: 100, fieldType: "Number", type: "", cellsformat: "", description: "" });

        this.filterFields$ = this.filterFieldsSubject.asObservable();
    }

    ngOnInit() {
        this.isFiltersReady = false;


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

                this.setAdvancedFilters();
            });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }

    getData() {
        this.isLoading = true;
        if (!this.isFiltersReady) {
            return;
        }
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
        if (this.advancedFilter === "") {
            delete params['_filter'];
        } else {
            params._filter = this.advancedFilter;
        }

        return params;
    }

    public getFieldNames(): string[] {
        return this.columns.map((c) => c.datafield);
    }

    public getLoadIdentifier() {
        return "changelog-" + this.uid;
    }

    public onFiltersLoaded() {
        this.isFiltersReady = true;
        this.getData();
    }
    advancedFiltersChanged($event: Filters) {
        this.advancedFilter = $event?.filter ?? "";
        this.getData();
    }
    public canExportRecords() {
        return true;
    }

    public setAdvancedFilters():void {
        this.auditService.getFilterLists(this.uid).subscribe((lists) => {
            let fields: AdvancedFilterFieldType[] = [];

            this.columns.forEach((c) => {
                let field: AdvancedFilterFieldType = {
                    Name: c.datafield,
                    FriendlyName: c.text,
                    Type: lists[c.datafield] ? new FieldType("Lookup") : new FieldType(c.fieldType),
                    Category: "",
                    RemovePopulatedOperator: ["newValue", "previousValue"].indexOf(c.datafield) === -1
                }
                if (lists[c.datafield]) {
                    field.ValueList = lists[c.datafield].map((l) => {
                        return {value: l, title: l};
                    });
                }
                fields.push(field);
            });
            this.filterFieldsSubject.next(fields);
            this.filterFieldsSubject.complete();

        });
    }
}
