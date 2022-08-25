import { Component, EventEmitter, Output } from "@angular/core";
import { AdminBaseComponent } from "../admin-base.component";
import { SearchService } from "../../../services/search.service";
import { SecondaryNavService } from "../../../services/right-sidebar.service";
import { HeaderBreadcrumbService } from "../../../services/header-breadcrumb.service";
import { Title } from "@angular/platform-browser";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { IndexableType, IndexableStatus } from "../../../models/search-admin.model";
import { TreeNode } from "primeng/api";
import { CompanySettingsService } from "../../../services/settings.service";
import { ReuseInterceptor } from '../../../http-interceptors/reuse.interceptor';
import { TreeTable, TreeTableToggler } from "primeng/treetable";

@Component({
    selector: "d3s-admin-search-component",
    templateUrl: "./admin-search.component.html",
	styleUrls: ['./admin-search.component.less']
})

export class AdminSearchComponent extends AdminBaseComponent {

    isUpdating: boolean = false;
    indexableHash: any;
    indexableNodes: TreeNode[];
	selectedIndexes: TreeNode<IndexableStatus>[] = [];
	
	@Output() onModalClose: EventEmitter<boolean> = new EventEmitter<boolean>();
	
    readonly jobStatuses: { name: string, color: string }[] = [
		{ name: "None", color: "gray" },
		{ name: "Pending", color: "gray" },
		{ name: "Processing", color: "yellow" },
		{ name: "Processing By Asset Type", color: "yellow" },
		{ name: "Error", color: "red" },
		{ name: "Completed", color: "green" }
	];
    readonly emptyUid: string = "00000000-0000-0000-0000-000000000000";
	readonly emptyDate: string = "0001-01-01T00:00:00.000Z";

    refreshViewLabel = $localize`Refresh View`;
	rebuildIndexesLabel = $localize`Rebuild Indexes`;

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
    }

    ngOnInit() {
        this.getIndexableTypes();
    }

    getIndexableTypes() {
        this.isLoading = true;
        this.searchService.getIndexableTypes()
            .subscribe((types) => {
                this.indexableHash = {};
                this.indexableNodes = [];

                types
                    .filter((t) => t.AssetTypeUid === this.emptyUid)
                    .forEach((t) => {
                        let elem: IndexableStatus = this.convertTypeToStatus(t);
                        this.indexableHash[this.getIndexableStatusId(elem)] = { data: elem, children: [] };

                        this.indexableNodes.push(this.indexableHash[this.getIndexableStatusId(elem)]);
                    });

                types
                    .filter((t) => t.AssetTypeUid !== this.emptyUid)
                    .sort((a, b) => 0 - (a.Name > b.Name ? -1 : 1))
                    .forEach((t) => {
                        let elem: IndexableStatus = this.convertTypeToStatus(t);
                        this.indexableHash[this.getIndexableStatusId(elem)] = { data: elem, children: [] };

                        if (this.indexableHash[`${elem.Class}-${this.emptyUid}`]) {
                            this.indexableHash[`${elem.Class}-${this.emptyUid}`].children.push(this.indexableHash[this.getIndexableStatusId(elem)]);
                        }
                    });

                this.updateStatus();
                this.isLoading = false;
            });
    }

    private getIndexableStatusId(s: IndexableStatus): string {
        return `${s.Class}-${s.AssetTypeUid}`;
    }

    private convertDate(iso: string): string {
		if (iso !== this.emptyDate) {
			return iso;
		}
		return null;
    }

    private convertTypeToStatus(t: IndexableType): IndexableStatus {
        let elem: IndexableStatus = new IndexableStatus();
        elem.AssetTypeUid = t.AssetTypeUid;
        elem.Class = t.Class;
        elem.ClassName = t.ClassName;
        elem.Name = t.Name;
        elem.Status = 0;
		elem.Menu = [];
        return elem;
    }

    updateStatus(): void {
        this.isUpdating = true;
        this.reuseInterceptor.forceRefresh();
        this.searchService.getIndexableStatus()
            .subscribe((statuses) => {
                statuses.forEach((s) => {
                    if (this.indexableHash[this.getIndexableStatusId(s)]) {
                        let elem: IndexableStatus = this.indexableHash[this.getIndexableStatusId(s)].data;
                        elem.TargetCount = s.TargetCount;
                        elem.CurrentCount = s.CurrentCount;
                        elem.Start = this.convertDate(s.Start);
                        elem.LastUpdate = this.convertDate(s.LastUpdate);
                        elem.Status = s.Status;
						elem.Menu = this.getKebabMenuItems(this.indexableHash[this.getIndexableStatusId(s)]);
                    }
                });
				this.selectedIndexes = [];
                this.isUpdating = false;
            });
    }

    canRebuild(data: IndexableStatus): boolean {
        return (data.Status < 1 || data.Status > 3);
    }

	rebuild(data: TreeNode<IndexableStatus>[]) {
		data = data.filter((node) => this.canRebuild(node.data));
		data.forEach((node) => {
			if (node.children.length > 0) {
				const removableAssets = node.children.map((node) => node.data.AssetTypeUid);
				data = data.filter((filterNode) => {
					return !removableAssets.includes(filterNode.data.AssetTypeUid);
				});
			}
		});
		this.isUpdating = true;
		this.searchService.sendRebuildRequest(data.map((node) => node.data)).subscribe(() => {
			this.updateStatus();
		});
	}

	onRowClick($event: MouseEvent, rowNode: any, treeTable: TreeTable, rowToggle: TreeTableToggler, kebabMenuDiv): void {
		if (!kebabMenuDiv.contains($event.target)) {
			if (rowNode.node.children) {
				if (rowNode.node.children.length > 0) {
					rowToggle.onClick($event);
				} else if (!this.isUpdating && this.canRebuild(rowNode.node.data)) {
					treeTable.toggleNodeWithCheckbox({ originalEvent: $event, rowNode });
				}
			}
		}
    }

	getKebabMenuItems(node: TreeNode<IndexableStatus>) {
		const menu = [];
		if (node.children) {
			if (node.children.length) {
				menu.push({ title: $localize`Rebuild Category Index`, disabled: !this.canRebuild(node.data) });
			} else {
				menu.push({ title: $localize`Rebuild Asset Type Index`, disabled: !this.canRebuild(node.data) });
			}
		}
		menu.push({ title: $localize`Refresh View` });
		return menu;
	}
	
	onKebabMenuClick(menuItem: any, node: TreeNode<IndexableStatus>): void {
		let key = menuItem.value.toLowerCase();
		if (key === $localize`Refresh View`.toLowerCase()) {
			this.updateStatus();
		} else {
			this.rebuild([node]);
		}
    }
}