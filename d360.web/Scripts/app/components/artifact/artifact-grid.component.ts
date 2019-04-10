import {of as observableOf, Subject, Observable} from 'rxjs';
import {debounceTime, map, distinctUntilChanged, delay, mergeMap} from 'rxjs/operators';
import {
    Component,
    Input,
    Output,
    OnChanges,
    SimpleChange,
    EventEmitter,
    ViewChild,
    OnInit,
    ChangeDetectionStrategy,
    ChangeDetectorRef
} from '@angular/core';
import {LazyLoadEvent, DataTable} from 'primeng/primeng';
import {Router, ActivatedRoute} from '@angular/router';

import {Lookup, LookupItem} from '../../models/lookup.model';
import {
    GridDefinition,
    GridColumn,
    GridField,
    GridFilterColumn,
    GridFilterExpression,
    GridRelationshipFilterExpression,
    GridAttributeFilterExpression
} from '../../models/grid-definition.model';
import {MessagesService} from '../../services/messages.service';
import {GridDefinitionService} from '../../services/grid-definition.service';
import {ArtifactService} from '../../services/artifacts.service';
import {PermissionsService} from '../../services/permissions.service';
import {StateService} from '../../services/state.service';
import {HeaderActionsService} from '../../services/header-actions.service';
import {ArtifactType} from '../../models/artifact-type.model';
import {BaseComponent} from '../shared/base.component';
import {SiteUrlHelpers} from '../../static/site-url-helpers';
import {StringConstants} from '../../static/string-constants';
import {ObjectDetailService} from '../../services/object-detail.service';

@Component({
    selector: 'd3s-artifact-grid',
    providers: [GridDefinitionService, ArtifactService, PermissionsService, ObjectDetailService],
    templateUrl: './artifact-grid.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '(document:click)': 'clickedOutside()',
    },
})

export class ArtifactGridComponent extends BaseComponent implements OnChanges {
    @Input() rowID: string = 'ObjectID';
    @Input() artifactType: ArtifactType;
    @Input() titlePostfix: string = ''; // added to end of header title.
    @Input() rowsPerPage: number = 25;
    @ViewChild('dt') dt: DataTable;

    showEditButton: boolean = true;
    showDeleteButton: boolean = true;
    showAddButton: boolean = true;
    showCustomExport: boolean = false;
    isEditing: boolean = false;
    isMenuOpen: boolean = false;
    showArtifactDetails: boolean = false;
    showCertificationStatus: boolean = false;
    certificationStatusIndex: string = null;

    totalRecords: number;

    searchValue: string = "";

    searchDelayMilliSeconds: number = 500;
    error: any;
    items: any[];
    columns: GridColumn[] = [];
    fields: GridField[] = [];
    filtercolumns: GridFilterColumn[] = [];
    topLevelFilters: GridFilterColumn[] = [];

    showDelete: boolean = false;
    showEditor: boolean = false;
    isLoading: boolean = false;

    selected: any = null;
    itemUrl: string;

    theDeleteCallback: Function;

    public simpleSearch = new Subject<any>();

    get globalFilterFields(): string[] {
        return this.columns.map(c => c.datafield);
    }

    constructor(
        private headerActionsService: HeaderActionsService,
        private messagesService: MessagesService,
        private stateService: StateService,
        private permissionsService: PermissionsService,
        private router: Router,
        private gridDefinitionService: GridDefinitionService,
        private artifactService: ArtifactService,
        private changeDetectorRef: ChangeDetectorRef,
        private objectDetailService: ObjectDetailService
    ) {
        super();

        this.theDeleteCallback = this.deleteItem.bind(this);
        var me = this;

        const subscription = this.simpleSearch.pipe(
            map(event => event.target.value),
            debounceTime(1000),
            distinctUntilChanged(),
            mergeMap(
                search => observableOf(search).pipe(delay(500))
            )
        )
            .subscribe(
                data => {
                    this.doSimpleSearch(me.dt, me.isLoading);
                }
            );
    }

    get showGridSimpleFilter(): boolean {
        return this.stateService.artifactTypeFilters.showSimpleFilter;
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['artifactType'] && this.artifactType != null) {
            this.load();
        }

