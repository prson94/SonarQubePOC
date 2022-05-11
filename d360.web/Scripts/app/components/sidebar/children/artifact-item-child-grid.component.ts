import { Input, Component, OnInit, OnChanges, SimpleChange, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';

import { BaseComponent } from '../../shared/base.component';
import { ArtifactService } from '../../../services/artifacts.service';
import { AssetService } from '../../../services/asset.service';
import { GridDefinitionService } from '../../../services/grid-definition.service';
import { GridColumn, GridField, GridScoreAllocation } from '../../../models/grid-definition.model';
import { SortOrder } from '../../../models/enums.model';
import { Artifacts } from '../../../models/artifacts.model';
import { LazyLoadEvent } from 'primeng/api';
import { Table } from 'primeng/table';
import { debounceTime } from 'rxjs/operators';
import { ObjectStatistics } from '../../../models/object-statistics.model';
import { ObjectStatisticsService } from '../../../services/object-statistics.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-artifact-item-child-grid',
    templateUrl: './artifact-item-child-grid.component.html',
    providers: [ArtifactService, GridDefinitionService, AssetService],
})

export class ArtifactItemChildGridComponent extends BaseComponent implements OnChanges {
    @Input() artifactTypeId: number;
    @Input() parentId: number;
    @Input() parentUid: string;
    @Input() showFilter: boolean;
    @Input() assetTypeUid: string;
    @Input() objectTypeUid: string;
    @Input() displayName: string;
    @Input() assettypename: string;

    columns: GridColumn[] = [];
    fields: GridField[] = [];
    scoreAllocations: GridScoreAllocation[] = [];
    artifacts: Artifacts;
    private searchDelayMilliSeconds: number = 300;
    private simpleSearchID: number = 0;
    private totalRecords: number = 10000;

    private useGraph: boolean = true;

    exportMsg: string = $localize`Export not available for over ${this.maxExportRows} rows`;

    numberOfRows: number = this.defaultInitialItemsPerPage;
    currentPage: number = 1;
    sortField: string;
    sortOrder: SortOrder;
    filter: string;
    statistics: ObjectStatistics;
    isLoading: boolean = false;
    subjectUid: string;

    get globalFilterFields(): string[] {
        return this.columns.map(c => c.datafield);
    }

    constructor(
        protected router: Router,
        protected gridDefinitionService: GridDefinitionService,
        protected artifactService: ArtifactService,
        protected assetService: AssetService,
        protected objectStatisticsService: ObjectStatisticsService,
        protected settingsService: CompanySettingsService,
        private ref: ChangeDetectorRef
    ) {
        super(settingsService);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['artifactTypeId'] && this.artifactTypeId > 0) {
            if (this.artifacts) this.artifacts = undefined;
            this.getFieldsDefinition();
        }
    }

    loadArtifactsLazy(event: LazyLoadEvent) {
        /**
         * event.first = First row offset
         * event.rows = Number of rows per page
         * event.sortField = Field name to sort with
         * event.sortOrder = Sort order as number, 1 for asc and -1 for dec
         * filters: FilterMetadata object having field as key and filter value, filter matchMode as value
         */

        this.filter = "";
        for (var key in event.filters) {
            this.filter = event.filters[key].value;
            break;
        }

        this.sortOrder = event.sortOrder;
        this.sortField = event.sortField == undefined ? "" : this.getFieldApiName(event.sortField);
        this.numberOfRows = event.rows;
        this.currentPage = (event.first / event.rows) + 1;
        this.getData();
    }

    getData() {
        this.isLoading = true;
        this.assetService.getArtifactType(this.artifactTypeId).subscribe(i => {
            this.subjectUid = i.uid;

            this.assetService.getAssets(i.uid, this.getParams()).pipe(
                debounceTime(500)).subscribe(res => {
                    this.totalRecords = res.total;
                    this.artifacts = res;
                    if (this.scoreAllocations && this.scoreAllocations.length > 0) {
                        res.items.forEach((i) => {
                            this.scoreAllocations.forEach((s) => {
                                i[s.Name + '_threshold'] = this.getThreshold(i[s.Name], s.LowerThreshold, s.UpperThreshold);
                            });
                        });
                    }

                    if (this.totalRecords < 1000) {
                        this.useGraph = false;
                    }

                    this.isLoading = false;
                    this.ref.markForCheck();
                });
        });
    }

    getThreshold(value: string, lower: number, upper: number): string {
        if (value == null || value.length < 1) {
            return '';
        }
        if (value.indexOf('%') > -1) {
            value = value.replace('%', '');
        }
        if (isNaN(+value)) {
            return '';
        }

        let v = +value;

        if (v <= lower) {
            return 'poor';
        }
        else if (v > lower && v <= upper) {
            return 'average';
        }
        else {
            return 'good';
        }

    }

    getParams() {
        let sortOrderText = this.sortOrder == SortOrder.None ? "" : (this.sortOrder == SortOrder.Descending ? "desc" : "asc");
        var params = { _pagesize: this.numberOfRows, _pagenum: this.currentPage, _subjectUid: this.subjectUid, _filter: "ParentUid eq '" + this.parentUid + "'", _order: this.sortField, _direction: sortOrderText, _simpleFilter: this.filter, _includeParent: true, useGraphForParent: this.useGraph, useTypeLevelDefaultSorts: true, _listColorsAsJSON: true };

        if (params._order === '' || typeof params._order === "undefined") {
            delete params['_order'];
        }
        else {
            delete params['useTypeLevelDefaultSorts'];
        }

        params['_ischildtab'] = true;

        return params;
    }

    getFieldsDefinition() {
        this.isLoading = true;
        this.gridDefinitionService.getGridDefinition(this.artifactTypeId, "ArtifactType").subscribe(
            result => {
                this.columns = result.Columns.filter(x => x.datafield != 'Name');
                /* remove name we want it to be a cool link with tooltip we know its there! */
                this.fields = result.Fields;
                this.scoreAllocations = result.ScoreAllocations;

                this.isLoading = false;
                this.getData();
            }
        );
    }

    getFieldApiName(field: string) {
        return this.fields.find(x => x.name == field).apiName;
    }

    private checkSimpleSearchEnter(event, dt: Table) {
        if (event.keyCode == 13) {
            this.doSimpleSearch(dt);
        } else {
            if (this.simpleSearchID > 0) {
                window.clearTimeout(this.simpleSearchID);
                this.simpleSearchID = 0;
            }

            this.simpleSearchID = window.setTimeout(() => this.doSimpleSearch(dt), this.searchDelayMilliSeconds);
        }
    }

    private doSimpleSearch(dt: Table) {
        if (dt) {
            dt.reset();
        }

        this.getData();
    }

    selectArtifact(artifact) {
        this
            .router
            .navigateByUrl(
                `asset/${artifact.AssetUid}`);
    }

    canExportRecords() {
        return this.totalRecords <= this.maxExportRows;
    }
    export() {
        var FileName = this.assettypename.charAt(0).toUpperCase() + this.assettypename.substring(1).toLowerCase();
        this.isLoading = true;
        this.assetService
            .downloadAssetsExcel(
                this.subjectUid,
                this.getParams(),
                $localize`Filtered ${FileName} list`,
                () => { this.isLoading = false; }
            );
    }
}
