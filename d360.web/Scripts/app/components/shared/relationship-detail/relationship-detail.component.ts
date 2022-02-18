import { OnInit } from '@angular/core';
import { Input, Component, OnChanges, SimpleChange, OnDestroy, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { forkJoin, Observable, ReplaySubject, Subscription } from 'rxjs';
import { FieldType } from '../../../models/fieldtype-api.model';
import { RelationshipType } from '../../../models/relationship.model';
import { RelationshipsService } from '../../../services/relationships.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { AdvancedFilterFieldType, Filters } from '../../assets-grid/advanced-filtering/advanced-filtering.models';
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
    relationships: any[] = [];
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

    filterFields$: Observable<AdvancedFilterFieldType[]>;
    private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);

    filterFieldList: AdvancedFilterFieldType[] = [
        {
            Name: 'path',
            FriendlyName: 'Asset Path',
            Type: new FieldType("Text"),
            Category: ""
        },
        {
            Name: 'assetTypePath',
            FriendlyName: 'Asset Type Path',
            Type: new FieldType("Text"),
            Category: ""
        }
    ]


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
        this.loadTypesSub = this.relationshipService.getRelationshipsByAssetTypeUid(this.assetTypeUid)
            .subscribe((result) => {
                this.relationshipTypes = result;
                this.cdRef.detectChanges();
            });

    }

    loadRelationshipLazy($event) {
        console.log($event);
        this.isLoading = true;
        if (this.loadRelationshipsSub) {
            this.loadRelationshipsSub.unsubscribe();
        }
        var params = {};
        params["_pageSize"] = $event["rows"];
        params["_pageNum"] = (+$event["first"] / +$event["rows"]) + 1;


        params["_order"] = $event["sortField"] ?? "RelationshipTypeName";
        params["_direction"] = +$event["sortOrder"] > 0 ? "asc" : "desc";

        this.loadRelationshipsSub =
            this.relationshipService.getRelationshipsForAsset(this.assetUid, params)
                .subscribe((result) => {
                    this.relationships = result["items"];
                    this.totalRecords = result["total"];
                    this.isLoading = false;
                    this.cdRef.detectChanges();
                });
    }

    onSimpleSearch($event) {
        console.log("simple search");
    }

    advancedFiltersChanged($event: Filters) {
        this.advancedFilter = $event.filter;
        console.log("advanced filters");
    }
}
