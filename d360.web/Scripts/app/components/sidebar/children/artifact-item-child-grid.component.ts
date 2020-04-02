import {Input, Component, OnInit, OnChanges, SimpleChange, ChangeDetectionStrategy, ChangeDetectorRef} from '@angular/core';
import {Router} from '@angular/router';

import {BaseComponent} from '../../shared/base.component';
import { ArtifactService } from '../../../services/artifacts.service';
import { AssetService } from '../../../services/asset.service';
import {GridDefinitionService} from '../../../services/grid-definition.service';
import {GridColumn, GridField} from '../../../models/grid-definition.model';
import {SortOrder} from '../../../models/enums.model';
import {Artifacts} from '../../../models/artifacts.model';
import { LazyLoadEvent } from 'primeng/api';
import { Table } from 'primeng/table';
import {SiteUrlHelpers} from '../../../static/site-url-helpers';
import {StringConstants} from '../../../static/string-constants';
import {  debounceTime } from 'rxjs/operators';
import { ObjectStatistics } from '../../../models/object-statistics.model';
import { ObjectStatisticsService } from '../../../services/object-statistics.service';

@Component({
    selector: 'd3s-artifact-item-child-grid',
    templateUrl: './artifact-item-child-grid.component.html',
    providers: [ArtifactService, GridDefinitionService, AssetService],
})

export class ArtifactItemChildGridComponent extends BaseComponent implements OnChanges {
    @Input() artifactTypeId: number;
    @Input() parentId: number;
    @Input() showFilter: boolean;
    @Input() assetTypeUid: string;
    @Input() objectTypeUid: string;
    @Input() displayName: string;

    private columns: GridColumn[] = [];
    private fields: GridField[] = [];
    private artifacts: Artifacts;
    private searchDelayMilliSeconds: number = 300;
    private simpleSearchID: number = 0;
    private totalRecords: number = 10000;
    private useGraph: boolean = true;

    private numberOfRows: number = this.defaultInitialItemsPerPage;
    private currentPage: number = 0;
    private sortField: string;
    private sortOrder: SortOrder;
    private filter: string;
    private statistics: ObjectStatistics;
    isLoading: boolean = false;

    get globalFilterFields(): string[] {
        return this.columns.map(c => c.datafield);
    }

    constructor(
        protected router: Router,
        protected gridDefinitionService: GridDefinitionService,
        protected artifactService: ArtifactService,
        protected assetService: AssetService,
        protected objectStatisticsService: ObjectStatisticsService,
        private ref: ChangeDetectorRef
    ) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['artifactTypeId'] && this.artifactTypeId > 0) {
            if (this.artifacts) this.artifacts = undefined;
            this.getFieldsDefinition();
        }
    }

    private loadArtifactsLazy(event: LazyLoadEvent) {
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
        this.sortField = event.sortField == undefined ? "" : event.sortField;
        this.numberOfRows = event.rows;
        this.currentPage = event.first / event.rows;
        this.getData();
    }

    getData() {
        this.assetService.getArtifactType(this.artifactTypeId).subscribe(i => {
            console.log(this.displayName);
            let sortOrderText = this.sortOrder == SortOrder.None ? "" : (this.sortOrder == SortOrder.Descending ? "desc" : "asc");
            var params = { pagesize: this.numberOfRows, pagenum: this.currentPage, _subjectUid: i.uid, _filter: "ParentDisplayName eq '" + this.displayName + "'", _order: 'name', _direction: sortOrderText, _simpleFilter: this.filter, _includeParent: true, useGraphForParent: this.useGraph };
            //var params = { pagesize: this.numberOfRows, pagenum: this.currentPage, _subjectUid: i.uid, _filter: "ParentDisplayName eq 'shane'", _order: 'name', _direction: sortOrderText, _simpleFilter: this.filter, _includeParent: true, useGraphForParent: this.useGraph };
            this.assetService.getAssets(i.uid, params).subscribe(res => {
                this.totalRecords = res.total;
                this.artifacts = res;

                if (this.totalRecords < 1000) {
                    this.useGraph = false;
                }

                this.isLoading = false;
                this.ref.detectChanges();
            });
        });
    }

    getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.artifactTypeId, "ArtifactType").subscribe(
            result => {
                this.columns = result.Columns.filter(x => x.datafield != 'Name');
                /* remove name we want it to be a cool link with tooltip we know its there! */
                this.fields = result.Fields;

                this.isLoading = false;
            }
        );
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

        this.currentPage = 0;
        this.getData();
    }

    selectArtifact(artifact) {
        this
            .router
            .navigateByUrl(SiteUrlHelpers.getObjectUrl(
                'Artifact',
                artifact.ObjectID,
                this.artifactTypeId
            )
            )
            ;
    }
}
