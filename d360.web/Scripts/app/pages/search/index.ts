import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { forkJoin, Subscription } from "rxjs";
import { ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import {
    SearchAggregation,
    SearchQuery,
    SearchResults,
    SearchSelection
} from '../../models/search-result.model';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { DataProfileService } from '../../services/dataprofile.service';
import { SidePanelButton } from "../../models/side-panel.model";
import { CompanySettingsService } from '../../services/settings.service';
import { CompanySettingEnum } from '../../models/settings.model';
import { SemanticType } from '../../models/semantic-type.model';
import { SidePanelService } from '../../services/side-panel.service';
import { IOutputData } from 'angular-split';
import { LinkClickInterceptor } from '../../services/href-click-service';
import { FeatureFlagsInitService } from '../../services/feature-flags-init.service';
import { BaseComponent } from '../../components/shared/base.component';
import { LoadingComponent } from '../../_shared/components/loading';
import { ResultItem } from './components/result';
import { AngularSplitModule } from 'angular-split';
import { CoreModule } from '../../components/shared/core.module';
import { SemanticsModule } from '../../components/semantic/semantics.module';
import { AssetDetailModule } from '../../components/shared/asset-detail/asset-detail.module';
import { AssetEditorModule } from '../../components/shared/asset-editor/asset-editor.module';
import { CheckTreeModule } from '../../components/shared/small-widgets/check-tree/check-tree.module';
import { DataProfileModule } from '../../components/shared/dataprofile/dataprofile.module';
import { AssetTypeDetailModule } from '../../components/shared/asset-type-detail/asset-type-detail.module';
import { TaggedAssetDetailModule } from '../../components/shared/tagged-assets/tagged-assets-detail.module';
import { SiteModalModule } from '../../components/shared/modal/gov-modal.module';
import { PopupMenuItem } from '../../components/shared/controls/popup-menu/popup-menu.component';
import { CheckTree } from '../../components/shared/small-widgets/check-tree/check-tree.component';
import { CheckTreeNode } from '../../components/shared/small-widgets/check-tree/checktreenode';
import { TooltipModule } from 'primeng/tooltip';
import { SidePanelModule } from '../../components/shared/sidepanel/side-panel.module';
import { SearchService } from '../../services/search.service';
import { TypeaheadSearch } from '../../_shared/components/typeahead-search';
import { Paginator } from '../../_shared/components/paginator';

@Component({
    selector: 'd3s-search',
    templateUrl: './index.html',
	styleUrls: ["index.less"],
	standalone: true,
	imports: [
		AngularSplitModule,
		AssetDetailModule,
		AssetEditorModule,
		AssetTypeDetailModule,
		CheckTreeModule,
		CoreModule,
		DataProfileModule,
		LoadingComponent,
		Paginator,
		ResultItem,
		SemanticsModule,
		SidePanelModule,
		SiteModalModule,
		TaggedAssetDetailModule,
		TooltipModule,
		TypeaheadSearch
	]
})
export class SearchIndex extends BaseComponent implements OnInit, OnDestroy {
    public searchResults: SearchResults;
	public categories: CheckTreeNode[] = [];
    public searchText: string;
	public searchTypes: string[] = [];
	public selectedFilters: CheckTreeNode[] = [];

	public treeLoading: boolean = false;

	public pageNumber: number = 1;
	public resultCount: number = 0;
	public resultsPerPage: number = 10;

    public fromNumber: number = 0;
    public sub: any;
    public PageNumberSub: any;
    public selection: SearchSelection;

    public sidePanelOpen: boolean = true;
    public sidePanelLoading: boolean = false;
    public sidePanelTab: string;
    public sidePanelStorageKey: string = "searchresults";
    public hasProfiling: boolean = false;
    public dataProfile: any;
    public advancedFiltersLoaded: boolean = false;

    public exportLimit: number = 0;
    public searchExportTooltip: string = "Export to Excel";
    public isExportInProgress: boolean = false;
	public canExport: boolean = false;


    showEditor: boolean = false;
    semanticType: SemanticType;
    secondarySidePanelOpen: boolean;
    secondarySidePanel: string = "detail";
	resourceUid: string;

	hrefSub: Subscription;
	selectedAsset: Record<string, unknown>;
	selectedReferenceItem: Record<string, unknown>;
	selectedTag: Record<string, unknown>;

	get assetEditorTitle(): string {
		return this.selection ? $localize`Edit Asset` : $localize`Create New Asset`;
	}

	get searchResultsLabel() {
		return $localize`Search Results`;
	}

    public extraButtons: SidePanelButton[] = [new SidePanelButton({
		label: $localize`Filters`,
		tooltip: $localize`Filters`,
		disabledTooltip: null,
		nothingSelectedMessage: $localize`Filters not available`,
		notApplicableMessage: $localize`Filters not available`,
		multipleSelectedMessage: $localize`Filters not available`,
        key: 'filters',
        icon: 'fa-filter',
        disabled: false,
        visible: true,
        needsSelection: false,
        panelMenu: [
            new PopupMenuItem({
				title: $localize`Expand All`,
                callback: () => this.filterExpandAll()
            }),
            new PopupMenuItem({
				title: $localize`Collapse All`,
                callback: () => this.filterCollapseAll()
            })
        ]
    })];


    @ViewChild("title", { static: false }) title: ElementRef;
    @ViewChild("catagoryFilter", { static: false }) catagoryFilter: CheckTree;

	constructor(
		private route: ActivatedRoute,
		protected titleService: Title,
		protected featureFlagService: FeatureFlagsInitService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected secondaryNavService: SecondaryNavService,
		public sidePanelService: SidePanelService,
		protected searchService: SearchService,
        private dataProfileService: DataProfileService,
        protected settingsService: CompanySettingsService,
		private linkClickInterceptor: LinkClickInterceptor) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;

		this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
			this.linkClickInterceptor.handleEvent(this, ev);
		});
    }

    ngOnInit() {
		this.setBrowserTitle(this.titleService, this.searchResultsLabel);

        this.headerBreadcrumbService.clearBreadcrumbs();
		this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.searchResultsLabel));

        this.secondaryNavService.clearItems();
        this.secondaryNavService.clearButtons();
        this.secondaryNavService.clearCurrentObject();
		this.secondaryNavService.setCurrentArea(this.searchResultsLabel, 'fa-search', null);
        this.secondaryNavService.showHeader(false);

        this.searchTypes = this.settingsService.getSettingById(CompanySettingEnum.DefaultSearchTypes).StringSetting.Value.split(',');
        this.exportLimit = Math.min(5000, <number>this.settingsService.getSettingById(CompanySettingEnum.MaxExcelExportRows).ScalarValue);

        this.sub = this.route.queryParams.subscribe((params) => {
            this.searchText = params['query'] ? params['query'] : '';
			if (this.searchText !== '') {
				this.loadResults(true);
			}
        });
    }

	loadResults(includeAggregations: boolean) {
		this.isLoading = true;

		const searchQuery: SearchQuery = {
			Term: this.searchText,
			IncludeAggregations: includeAggregations,
			From: 0,
			Size: this.resultsPerPage
		};

		if (includeAggregations) {
			this.sidePanelLoading = true;
			this.treeLoading = true;
			this.pageNumber = 1;
		}
		else {
			if (this.selectedFilters.length > 0) {
				searchQuery.AggregationFilters = [];

				this.selectedFilters.forEach((filter) => {
					if (filter.type === "Class") {
						searchQuery.AggregationFilters.push({ Class: filter.key });
					}
					else {
						searchQuery.AggregationFilters.push({ Uid: filter.key })
					}
				});
			}
		}

		searchQuery.From = (this.pageNumber - 1) * this.resultsPerPage;

		this.searchService.getSearchResultsByQuery(searchQuery).subscribe((results) => {
			this.isLoading = false;
			this.sidePanelLoading = false;
			this.treeLoading = false;
			this.searchResults = results;
			if (includeAggregations) {
				// Then we should reset the total result count.
				this.categories = this.convertCataegoriesToCheckTreeNodes(results.Aggregations);
				this.resultCount = results.Matches;

				this.canExport = this.resultCount <= this.exportLimit;
				this.searchExportTooltip = (this.resultCount <= this.exportLimit) ? $localize`Export to Excel` : $localize`No more than ${this.exportLimit} items can be exported.\nPlease refine your search.`;
			}
		});
	}

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
        if (this.PageNumberSub) {
            this.PageNumberSub.unsubscribe();
		}
		if (this.hrefSub) {
			this.hrefSub.unsubscribe();
		}
    }

	resultSelected($event) {
		this.selection = $event;
		this.selectedAsset = this.selectedReferenceItem = this.selectedTag = null;
		if (!$event || !$event.IsNew) {
			return;
		}
        if (this.selection && this.selection.HasProfiling) {
            this.sidePanelLoading = true;
            this.dataProfileService.getDataProfiles(this.selection.AssetUid).subscribe(
                (r) => {
                    if (r && r.items && r.items.length > 0) {
                        this.dataProfile = r.items[0];

                        forkJoin(
                            this.dataProfileService.getMatchCounts(this.dataProfile.assetUid, 'Structure'),
                            this.dataProfileService.getMatchCounts(this.dataProfile.assetUid, 'Data')
                        ).subscribe((res) => {
                            this.dataProfile['matches'] = {
                                structure: res[0],
                                data: res[1]
                            };
                        });
                    }
                    this.sidePanelLoading = false;
                });
        } else {
            this.dataProfile = null;
        }
    }

	convertCataegoriesToCheckTreeNodes(aggregations: SearchAggregation[]): CheckTreeNode[] {
		const nodes: CheckTreeNode[] = [];

		aggregations.forEach((agg) => {
			const node: CheckTreeNode = {
				label: agg.DisplayName,
				key: agg.Class,
				type: "Class",
				data: agg,
				count: agg.ResultCount,
				leaf: !agg.Items || (agg.Items && agg.Items.length === 0)
			};
			if (agg.Items && agg.Items.length > 0) {
				node.children = agg.Items.map((t) => {
					return {
						label: t.DisplayName,
						key: t.Uid,
						type: "AssetType",
						data: t,
						count: t.ResultCount,
						leaf: true
					};
				});
			}
			nodes.push(node);
		});

		return nodes;
	}

    get panelApplies(): boolean {
        let ret = false;
        if (this.sidePanelTab === "filters" || this.selection == null) {
            ret = true;
        } else if (this.sidePanelTab === "detail") {
            ret = this.selection?.AssetUid !== null;
        } else if (this.sidePanelTab === "dataprofile") {
            ret = this.selection.HasProfiling;
        }
        return ret;
    }

    //Class/assettype selection changed
    public filterCheckTree(selectedNodes: CheckTreeNode[]) {
		this.pageNumber = 1;
		this.loadResults(false);
    }

    public isExportEnabled(): boolean {
        return !this.isExportInProgress && this.canExport;
    }

    public doExport() {
        const fileName = "SearchResults";
        this.isExportInProgress = true;

        //this.searchStateService.getExcel(this.exportLimit)
        //    .subscribe((data) => {
        //        this.searchStateService.downloadFile(data, fileName);
        //        this.isExportInProgress = false;
        //    }, (err) => {
        //        // eslint-disable-next-line no-console
        //        this.isExportInProgress = false;
        //    });
    }

    paginate(data) {
        /*
            event.page: New page number
            event.rows: Number of rows to display in new page
        */
		this.pageNumber = data.page;
		this.resultsPerPage = data.rows;
		this.resultSelected(null);
		this.loadResults(false);
    }

    public filterClear() {
        this.catagoryFilter?.clearSelection();
    }
    public filterExpandAll() {
        this.catagoryFilter?.expandAll();
    }
    public filterCollapseAll() {
        this.catagoryFilter?.collapseAll();
    }

    getSidePanelWidth(): number {
        return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.sidePanelStorageKey);
    }

    getSidePanelMaxWidth(): number {
        return this.sidePanelService.getSidePanelMaxWidth(this.sidePanelOpen);
    }

    getSidePanelMinWidth(): number {
        return this.sidePanelService.getSidePanelMinWidth(this.sidePanelOpen);
    }

    onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData): void {
        this.sidePanelService.onSidePanelDragEnd(sidePanelStorageKey, event);
    }

    saveItem() {
        this.showEditor = false;
		this.loadResults(false);
    }

    secondaryPanelOpen(event: any) {
        this.secondarySidePanelOpen = true;
        if (event) {
            if (event.resourceUid) {
                this.secondarySidePanel = "user";
                this.resourceUid = event.resourceUid;
            }
            if (event.semanticType) {
                this.secondarySidePanel = "detail";
                this.semanticType = event.semanticType;
            }
        } else {
            this.secondarySidePanel = "status";
        }
    }

    getQualifier(selection: SearchSelection): string {
        const pipe = selection.ID.indexOf("|", 0) + 1;
        return selection.ID.substring(pipe);
    }
}