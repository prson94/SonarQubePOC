import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter, ViewChild, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from "@angular/core";
import { Router } from "@angular/router";
import { GridColumn, GridField } from "../../../models/grid-definition.model";
import { GridDefinitionService } from "../../../services/grid-definition.service";
import { RelationshipsService } from "../../../services/relationships.service";
import { BaseComponent } from "../base.component";
import { SiteUrlHelpers } from "../../../static/site-url-helpers";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { AssetService } from "../../../services/asset.service";
import { LazyLoadEvent } from "primeng/api";
import { Subscription } from "rxjs";
import { debounceTime } from "rxjs/operators";
import { RelationshipTypeUIModel } from "../../../models/relationship.model";


@Component({
    selector: "d3s-dynamic-relationship-grid",
    providers: [GridDefinitionService, RelationshipsService, AssetService],
    templateUrl: "./dynamic-relationship-grid.component.html",
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class DynamicRelationshipGridComponent extends BaseComponent implements OnChanges, OnDestroy {
    @Input() intersectTypeUid: string;
    @Input() assetUid: string;
    @Input() objectUid: string;
    @Input() subjectUid: string;
    @Input() isSubject: boolean;

    @Input() relationshipName: string = '';
    @Input() count: number = 0;

    @Input() addRelationship: boolean;
    @Input() hasEdit: boolean = false;
    @Input() hasDelete: boolean = false;
    @Input() readOnly: boolean = false;

    @Output() readOnlyChange = new EventEmitter();
    @Output() addRelationshipChange = new EventEmitter();
    @Output() relationshipAdded = new EventEmitter();
    @Output() relationshipRemoved = new EventEmitter();
    @Output() deleteOn = new EventEmitter();
    @Output() deleteOff = new EventEmitter();
    @Output() onFilterChange = new EventEmitter();

    @Input() simpleFilter: boolean;

    isSaveDisabled: boolean = false;

    private fields: GridField[] = [];
    private currentParams: any;

    relationshipLoadSub: Subscription;

    get globalFilterFields(): string[] {
        return this.columns.map((c) => c.datafield);
    }

    relations: any[] = [];
    columns: GridColumn[] = [];

    selected: any = null;
    showEditor: boolean = false;
    showDelete: boolean = false;
    showEditPencil: boolean = true;
    isGridLoading: boolean = false;
    isDataLoading: boolean = true;
    theDeleteCallback: Function;

    totalRecords: number = 0;

    @ViewChild("dt", { static: false }) datatable;

    constructor(private router: Router,
        private gridDefinitionService: GridDefinitionService,
        protected relationshipsService: RelationshipsService,
        private messagesService: MessagesObservableService,
        private assetService: AssetService,
        private cdRef: ChangeDetectorRef
    ) {
        super();
        this.theDeleteCallback = this.deleteItem.bind(this);
    }

    ngOnDestroy(): void {
        if (this.relationshipLoadSub) {
            this.relationshipLoadSub.unsubscribe();
        }
        this.closeEditor();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if ((changes["intersectTypeUid"]
            || changes["objectUid"]
            || changes["subjectUid"]
            || changes["isSubject"]
        )

            && (this.intersectTypeUid != null
                && this.objectUid != null
                && this.subjectUid != null)) {
            this.relations = [];
            this.load();
        }
    }

    load() {
        this.getFieldsDefinition();
    }

    getFieldsDefinition() {
        this.isGridLoading = true;

        this.gridDefinitionService.getGridDefinition(this.intersectTypeUid, "IntersectType")
            .subscribe((result) => {
                this.isGridLoading = false;
                this.columns = result.Columns;
                this.fields = result.Fields;
                this.showEditPencil = (result.FieldsCount > 0);
                this.readOnly = result.IsReadOnly;
                this.readOnlyChange.emit(this.readOnly);
                this.cdRef.markForCheck();
            });
    }

    loadRelationshipLazy($event: LazyLoadEvent) {

        var params = {};
        if (this.isSubject) {
            params["subjectUid"] = this.assetUid;
            params["_order"] = "object.[path]";
        }
        else {
            params["objectUid"] = this.assetUid;
            params["_order"] = "subject.[path]";
        }

        if ($event.globalFilter) {
            params["_simpleFilter"] = "*" + $event.globalFilter;
        }

        if ($event.sortField) {
            params["_direction"] = "asc";
            var field = this.columns.filter((f) => f.datafield === $event.sortField);
            if (field && field.length > 0) {
                if (field[0]["apiName"]) {
                    params["_order"] = field[0]["apiName"];

                } else if (field[0].datafield === "Name") {
                    params["_order"] = this.isSubject ? "object.[path]" : "subject.[path]";
                }
            }

            if ($event.sortOrder === -1) {
                params["_direction"] = "desc";
            }
        }


        if ($event.filters) {
            let filters: string[] = [];
            var keys = Object.keys($event.filters);
            keys.forEach((key) => {
                var col = this.columns.filter((f) => f.datafield === key);
                if (col.length > 0) {
                    var fieldApiName = col[0]["apiName"];
                    if (!fieldApiName && col[0].text === "Asset Path") {
                        if (this.isSubject) {
                            fieldApiName = "Object.[Path]";
                        }
                        else {
                            fieldApiName = "Subject.[Path]";
                        }
                    }

                    var value = $event.filters[key].value;

                    let operator = "ct";

                    switch (col[0]["filtertype"]) {
                        case "number":
                            operator = "eq";
                            break;
                        default:
                            value = (value as string).replace(/'/g, "&apos;");
                            value = `'${encodeURIComponent(value)}'`;
                            operator = "ct";
                            break;
                    }
                    filters.push(`(${fieldApiName} ${operator} ${value})`);
                }
            })
            if (filters.length > 0) {
                params["_filter"] = `(${filters.join(" and ")})`;
            }
        }

        params["_includePath"] = true;
        params["_pageSize"] = $event.rows;
        params["_pageNum"] = ($event.first / $event.rows) + 1;
        params["_listcolorsasjson"] = true;

        this.currentParams = JSON.parse(JSON.stringify(params));

        if (this.relationshipLoadSub) {
            this.relationshipLoadSub.unsubscribe();
        }

        this.isDataLoading = true;

        this.relationshipLoadSub = this.relationshipsService
            .getRelationships(this.intersectTypeUid, params)
            .pipe(debounceTime(200))
            .subscribe((res) => {
                this.totalRecords = +res["total"];
                this.relations = [...[]];
                if (this.totalRecords > 0) {
                    this.relations = res["items"] as any[];
                    this.relations.forEach((rel) => {
                        rel["Name"] = this.isSubject ? rel.Object["[Path]"] : rel.Subject["[Path]"];
                    });
                }
                this.isDataLoading = false;
                this.cdRef.markForCheck();
            },
                (err) => {
                    this.totalRecords = 0;
                    this.relations = [];
                    this.isDataLoading = false;
                    this.cdRef.markForCheck();
                }
            );
    }

    private shouldShowEditor(): boolean {
        return (this.addRelationship || this.showEditor);
    }

    public export() {
        if (this.datatable)
            this.datatable.exportCSV();
    }

    closeEditor() {
        this.showEditor = false;
        if (this.addRelationship) {
            this.addRelationship = !this.addRelationship;
            this.addRelationshipChange.emit(this.addRelationship);
        }
    }

    saveRelationship(event) {
        this.isSaveDisabled = true;
        let model: any[] = [];
        let fields: any = {};
        for (var prop in event.item) {
            if (prop != "IntersectTypeID" && prop != "Source" && prop != "SourceID" && prop != "Items" && prop != "ID" && prop != "Uid") {
                fields[prop] = event.item[prop];
            }
        }

        if (event.action == "new") {
            const assets = event.item.Items.split(',');
            assets.forEach(a => {
                let newRel: any = {};
                if (this.isSubject)
                    newRel = { SubjectAssetUid: this.assetUid, ObjectAssetUid: a, Fields: fields };
                else
                    newRel = { ObjectAssetUid: this.assetUid, SubjectAssetUid: a, Fields: fields };

                model.push(newRel);
            });
        }
        else {
            let newRel: any = {};
            newRel = { SubjectAssetUid: this.selected.Subject.Uid, ObjectAssetUid: this.selected.Object.Uid, Fields: fields };
            model.push(newRel);
        }

        this.relationshipsService.saveRelationships(this.intersectTypeUid, model)
            .subscribe(res => {

                if (event.action == "new") {
                    this.showMessageForApiResults(this.messagesService, res, " Relationships succesfully added!");
                    this.relationshipAdded.emit({ uid: this.intersectTypeUid, isSubject: this.isSubject, data: model });
                }
                else {
                    this.showMessageForApiResults(this.messagesService, res, " Relationships succesfully updated!");
                }

                if (!res.some(x => x.Success != true)) {
                    this.closeEditor();
                }

                this.isSaveDisabled = false;
                this.cdRef.markForCheck();
            });
    }

    deleteItem(item) {
        let model: any[] = [];
        let deleteItem: any = {};

        deleteItem["Cascade"] = true;
        deleteItem["uid"] = item;
        model.push(deleteItem);

        this.relationshipsService.deleteRelationshipV2(this.intersectTypeUid, model)
            .subscribe(res => {

                this.showMessageForApiResults(this.messagesService, res, " Relationship succesfully deleted!");
                if (!res.some((x) => x.Success != true)) {
                    this.relations = this.relations.filter((x) => x.Uid != item);
                    this.relationshipRemoved.emit({ uid: this.intersectTypeUid, isSubject: this.isSubject });
                }
                this.showDelete = false;
                this.deleteOff.emit();
            });

    }

    doDelete() {
        this.deleteOn.emit();
        this.showDelete = true;
    }

    cancelDelete() {
        this.deleteOff.emit();
        this.showDelete = false;
    }

    selectObject(item) {
        let isAsset: boolean = true;
        var uid = this.isSubject ? item.Object.Uid : item.Subject.Uid;
        if (!uid) {
            isAsset = false;
            uid = this.isSubject ? item.Object.AssetTypeUid : item.Subject.AssetTypeUid;
        }
        if (item.Object != "Task") {
            var url = isAsset ? ("asset/" + uid) : ("assettype/" + uid);
            this.router.navigateByUrl(url);
        }
        else {
            this.assetService.getProcessDiagramUrl(uid)
                .subscribe((res) => {
                    this.router.navigateByUrl(res);
                })

        }
    }

    onFilter(event: any) {

        let count = 0;
        let qstring: string = "";

        for (var key in event.filters) {
            var matchcondition: string = event.filters[key].matchMode == "startsWith" ? "STARTS_WITH" : event.filters[key].matchMode;
            qstring += `&filterdatafield${count}=${key}&filtercondition${count}=${matchcondition}&filtervalue${count}=${event.filters[key].value}`;
            count++;
        }
        qstring += "&filterscount=" + count;
        this.onFilterChange.emit(qstring);
    }

    getAssetUid(item: RelationshipTypeUIModel) {
        return this.isSubject ? item.Object.Uid : item.Subject.Uid;
    }
}
