import { OnInit, ViewChild } from '@angular/core';
import { Input, Component, OnChanges, SimpleChange, OnDestroy, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Table } from 'primeng/table';
import { forkJoin, Observable, of, ReplaySubject, Subscription } from 'rxjs';
import { V2ApiFilters } from '../../../models/asset-search.model';
import { FieldType } from '../../../models/fieldtype-api.model';
import { RelationshipCount, RelationshipType } from '../../../models/relationship.model';
import { RelationshipsService } from '../../../services/relationships.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { AdvancedFilterFieldType, Filters, LookupValuesAPIModel, LookupValuesAPIParameters } from '../../assets-grid/advanced-filtering/advanced-filtering.models';
import { BaseComponent } from '../base.component';

@Component({
    selector: 'gov-relationship-detail',
    templateUrl: './relationship-detail.component.html',
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['relationship-detail.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [RelationshipsService]
})


export class RelationshipDetailComponent extends BaseComponent implements OnChanges, OnDestroy, OnInit {
    @Input() assetUid: string = "";
    @Input() assetTypeUid: string = "";

    relationshipTypes: RelationshipType[] = [];
    relationshipCounts: RelationshipCount[] = [];
    relationships: any[] = [];
    relationshipTypesResolvedNames: any[] = [];

    selectedRelationship: any;

    isLoading: boolean = false;

    sidePanelOpen: string = '';
    sidePanelTab: string = '';
    sidePanelStorageKey: string = '';

    loadTypesSub: Subscription;
    loadRelationshipsSub: Subscription;
    totalRecords: number = 0;

    simpleFilter: string = "";
    advancedFilter: string = "";
    sortField: string = "relationshiptypename";
    sortOrder: string = "asc";

    filterFields$: Observable<AdvancedFilterFieldType[]>;
    private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);
    readonly menuKey = '~menu';

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

    constructor(
        private cdRef: ChangeDetectorRef,
        private relationshipService: RelationshipsService,
        protected settingsService: CompanySettingsService,
    ) {
        super(settingsService);
        this.sidePanelStorageKey = "relationship-detail";
    }
    ngOnInit() {
        this.filterFields$ = this.filterFieldsSubject.asObservable();
        this.filterFieldsSubject.next(this.filterFieldList);
        this.filterFieldsSubject.complete();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p === 'assetUid' && this.assetUid) {
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
                        this.relationshipTypesResolvedNames.push({ uid: rc.IntersectTypeUid, name: name });
                    }
                });
                this.relationshipTypesResolvedNames.sort((a, b) => a["name"].localeCompare(b["name"]));
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

        this.loadRelationshipsSub =
            this.relationshipService.getRelationshipsForAsset(this.assetUid, this.getParams())
                .subscribe((result) => {
                    this.relationships = result["items"];

                    this.relationships.forEach((i) => {

                        i[this.menuKey] = [
                            { title: 'Edit Relationship' },
                            { title: 'Delete Relationship' },
                        ];
                    });

                    this.totalRecords = result["total"];
                    this.isLoading = false;
                    this.cdRef.detectChanges();
                });
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

        if (this.advancedFilter) {
            params._filter = this.advancedFilter;
        }
        return params;
    }
    selectRow(row: any) {
        this.selectedRelationship = row;
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
        console.log("simple search");
    }

    advancedFiltersChanged($event: Filters) {
        this.advancedFilter = $event.filter;
        if (this.dt) {
            this.dt.first = 0;
        }
        this.loadRelationshipLazy(null);
    }
}