        //clear out the filters if the artifacttype is different
        this.stateService.resetArtifactTypeFilterIfRequired(this.artifactType.ID);
    }

    load() {
        this
            .loadPermissions(this.permissionsService, StringConstants.ObjectArtifactType, this.artifactType.ID)
            .then(() => this.changeDetectorRef.markForCheck())
        ;

        this.getFieldsDefinition();

        if (this.artifactType.AutoDisplayDescription) {
            this.toggleArtifactDetail();
        }
    }

    public filterGridData() {
        this.isLoading = true;
        this.stateService.artifactTypeFilters.currentPageNumber = 0;
        this.getData();
    }

    resetFilters(val) {
        this.stateService.artifactTypeFilters.showSimpleFilter = val;
        this.stateService.artifactTypeFilters.simpleTextFilter = '';
        this.stateService.artifactTypeFilters.filters = [];
        this.stateService.artifactTypeFilters.attributes = [];
        this.stateService.artifactTypeFilters.relationships = [];
        this.stateService.artifactTypeFilters.owners = null;

        this.filterGridData();
    }

    deleteItem(id: number) {
        this
            .artifactService
            .deleteArtifact(id)
            .subscribe(
                result => {
                    this.showMessageForResult(this.messagesService, result);
                    this.headerActionsService.emitFavoritesChange(); // favorites need to be reloaded if an object was removed
                    this.showDelete = false;
                    this.getData();
                    this.changeDetectorRef.markForCheck();
                }
            )
        ;
    }

    getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.artifactType.ID, StringConstants.ObjectArtifactType).subscribe(
            result => {
                let statusField;

                this.columns = result.Columns.filter(x => x.datafield != 'Name');
                this.filtercolumns = result.FilterColumns;
                this.fields = result.Fields;
                this.topLevelFilters = result.TopLevelFilterColumns;

                statusField = this.fields.find(x => x.apiName != null && x.apiName.toLowerCase() == "status");

                if (statusField != null) {
                    this.showCertificationStatus = true;
                    this.certificationStatusIndex = statusField.name;
                }

                this.changeDetectorRef.markForCheck();
            }
        );
    }

    getData() {
        this.isLoading = true;
        this.artifactService.getArtifacts(this.artifactType.ID, this.rowsPerPage, this.stateService.artifactTypeFilters.currentPageNumber, this.stateService.artifactTypeFilters.sortField, this.stateService.artifactTypeFilters.sortOrder, this.stateService.artifactTypeFilters.filters, this.stateService.artifactTypeFilters.relationships, this.stateService.artifactTypeFilters.attributes, this.stateService.artifactTypeFilters.simpleTextFilter, this.stateService.artifactTypeFilters.owners).pipe(debounceTime(3000))
            .subscribe(result => {
                    this.items = result.results;
                    this.totalRecords = result.total;
                    if (this.items && this.items.length > 0) this.selected = this.items[0];
                    this.isLoading = false;
                    this.changeDetectorRef.markForCheck();
                },
                error => {
                    this.isLoading = false;
                    this.messagesService.showError("Error", error.message);
                }
            );
    }

    getCertificationStatusColor(status: string) {
        status = status.toLowerCase().trim();

        switch (status) {
            case 'draft':
                return '#BBBBBB';
            case 'certified':
                return '#3f9d40';
            case 'under review':
                return '#e2792a';
            default:
                //custom status, we need to generate a color
                let hash = 0;
                for (let i = 0; i < status.length; i++) {
                    hash = status.charCodeAt(i) + ((hash << 5) - hash);
                    hash = hash & hash;
                }
                return `hsl(${(hash * 2) % 360}, 70%, 70%)`;
        }
    }

    closeEditor() {
        this.showEditor = false;
    }

    add() {
        this.selected = null;
        this.showEditor = true;
    }

    export(listableOnly) {
        this.artifactService.getArtifactsXls(listableOnly, this.artifactType, this.stateService.artifactTypeFilters.sortField, this.stateService.artifactTypeFilters.sortOrder, this.stateService.artifactTypeFilters.filters, this.stateService.artifactTypeFilters.relationships, this.stateService.artifactTypeFilters.attributes, this.stateService.artifactTypeFilters.simpleTextFilter, this.stateService.artifactTypeFilters.owners);
    }

    customExport() {
        //show the custom export screen        
        this.showCustomExport = !this.showCustomExport;
    }

    saveItem(event) {
        this.isEditing = true;
        this.isLoading = true;
        this.showEditor = false;

        let values: any = {};

        //takes the form and convert any array values to , separated string values
        for (var p in event.item) {
            if (event.item.hasOwnProperty(p)) {
                if (Array.isArray(event.item[p])) {
                    values[p] = event.item[p].join();
                } else {
                    values[p] = event.item[p];
                }
            }
        }

        this
            .artifactService
            .saveArtifact(values)
            .subscribe(result => {
                this.isEditing = false;
                this.showMessageForResult(this.messagesService, result);
                if (event.item.ID) this.headerActionsService.emitFavoritesChange(); // favorites need to be reloaded if an object was edited                
                this.isLoading = false;
                this.changeDetectorRef.markForCheck();
            });
    }

    selectArtifact(artifact) {
        this
            .router
            .navigateByUrl(
                SiteUrlHelpers
                    .getObjectUrl(
                        'Artifact',
                        artifact.ObjectID,
                        this.artifactType.ID
                    )
            )
        ;
    }

    private loadArtifactsLazy(event: LazyLoadEvent) {
        //event.first = First row offset
        //event.rows = Number of rows per page
        //event.sortField = Field name to sort with
        //event.sortOrder = Sort order as number, 1 for asc and -1 for dec
        //filters: FilterMetadata object having field as key and filter value, filter matchMode as value        
        this.stateService.artifactTypeFilters.sortOrder = event.sortOrder;
        this.stateService.artifactTypeFilters.sortField = event.sortField == undefined ? "" : event.sortField;
        this.rowsPerPage = event.rows;
        this.stateService.artifactTypeFilters.currentPageNumber = event.first / event.rows;
        this.getData();
    }

    private doSimpleSearch(dt: DataTable, isLoading: boolean) {

        if (isLoading) {
            return;
        }

        isLoading = true;
        if (dt) {
            dt.reset();
        }
    }

    protected onRightClick(event, rightMenu, artifact, grid) {
        var gridRect = grid.el.nativeElement.getBoundingClientRect();
        var itemRect = event.srcElement.getBoundingClientRect();

        this.isMenuOpen = true;

        rightMenu.style.top = (event.screenY - gridRect.top) + 'px';
        rightMenu.style.left = (event.offsetX) + 'px'; //correct

        this.itemUrl = SiteUrlHelpers.getObjectUrl('Artifact', artifact.ObjectID, this.artifactType.ID);

        return false;
    }

    clickedOutside() {
        if (this.isMenuOpen) {
            this.isMenuOpen = false;
        }
    }

    private toggleArtifactDetail() {
        this.showArtifactDetails = !this.showArtifactDetails;
    }

    protected doShowDelete() {
        this
            .objectDetailService
            .getObject(this.selected.ObjectID, 'Artifact')
            .then(r => {
                    this.selected.DisplayValue = r.DisplayValue;
                    this.showDelete = true;
                    this.changeDetectorRef.markForCheck();
                }
            )
        ;
    }
}
