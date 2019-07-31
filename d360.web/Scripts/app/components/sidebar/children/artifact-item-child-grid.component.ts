import {Input, Component, OnInit, OnChanges, SimpleChange, ChangeDetectionStrategy} from '@angular/core';
import {Router} from '@angular/router';

import {BaseComponent} from '../../shared/base.component';
import {ArtifactService} from '../../../services/artifacts.service';
import {GridDefinitionService} from '../../../services/grid-definition.service';
import {GridColumn, GridField} from '../../../models/grid-definition.model';
import {SortOrder} from '../../../models/enums.model';
import {Artifacts} from '../../../models/artifacts.model';
import {LazyLoadEvent, DataTable} from 'primeng/primeng';
import {SiteUrlHelpers} from '../../../static/site-url-helpers';
import {StringConstants} from '../../../static/string-constants';
import {  debounceTime } from 'rxjs/operators';

@Component({
    selector: 'd3s-artifact-item-child-grid',
    templateUrl: './artifact-item-child-grid.component.html',
    providers: [ArtifactService, GridDefinitionService],
})

export class ArtifactItemChildGridComponent extends BaseComponent implements OnChanges {
    @Input() artifactTypeId: number;
    @Input() parentId: number;
    @Input() showFilter: boolean;

    private columns: GridColumn[] = [];
    private fields: GridField[] = [];
    private artifacts: Artifacts;
    private searchDelayMilliSeconds: number = 300;
    private simpleSearchID: number = 0;

    private numberOfRows: number = this.defaultInitialItemsPerPage;
    private currentPage: number = 0;
    private sortField: string;
    private sortOrder: SortOrder;
    private filter: string;

    get globalFilterFields(): string[] {
        return this.columns.map(c => c.datafield);
    }

    constructor(
        protected router: Router,
        protected gridDefinitionService: GridDefinitionService,
        protected artifactService: ArtifactService
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
        this
            .artifactService
            .getArtifactByParentAndArtifactType(
                this.parentId,
                this.artifactTypeId,
                this.filter,
                this.numberOfRows,
                this.currentPage,
                this.sortField,
                this.sortOrder
            )
            .pipe(debounceTime(this.searchDelayMilliSeconds))
            .subscribe(
                res => {
                    this.artifacts = res;
                }
            )
        ;
    }

    getFieldsDefinition() {
        this.isLoading = true;
        this.gridDefinitionService.getGridDefinition(this.artifactTypeId, "ArtifactType").subscribe(
            result => {
                this.columns = result.Columns.filter(x => x.datafield != 'Name');
                /* remove name we want it to be a cool link with tooltip we know its there! */
                this.fields = result.Fields;

                this.isLoading = false;
            }
        );
    }

    private checkSimpleSearchEnter(event, dt: DataTable) {
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

    private doSimpleSearch(dt: DataTable) {
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
                artifact.ID,
                this.artifactTypeId
                )
            )
        ;
    }
}
