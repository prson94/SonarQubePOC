import { OnInit, ViewChild } from '@angular/core';
import { Input, Component, OnChanges, SimpleChange, OnDestroy, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { forEach } from 'lodash';
import { Table } from 'primeng/table';
import { forkJoin, Observable, of, ReplaySubject, Subscription } from 'rxjs';
import { V2ApiFilters } from '../../../models/asset-search.model';
import { FieldType, FieldTypeAPIModelField } from '../../../models/fieldtype-api.model';
import { GridColumn, GridField } from '../../../models/grid-definition.model';
import { RelationshipCount, RelationshipType } from '../../../models/relationship.model';
import { AssetService } from '../../../services/asset.service';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { GridDefinitionService } from '../../../services/grid-definition.service';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { RelationshipsService } from '../../../services/relationships.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { AdvancedFilteringComponent } from '../../assets-grid/advanced-filtering/advanced-filtering.component';
import { AdvancedFilterFieldType, Filters, LookupValuesAPIModel, LookupValuesAPIParameters } from '../../assets-grid/advanced-filtering/advanced-filtering.models';
import { BaseComponent } from '../base.component';

@Component({
    selector: 'gov-relationship-grid',
    templateUrl: './relationship-grid.component.html',
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['relationship-grid.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [RelationshipsService]
})


export class RelationshipGridComponent extends BaseComponent implements OnChanges, OnDestroy, OnInit {
    @Input() assetUid: string = "";
    @Input() assetTypeUid: string = "";

    relationshipTypes: RelationshipType[] = [];
    relationshipCounts: RelationshipCount[] = [];
    relationships: any[] = [];
    relationshipTypesResolvedNames: any[] = [];

    selectedRelationship: any;
    selectedRelAsset: any;

    advFilterIdentifier: string = '';

    isLoading: boolean = false;
    areTypesLoaded: boolean = false;

    sidePanelOpen: string = '';
    sidePanelTab: string = 'filters';
    sidePanelStorageKey: string = '';

    loadTypesSub: Subscription;
    loadRelationshipsSub: Subscription;
    totalRecords: number = 0;

    simpleFilter: string = "";
    advancedFilter: string = "";
    advancedFilterData: any;
    sortField: string = "relationshiptypename";
    sortOrder: string = "asc";

    filterFields$: Observable<AdvancedFilterFieldType[]>;
    private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);
    readonly menuKey = '~menu';


    fields: GridField[] = [];
    columns: GridColumn[] = [];

    loadedFilterFields: FieldTypeAPIModelField[] = [];

    public getRelationshipTypes(params: LookupValuesAPIParameters): Observable<LookupValuesAPIModel> {
        let data: LookupValuesAPIModel = new LookupValuesAPIModel();
        data.count = this.relationshipTypesResolvedNames.length;
        data.items = this.relationshipTypesResolvedNames.map((item) => item["name"]);
        return of(data);
    }
    filterFieldList: AdvancedFilterFieldType[] = [
        {
            Name: 'relationshiptype',
            FriendlyName: 'Relationship Type',
            Type: new FieldType("Lookup"),
            Category: "",
            ValueLoader: this.getRelationshipTypes.bind(this),
            RemovePopulatedOperator: true

        },
        {
            Name: 'assetpath',
            FriendlyName: 'Asset',
            Type: new FieldType("Text"),
            Category: ""
        }
    ]

    @ViewChild('dt', { static: false }) dt: Table;
    @ViewChild('advFilterComponent', { static: false }) advFilterComponent: AdvancedFilteringComponent;

    hrefSub: Subscription;
    selectedAsset: any;
    selectedReferenceItem: any;
    selectedTag: any;

    constructor(
        private cdRef: ChangeDetectorRef,
        private relationshipService: RelationshipsService,
        private fieldService: FieldsObservableService,
        protected settingsService: CompanySettingsService,
        private gridDefinitionService: GridDefinitionService,
        private linkClickInterceptor: LinkClickInterceptor,
    ) {
        super(settingsService);
        this.sidePanelStorageKey = "relationship-detail";

        this.filterFieldList.filter((x) => x.Name === 'relationshiptype').forEach((x) => x.Type.Lookup.IsPrimaryFilter = true);

        this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
            this.selectedRelAsset = this.selectedRelationship = null;
            this.linkClickInterceptor.handleEvent(this, ev);
        });
    }


    //advanced filters component may change when only one relationship type is filtered
    //we must check filter states on advanced filter updates to show correct fields
    private loadedFiltersHash: string = '';
    updateAdvancedFilters() {
        if (this.loadedFiltersHash !== this.advancedFiltersHash) {
            this.loadedFiltersHash = this.advancedFiltersHash;
            this.filterFieldsSubject.next(this.getAdvancedFilterFields);
            this.filterFieldsSubject.complete();
            if (this.advFilterComponent) {
                this.advFilterComponent.initializeData(true);
            }
        }
    }

    get getAdvancedFilterFields(): AdvancedFilterFieldType[] {
        let filters: AdvancedFilterFieldType[] = [];
        this.filterFieldList.forEach((f) => filters.push(f));
        this.loadedFilterFields.forEach((f) => filters.push(f));
        return filters;
    }

    get advancedFiltersHash(): string {
        return JSON.stringify(this.getAdvancedFilterFields.map((f) => f.Name));
    }

    ngOnInit() {
        this.filterFields$ = this.filterFieldsSubject.asObservable();
        this.updateAdvancedFilters();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p === 'assetUid' && this.assetUid) {
                this.advFilterIdentifier = "Relationships_" + this.assetUid;
                this.initialLoad();
            }
        }

    }

    ngOnDestroy() {
        if (this.loadTypesSub) {
            this.loadTypesSub.unsubscribe();
        }
        if (this.loadRelationshipsSub) {
            this.loadRelationshipsSub.unsubscribe();
        }
    }

    public initialLoad(): void {
        if (this.loadTypesSub) {
            this.loadTypesSub.unsubscribe();
        }

        this.loadTypesSub = forkJoin(
            this.relationshipService.getRelationshipsByAssetTypeUid(this.assetTypeUid),
            this.relationshipService.getRelationshipsCountsForAsset(this.assetUid))
            .subscribe((data) => {
                this.relationshipTypes = data[0];
                this.relationshipCounts = data[1];

                this.relationshipTypesResolvedNames = [];
                this.relationshipCounts.forEach((rc) => {
                    var type = this.relationshipTypes.filter((type) => type.Uid.toLocaleLowerCase() === rc.IntersectTypeUid.toLocaleLowerCase());
                    if (type.length > 0) {
                        let name: string = "";
                        if (rc.IsSubject) {
                            name = type[0].Predicate.Name + " " + type[0].Object.Name;
                        }
                        else {
                            name = type[0].Predicate.Inverse + " " + type[0].Subject.Name;
                        }
                        this.relationshipTypesResolvedNames.push({ uid: rc.IntersectTypeUid, name: name, count: rc.Count, isSelected: false });
                    }
                });
                this.relationshipTypesResolvedNames.sort((a, b) => a["name"].localeCompare(b["name"]));
                this.areTypesLoaded = true;
                this.cdRef.detectChanges();
            });
    }

    loadRelationshipLazy($event) {
        if (!this.assetUid) {
            return;
        }
        this.isLoading = true;


        if (this.loadRelationshipsSub) {
            this.loadRelationshipsSub.unsubscribe();
        }

        if ($event) {
            this.sortField = $event["sortField"] ?? this.sortField;
            this.sortOrder = +$event["sortOrder"] === 1 ? "asc" : "desc";
        }
        if (this.singleSelectedRelationship) {
            this.loadRelationshipsSub =
                forkJoin(
                    this.relationshipService.getRelationshipsForAsset(this.assetUid, this.getParams()),
                    this.gridDefinitionService.getGridDefinition(this.singleSelectedRelationship.uid, "IntersectType"))
                    .subscribe((result) => {
                        this.processGetRelationshipResponse(result[0], result[1]);
                    });
        }
        else {
            this.loadRelationshipsSub =
                this.relationshipService.getRelationshipsForAsset(this.assetUid, this.getParams())
                    .subscribe((result) => {
                        this.processGetRelationshipResponse(result);
                    });
        }
    }

    processGetRelationshipResponse(relationships: any, gridData: any = null) {
        this.columns = [];
        this.fields = [];

        if (gridData) {
            //Asset path is inlcuded in grid by default, to avoid duplication we need to filter it out
            this.columns = gridData.Columns.filter((col) => col.datafield !== "Name");
            this.fields = gridData.Fields;
        }

        this.totalRecords = +relationships["total"];

        if (this.totalRecords > 0) {
            this.relationships = relationships["items"];

            this.relationships.forEach((i) => {
                i[this.menuKey] = [
                    { title: 'Edit Relationship' },
                    { title: 'Delete Relationship' },
                ];
            });
        }
        else {
            this.relationships = [];
        }

        this.isLoading = false;
        this.cdRef.detectChanges();
    }

    getParams(): V2ApiFilters {
        var params = new V2ApiFilters();
        params._pageSize = this.rowsPerPage;
        if (this.dt) {
            params._pageNum = (this.dt.first / this.dt.rows) + 1;
        }
        else {
            params._pageNum = 1;
        }
        params._order = this.sortField;
        params._direction = this.sortOrder;
        params["includeLegacyData"] = true;
        if (this.advancedFilter) {
            params._filter = this.advancedFilter;
        }

        if (this.simpleFilter) {
            params._simpleFilter = this.simpleFilter;
        }

        if (this.singleSelectedRelationship) {
            params["RelationshipTypeUid"] = this.singleSelectedRelationship.uid;
        }

        return params;
    }

    get singleSelectedRelationship(): any {
        if (!this.advancedFilterData) {
            return null;
        }
        var relFilter = this.advancedFilterData.filter(x => x.field === "relationshiptype");
        if (relFilter && relFilter.length !== 0 && relFilter[0]["value"] && relFilter[0]["value"].length === 1) {
            var value = relFilter[0]["value"][0]["value"];
            var selected = this.relationshipTypesResolvedNames.filter((x) => x["name"].toLowerCase() === value.toLowerCase());
            return selected[0];
        }
        return null;
    }

    selectRow(row: any) {
        this.selectedRelationship = row;
        this.selectedAsset = this.selectedReferenceItem = this.selectedTag = null;
        var isSubject = this.selectedRelationship.Subject.Uid.toLowerCase() === this.assetUid.toLowerCase();
        if (isSubject) {
            this.selectedRelAsset = {
                uid: this.selectedRelationship.Object.Uid,
                type: this.selectedRelationship.Object.Type,
                relUid: this.selectedRelationship.Uid
            };
        }
        else {
            this.selectedRelAsset = {
                uid: this.selectedRelationship.Subject.Uid,
                type: this.selectedRelationship.Subject.Type,
                relUid: this.selectedRelationship.Uid
            };
        }
    }

    clickMenuItem(event: any, item: any) {
        let key = event.value.toLowerCase();

        if (key === 'edit') {
            console.log("edit");
        } else if (key === 'delete') {
            console.log("delete");
        }
    }

    onSimpleSearch($event) {
        this.loadRelationshipLazy(null);
    }

    advancedFiltersChanged($event: Filters) {
        this.advancedFilter = $event.filter;
        this.advancedFilterData = $event.data;

        if (this.dt) {
            this.dt.first = 0;
        }
        this.loadRelationshipLazy(null);
        if (this.singleSelectedRelationship) {
            this.fieldService.getFieldsV2(null, null, this.singleSelectedRelationship.uid)
                .subscribe((res) => {
                    this.loadedFilterFields = res;
                    this.updateAdvancedFilters();
                });
        }
        else {
            this.loadedFilterFields = [];
            this.updateAdvancedFilters();
        }
    }
}
