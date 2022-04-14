import { Component, OnDestroy } from "@angular/core";
import { AdminBaseComponent } from "../admin-base.component";
import { SearchService } from "../../../services/search.service";
import { SecondaryNavService } from "../../../services/right-sidebar.service";
import { HeaderBreadcrumbService } from "../../../services/header-breadcrumb.service";
import { Title } from "@angular/platform-browser";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { IndexableType, IndexableStatus } from "../../../models/search-admin.model";
import { TreeNode } from "primeng/api";
import { StringConstants } from "../../../static/string-constants";
import { CompanySettingsService } from "../../../services/settings.service";
import { ReuseInterceptor } from '../../../http-interceptors/reuse.interceptor';



@Component({
    selector: "d3s-admin-search-component",
    templateUrl: "./admin-search.component.html"
})

export class AdminSearchComponent extends AdminBaseComponent implements OnDestroy {

    isUpdating: boolean = false;
    updateIn: number = 30;

    indexableHash;
    indexableNodes: TreeNode[];
    readonly JobStatus: string[] = ["None", "Pending", "Processing", "Processing By Asset Type", "Error", "Completed"];
    readonly emptyguid: string = "00000000-0000-0000-0000-000000000000";

    refreshViewLabel = $localize`Refresh View`;

    constructor(
        protected searchService: SearchService,
        private messagesService: MessagesObservableService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title,
        private reuseInterceptor: ReuseInterceptor
    ) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.areaName = StringConstants.Section_Search;
        this.setCommonItems();
    }

    ngOnInit() {
        this.getIndexableTypes();
    }

    getIndexableTypes() {
        this.isLoading = true;
        this.searchService.GetIndexableTypes()
            .subscribe((types) => {
                this.indexableHash = {};
                this.indexableNodes = [];

                types
                    .filter((t) => t.AssetTypeUid === this.emptyguid)
                    .forEach((t) => {
                        let elem: IndexableStatus = this.ConvertTypeToStatus(t);
                        this.indexableHash[this.IndexableStatusId(elem)] = { data: elem, children: [] };

                        this.indexableNodes.push(this.indexableHash[this.IndexableStatusId(elem)]);
                    });

                types
                    .filter((t) => t.AssetTypeUid !== this.emptyguid)
                    .sort((a, b) => 0 - (a.Name > b.Name ? -1 : 1))
                    .forEach((t) => {
                        let elem: IndexableStatus = this.ConvertTypeToStatus(t);
                        this.indexableHash[this.IndexableStatusId(elem)] = { data: elem, children: [] };

                        if (this.indexableHash[`${elem.Class}-${this.emptyguid}`]) {
                            this.indexableHash[`${elem.Class}-${this.emptyguid}`].children.push(this.indexableHash[this.IndexableStatusId(elem)]);
                        }
                    });

                this.UpdateStatus();
                this.isLoading = false;
            });
    }

    private IndexableStatusId(s: IndexableStatus) {
        return `${s.Class}-${s.AssetTypeUid}`;
    }

    private ConvertDate(d): Date {
        let m = /^\/Date\((\d+)\)\/$/.exec(d);
        if (m !== null) {
            return new Date(parseInt(m[1]));
        } else {
            return null;
        }


    }

    private ConvertTypeToStatus(t: IndexableType) {
        let elem: IndexableStatus = new IndexableStatus();
        elem.AssetTypeUid = t.AssetTypeUid;
        elem.Class = t.Class;
        elem.ClassName = t.ClassName;
        elem.Name = t.Name;
        elem.Status = 0;
        return elem;
    }

    UpdateStatus() {
        this.isUpdating = true;
        this.reuseInterceptor.forceRefresh();
        this.searchService.GetIndexbleStatus()
            .subscribe((statuses) => {
                statuses.forEach((s) => {
                    if (this.indexableHash[this.IndexableStatusId(s)]) {
                        let elem: IndexableStatus = this.indexableHash[this.IndexableStatusId(s)].data;
                        elem.TargetCount = s.TargetCount;
                        elem.CurrentCount = s.CurrentCount;
                        elem.Start = this.ConvertDate(s.Start);
                        elem.LastUpdate = this.ConvertDate(s.LastUpdate);
                        elem.Status = s.Status;
                    }
                });
                this.isUpdating = false;
            });
    }

    canRebuild(data: IndexableStatus): boolean {
        return (data.Status < 1 || data.Status > 3);
    }

    rebuild(data: IndexableStatus) {
        this.isUpdating = true;
        this.searchService.SendRebildRequest(data.Class, data.AssetTypeUid)
            .toPromise()
            .then(() => this.UpdateStatus());
    }

    ngOnDestroy() {
        this.clearSidebar();
    }
}