import { EventEmitter, OnInit, Output, ViewChild } from '@angular/core';
import { Input, Component, OnChanges, SimpleChange, OnDestroy, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Table } from 'primeng/table';
import { forkJoin, Observable, of, ReplaySubject, Subject, Subscription } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { Param } from '../../../enums/param.enum';
import { V2ApiFilters } from '../../../models/asset-search.model';
import { FieldType, FieldTypeAPIModelField } from '../../../models/fieldtype-api.model';
import { GridColumn, GridField } from '../../../models/grid-definition.model';
import { RelationshipCount, RelationshipType } from '../../../models/relationship.model';
import { AssetService } from '../../../services/asset.service';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { GridDefinitionService } from '../../../services/grid-definition.service';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { NumberOfRowsByCategoryService } from '../../../services/number-of-rows-by-category.service';
import { PermissionsService, Permissions } from '../../../services/permissions.service';
import { RelationshipsService } from '../../../services/relationships.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { AdvancedFilteringComponent } from '../../assets-grid/advanced-filtering/advanced-filtering.component';
import { AdvancedFilterFieldType, Filters, LookupValuesAPIModel, LookupValuesAPIParameters } from '../../assets-grid/advanced-filtering/advanced-filtering.models';
import { BaseComponent } from '../base.component';
import { AddRelationshipComponent } from './add-relationship.component';

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
    @Input() isInModal: boolean = false;
    @Output() onClose = new EventEmitter();

    assetPermissions: Permissions;

    relationshipTypes: RelationshipType[] = [];
    relationshipCounts: RelationshipCount[] = [];
    relationships: any[] = [];
    relationshipTypesResolvedNames: any[] = [];
    assetDetail: any = {};

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
    sortField: string = "";
    sortOrder: string = "";

    filterFields$: Observable<AdvancedFilterFieldType[]>;
    private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);
    readonly menuKey = '~menu';


    fields: GridField[] = [];
    columns: GridColumn[] = [];

    loadedFilterFields: FieldTypeAPIModelField[] = [];

    showEditor: boolean = false;
    isAddVisible: boolean = false;
    showDelete: boolean = false;
    deleteInProgress: boolean = false;
    isExportInProgress: boolean = false;

    loadPageNumberAfterDeletion: number = -1;
    selectIndexAfterDeletion: number = -1;
    @ViewChild('addRelationships', { static: false }) addRelationshipComponent: AddRelationshipComponent

    public getRelationshipTypes(params: LookupValuesAPIParameters): Observable<LookupValuesAPIModel> {
        let data: LookupValuesAPIModel = new LookupValuesAPIModel();
        data.count = this.relationshipTypesResolvedNames.length;
        data.items = [];
        this.relationshipTypesResolvedNames.forEach((item) => {
            if (params.filter) {
                if (item["name"].toLowerCase().indexOf(params.filter.toLowerCase()) !== -1) {
                    data.items.push({ value: item["uid"], name: item["name"], count: item.count });
                }
            }
            else {
                data.items.push({ value: item["uid"], name: item["name"], count: item.count });
            }
        });
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

    public rowsPerPage: number;
    public title: string = 'Relationships Grid'
    private destroy = new Subject<void>();

    constructor(
        private cdRef: ChangeDetectorRef,
        private numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
        private relationshipService: RelationshipsService,
        private assetService: AssetService,
        private fieldService: FieldsObservableService,
        protected settingsService: CompanySettingsService,
        private gridDefinitionService: GridDefinitionService,
        private linkClickInterceptor: LinkClickInterceptor,
        private messagesService: MessagesObservableService,
        private permissionService: PermissionsService
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
        if (this.loadedFilterFields) {
            this.loadedFilterFields.forEach((f) => filters.push(f));
        }
        return filters;
    }

    get advancedFiltersHash(): string {
        return JSON.stringify(this.getAdvancedFilterFields.map((f) => f.Name));
    }

    ngOnInit() {
        this.setRowsPerPage();
        this.numberOfRowsByCategoryService.defineNumberOfRows(this.defaultInitialItemsPerPage);

        this.filterFields$ = this.filterFieldsSubject.asObservable();
        this.updateAdvancedFilters();
    }

    setRowsPerPage(): void {
        this.numberOfRowsByCategoryService.rowsPerPage.pipe(
            takeUntil(this.destroy)
        ).subscribe((rowsPerPage) => {
            this.rowsPerPage = rowsPerPage[this.title] || this.defaultInitialItemsPerPage;
        });
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
        this.destroy.next();
        this.destroy.complete();
    }

    public initialLoad(): void {
        if (this.loadTypesSub) {
            this.loadTypesSub.unsubscribe();
        }

        this.assetService.getUIDetailsForAssetUID(this.assetUid)
            .subscribe((ad) => {
                this.assetDetail = ad;
                var permissionObs: Observable<Permissions> = this.permissionService.getAssetPermissions(this.assetUid);

                if (ad.Object === 'Resource' || ad.Object === 'ReferenceItemType') {
                    var p = new Permissions();
                    p.AddRelationships = p.EditRelationships = p.DeleteRelationships = p.ReadRelationships = true;
                    permissionObs = of(p);
                }

                if (ad.Object === 'ReferenceItemType') {
                    this.assetTypeUid = '0000000a-0000-0000-0000-000000000009';
                }

                this.loadTypesSub = forkJoin(
                    this.relationshipService.getRelationshipsByAssetTypeUid(this.assetTypeUid),
                    this.relationshipService.getRelationshipsCountsForAsset(this.assetUid),
                    permissionObs
                )
                    .subscribe((data) => {
                        this.relationshipTypes = data[0];
                        this.relationshipCounts = data[1];
                        this.assetPermissions = data[2];

                        this.processCountData();

                        this.areTypesLoaded = true;
                        this.cdRef.detectChanges();
                    });
            });


    }

    updateCountData() {
        this.relationshipService.getRelationshipsCountsForAsset(this.assetUid)
            .subscribe((data) => {
                this.relationshipCounts = data;
                this.processCountData();
                if (this.addRelationshipComponent) {
                    //trigger count update in child
                    this.addRelationshipComponent.initialLoad();
                }
                this.cdRef.detectChanges();
            });
    }

    processCountData() {
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
                this.relationshipTypesResolvedNames.push(
                    {
                        uid: rc.IntersectTypeUid,
                        name,
                        count: rc.Count,
                        isSelected: false
                    });
            }
        });
        this.relationshipTypesResolvedNames.sort((a, b) => a["name"].localeCompare(b["name"]));
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

            this.relationships.forEach((i, index) => {
                i["index"] = index;
                i[this.menuKey] = [];

                var type = this.relationshipTypes.filter((rt) => rt.Uid.toLowerCase() === i.RelationshipTypeUid.toLowerCase());

                if (type.length > 0) {
                    i["isHierarchy"] = type[0].Predicate.Type === "InterTypeHierarchy" || type[0].Predicate.Type === "IntraTypeHierarchy";

                    if ((this.assetPermissions.EditRelationships || this.assetPermissions.AddRelationships) && type[0].HasFieldTypes) {
                        i[this.menuKey].push({ title: 'Edit Relationship' });
                    }
                }

                if (this.assetPermissions.DeleteRelationships) {
                    i[this.menuKey].push({ title: 'Delete Relationship' });
                }
            });
        }
        else {
            this.relationships = [];
        }

        if (this.relationships.length > 0) {
            if (this.selectIndexAfterDeletion !== -1) {
                this.selectRow(this.relationships[this.selectIndexAfterDeletion]);
                this.selectIndexAfterDeletion = -1;
            }
            else {
                this.selectRow(this.relationships[0]);
            }
        }
        else {
            this.selectedRelAsset = this.selectedRelationship = this.selectedAsset = this.selectedReferenceItem = this.selectedTag = null;
        }

        this.isLoading = false;
        this.cdRef.detectChanges();
    }

    getParams(): V2ApiFilters {
        var params = new V2ApiFilters();
        params._pageSize = this.rowsPerPage ?? 10;
        if (this.dt) {
            params._pageNum = (this.dt.first / this.dt.rows) + 1;
            params._pageSize = this.dt.rows;
        }
        else {
            params._pageNum = 1;
        }

        if (this.loadPageNumberAfterDeletion !== -1) {
            params._pageNum = this.loadPageNumberAfterDeletion;
            this.loadPageNumberAfterDeletion = -1;
        }

        if (this.sortField) {
            params._order = this.sortField;
        }
        if (this.sortOrder) {
            params._direction = this.sortOrder;
        }
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
        if (this.relationshipCounts.length === 1) {
            var uid = this.relationshipCounts[0].IntersectTypeUid;
            return this.relationshipTypesResolvedNames.filter((x) => x["uid"].toLowerCase() === uid.toLowerCase())[0];
        }

        if (!this.advancedFilterData) {
            return null;
        }

        var relFilter = this.advancedFilterData.filter((x) => x.field === "relationshiptype");
        if (relFilter && relFilter.length !== 0 && relFilter[0]["value"] && relFilter[0]["value"].length === 1) {
            var value = relFilter[0]["value"][0]["value"];
            return this.relationshipTypesResolvedNames.filter((x) => x["uid"].toLowerCase() === value.toLowerCase())[0];
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
                relUid: this.selectedRelationship.Uid,
                name: this.selectedRelationship.RelationshipTypeName,
                target: this.selectedRelationship.Object["[Path]"]
            };
        }
        else {
            this.selectedRelAsset = {
                uid: this.selectedRelationship.Subject.Uid,
                type: this.selectedRelationship.Subject.Type,
                relUid: this.selectedRelationship.Uid,
                name: this.selectedRelationship.RelationshipTypeName,
                target: this.selectedRelationship.Subject["[Path]"]
            };
        }
    }

    clickMenuItem(event: any, item: any) {
        let key = event.value.toLowerCase();

        if (key === 'edit relationship') {
            this.showEditor = true;
        } else if (key === 'delete relationship') {
            this.showDelete = true;
        }
    }

    onSimpleSearch($event) {
        this.loadRelationshipLazy(null);
    }

    advancedFiltersChanged($event: Filters) {
        this.advancedFilter = $event.filter;
        this.advancedFilterData = $event.data;

        var typeFilters = this.advancedFilterData.filter((x) => x.field === 'relationshiptype') as any[];
        if (typeFilters.length > 0) {
            this.relationshipTypesResolvedNames.forEach((item) => item.isSelected = false);
            typeFilters.forEach((f) => {
                if (f.value) {
                    f.value.forEach((item) => {
                        var names = this.relationshipTypesResolvedNames.filter((x) => x.uid.toLowerCase() === item.value.toLowerCase());
                        names.forEach((sel) => sel.isSelected = true);
                    });
                }
            });
            this.relationshipTypesResolvedNames = JSON.parse(JSON.stringify(this.relationshipTypesResolvedNames));
        }

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

    saveItem($event) {
        this.showEditor = false;
        this.loadRelationshipLazy(null);
        this.updateCountData();
    }

    onAddComplete($event) {
        this.isAddVisible = false;
        this.loadRelationshipLazy(null);
        this.updateCountData();
    }


    delete() {
        this.deleteInProgress = true;
        var item = { uid: this.selectedRelationship.Uid };

        //previous item on the list needs to be selected after relationship deletion
        var pageOfDeletedItem = (this.dt.first / this.dt.rows) + 1;
        var indexOfDeletedItem = +this.selectedRelationship["index"];

        if (indexOfDeletedItem !== 0) {
            //if index is not 0 we should load same page and select previous item
            this.loadPageNumberAfterDeletion = pageOfDeletedItem;
            this.selectIndexAfterDeletion = indexOfDeletedItem - 1;
        }

        if (indexOfDeletedItem === 0) {
            //if index is 0 we should load previous page and select last item
            //if page is 1 we should stay on same page and select first item
            this.loadPageNumberAfterDeletion = pageOfDeletedItem - 1;
            if (this.loadPageNumberAfterDeletion === 0) {
                this.loadPageNumberAfterDeletion = 1;
                this.selectIndexAfterDeletion = 0;
            }
            else {
                this.selectIndexAfterDeletion = this.dt.rows - 1;
            }
        }
        this.relationshipService.deleteRelationshipV2(this.selectedRelationship.RelationshipTypeUid, [item])
            .subscribe((res) => {
                let msg = 'Relationship Successfully deleted';
                this.showMessageForApiResult(this.messagesService, res[0], msg);
                this.deleteInProgress = false;
                this.showDelete = false;
                this.loadRelationshipLazy(null);
                this.updateCountData();
            });

    }

    export() {
        this.isExportInProgress = true;
        this.relationshipService
            .getRelationshipsForAssetExcel(
                this.assetUid, this.getParams(),
                'Filtered ' + this.assetDetail.DisplayValue + ' Relationships',
                () => { this.isExportInProgress = false; }
            );
    }

    get fullRelationshipNameAsHTML(): string {
        return `${this.assetDetail.DisplayValue} - <strong>&nbsp;${this.selectedRelAsset.name}&nbsp;</strong> - ${this.selectedRelAsset.target}`;

    }
}
