import { Input, Component, OnInit, OnChanges, SimpleChange, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent} from '../shared/base.component';
import { ArtifactService, GridDefinitionService } from '../../services/index';
import { GridColumn, GridField } from '../../models/grid-definition.model';
import { SortOrder } from '../../models/enums.model';
import { Artifacts } from '../../models/artifacts.model';
import { LazyLoadEvent, DataTable } from 'primeng/primeng';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';

@Component({
    selector: 'd3s-artifact-item-child-grid',
    template: `            
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">        
                        <input type="text" class="grid-simple-filter" maxlength="200" (keyup)="checkSimpleSearchEnter($event,dt);" [(ngModel)]="filter" placeholder="Search..." autofocus autocomplete="off" />                                             
                        <p-dataTable #dt lazy="true" [totalRecords]="artifacts?.total" [value]="artifacts?.results" selectionMode="single" [rows]="numberOfRows" paginator="true" pageLinks="3" (onLazyLoad)="loadArtifactsLazy($event)" [rowsPerPageOptions]="defaultPagingOptions">                                                                       
                            <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                            <p-column field="Name" header="Name" sortable="true"  [style]="{'width':'250px'}">
                                <template let-item="rowData" pTemplate type="body">
                                    <d3s-tooltip objectType="Artifact" [objectId]="item.ID" tooltipType="preview"><a (click)="selectArtifact(item)">{{item.Name}}</a></d3s-tooltip>
                                </template>
                            </p-column>
                            <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [sortable]="column.sortable">
                                <template let-item="rowData" pTemplate type="body">
                                    <d3s-dynamic-field-value [column]="column" [fields]="fields" [item]="item"></d3s-dynamic-field-value>                                                                 
                                </template>
                            </p-column>                        
                        </p-dataTable>                   
                    </span>
                `,    
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
    

    constructor(protected router: Router, protected gridDefinitionService: GridDefinitionService, protected artifactService: ArtifactService) {
        super();
    }
    
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['artifactTypeId'] && this.artifactTypeId > 0) {
            if (this.artifacts) this.artifacts = undefined;
            this.getFieldsDefinition();            
        }
    }

    private loadArtifactsLazy(event: LazyLoadEvent) {
        //event.first = First row offset
        //event.rows = Number of rows per page
        //event.sortField = Field name to sort with
        //event.sortOrder = Sort order as number, 1 for asc and -1 for dec
        //filters: FilterMetadata object having field as key and filter value, filter matchMode as value

        this.sortOrder = event.sortOrder;
        this.sortField = event.sortField == undefined ? "" : event.sortField;
        this.numberOfRows = event.rows;
        this.currentPage = event.first / event.rows;
        this.getData();
    }

    getData() {
        this.artifactService.getArtifactByParentAndArtifactType(this.parentId, this.artifactTypeId, this.filter, this.numberOfRows, this.currentPage, this.sortField, this.sortOrder).
            then(res => {
                this.artifacts = res;

            });
    }
    

    getFieldsDefinition() {
        this.isLoading = true;
        this.gridDefinitionService.getGridDefinition(this.artifactTypeId, "ArtifactType")
            .then(result => {
                this.columns = result.Columns.filter(x => x.datafield != 'Name'); //remove name we want it to be a cool link with tooltip we know its there!                
                this.fields = result.Fields;
                this.isLoading = false;
            });
    }    

    private checkSimpleSearchEnter(event, dt: DataTable) {
        if (event.keyCode == 13) this.doSimpleSearch(dt);
        else {
            if (this.simpleSearchID > 0) {
                window.clearTimeout(this.simpleSearchID);
                this.simpleSearchID = 0;
            }

            this.simpleSearchID = window.setTimeout(() => this.doSimpleSearch(dt), this.searchDelayMilliSeconds);
        }
    }

    private doSimpleSearch(dt: DataTable) {
        if (dt) dt.reset();
        this.currentPage = 0;
        this.getData();
    }

    selectArtifact(artifact) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('Artifact', artifact.ID, this.artifactTypeId));
    }

};