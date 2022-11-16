import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    OnDestroy,
    ViewChild,
    ViewEncapsulation
} from '@angular/core';
import { Router } from '@angular/router';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { AdminBaseComponent } from '../admin-base.component';
import { GroupService } from '../../../services/group.service';
import { GroupApiModel } from '../../../models/group.model';
import { Title } from '@angular/platform-browser';
import { StringConstants } from '../../../static/string-constants';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { GridDefinitionService } from '../../../services/grid-definition.service';
import { GridColumn, GridField } from '../../../models/grid-definition.model';
import { forkJoin, Subject, Subscription } from 'rxjs';
import { AssetTypeClass } from '../../../models/asset.model';
import { AssetEditorComponent } from '../../shared/asset-editor/asset-editor.component';
import { Table } from 'primeng/table';
import { AssetDetailComponent } from '../../shared/asset-detail/asset-detail.component';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { FeatureFlags, FeatureFlagsService } from "../../../services/featureflags.service";
import { V2ApiFilters } from '../../../models/asset-search.model';
import { NumberOfRowsByCategoryService } from '../../../services/number-of-rows-by-category.service';
import { takeUntil } from 'rxjs/operators';
import { LazyLoadEvent } from 'primeng/api';
import { isEqual } from 'lodash';
import { SidePanelService } from '../../../services/side-panel.service';
import { IOutputData } from 'angular-split';
import { SortOrder } from '../../../models/enums.model';

declare var CurrentResourceID;
@Component({
    selector: 'd3s-admin-groups',
    providers: [GroupService],
    changeDetection: ChangeDetectionStrategy.Default,
    templateUrl: './admin-groups.component.html',
    styleUrls: ['admin-groups.component.less'],
    encapsulation: ViewEncapsulation.None
})

export class AdminGroupsComponent extends AdminBaseComponent implements OnDestroy {
    selectedRow: GroupApiModel;
    groupItems: GroupApiModel[];

    theDeleteCallback: Function;
    groupUid: string;
    public showDelete: boolean = false;

    selection: any = null;
    sidePanelOpen: boolean = false;
    sidePanelLoading: boolean = false;
    sidePanelStorageKey: string;
    sidePanelTab: string = 'detail';

    createButtonLabel = $localize`Create New Group`;
    deleteModalTitle = $localize`Delete Group`;
    editLabel = $localize`Edit`;

    labelCancel = $localize`Cancel`;
    labelDelete = $localize`Delete`;

	groupListHeading: string = 'Groups';

    simpleTextFilter: string = '';

    columns: GridColumn[] = [];
    fields: GridField[] = [];

    showEditor: boolean = false;
    loadSub: Subscription;

    deleteInProgress: boolean = false;
	isContainsSearchDefault: boolean = false;

	hrefSub: Subscription;
    selectedAsset: any;
    selectedReferenceItem: any;
    selectedTag: any;

	previousEvent: LazyLoadEvent;
    sortOrder: number = SortOrder.None;
    sortField: string = "";
	currentPageNumber: number = 0;
	totalRecords: number;
	rowsPerPage: number = this.defaultInitialItemsPerPage;
	defaultInitialItemsPerPage: number = 10;

	private destroy = new Subject<void>();

    @ViewChild('dynamicEditor', { static: false }) dynamicEditor: AssetEditorComponent;
    @ViewChild('dt', { static: false }) table: Table;
    @ViewChild('assetDetail', { static: false }) assetDetail: AssetDetailComponent;

    menuItems = [
        { title: $localize`Edit` },
        { title: $localize`Delete` },
    ];

    constructor(
        private router: Router,
        private groupService: GroupService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        secondaryNavService: SecondaryNavService,
        titleService: Title,
        private gridDefinitionService: GridDefinitionService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef,
        public sidePanelService: SidePanelService,
        private linkClickInterceptor: LinkClickInterceptor,
		private featureFlagService: FeatureFlagsService,
		public numberOfRowsByCategoryService: NumberOfRowsByCategoryService
    ) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.areaName = StringConstants.Section_Groups;
        this.adminHeading = StringConstants.SubArea_Security;
		this.setCommonItems();
		this.baseAssetTypeUid = this.groupTypeUid;
		this.buildSecondaryNavigationForAssetTypeUid(this.groupTypeUid);

        this.sidePanelStorageKey = 'list_' + AssetTypeClass.Group + '_' + CurrentResourceID;

        this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
            this.linkClickInterceptor.handleEvent(this, ev);
        });
		
		this.isContainsSearchDefault = this.featureFlagService.flags[FeatureFlags.ContainsSearchDefaultUiFlag];
	}

    ngOnInit() {
		this.load();

		this.setRowsPerPage();
		this.numberOfRowsByCategoryService.defineNumberOfRows(this.defaultInitialItemsPerPage);
    }

    ngOnDestroy() {
        if (this.loadSub) {
            this.loadSub.unsubscribe();
		}

		this.destroy.next();
		this.destroy.complete();
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

	setRowsPerPage(): void {
		this.numberOfRowsByCategoryService.rowsPerPage.pipe(
			takeUntil(this.destroy)
		).subscribe((rowsPerPage) => {
			this.rowsPerPage = rowsPerPage[this.groupListHeading] || this.defaultInitialItemsPerPage;
		});
	}

	public lazyLoadGroups(event: LazyLoadEvent) {
		if (isEqual(event, this.previousEvent)) {
			return;
		}
		this.previousEvent = event;

		this.rowsPerPage = event.rows;
		this.currentPageNumber = event.first / event.rows;
        this.sortOrder = event.sortOrder;
        this.sortField = event.sortField;
        
		this.load();
	}

    load() {
        this.isLoading = true;

        if (this.loadSub) {
            this.loadSub.unsubscribe();
        }
		
		this.loadSub = forkJoin(this.gridDefinitionService.getGridDefinition(1, "GroupType"), this.groupService.getGroupsLazy(this.getParams()))
            .subscribe((res) => {
				var gridDefinition = res[0];
				var groups = res[1];

                if (this.columns.length === 0 && this.fields.length === 0) {
                    this.columns = gridDefinition.Columns.filter((x) => x.datafield !== 'Name');
                    this.fields = gridDefinition.Fields;
                }

				this.totalRecords = groups.Total;
				this.groupItems = groups.items;

				if (this.selectedRow) {
					var sItem = this.groupItems.filter((item) => item.Uid === this.selectedRow.Uid);
					if (sItem.length > 0) {
						this.selectedRow = sItem[0];
					}
					else {
						this.selectedRow = null;
					}
				}

				this.isLoading = false;
				this.cdRef.markForCheck();
            });
    }

	getParams() {
		var params = new V2ApiFilters();

		if (this.simpleTextFilter) {
			params._simpleFilter = this.isContainsSearchDefault ? `*${this.simpleTextFilter}*` : this.simpleTextFilter;
		}
		else {
			delete params['_simpleFilter'];
		}

        if (this.sortField) {
            params._order = this.sortField;
        }

        if (this.sortOrder !== SortOrder.None) {
            params._direction = this.sortOrder === SortOrder.Ascending ? "asc" : "desc";
        }

		params._pageNum = this.currentPageNumber + 1;
		params._pageSize = this.rowsPerPage;

		return params;
	}

    add() {
        this.selectedRow = null;
        this.showEditor = true;
    }
    edit(item) {
        this.selectedRow = item;
        this.showEditor = true;
    }
    selectRow(data) {
        this.selectedRow = data;
        this.selectedAsset = this.selectedReferenceItem = this.selectedTag = null;
    }

    saveItem($event) {
        this.showEditor = false;
        this.load();
        if ($event.addAnother) {
            this.add();
            if (this.dynamicEditor) {
                this.dynamicEditor.load();
            }
        }

        //reload group detail component
        if (this.assetDetail) {
            this.assetDetail.load(false);
        }
    }

    deleteGroup(item) {
        this.selectedRow = item;
        this.showDelete = true;
    }

    delete() {
        this.deleteInProgress = true;
        this.groupService.deleteGroupWithUid(this.selectedRow.Uid).subscribe(
            (result) => {
                this.showDelete = false;
                this.selectedRow = null;
                this.load();
                this.deleteInProgress = false;
                this.showMessageForResult(this.messagesService, result);
            }
        );
    }

    clickMenuItem(event: any, item: any) {
        let key = event.value.toLowerCase();
        if (key === $localize`Edit`.toLowerCase()) {
            this.edit(item);
        } else if (key === $localize`Delete`.toLowerCase()) {
            this.deleteGroup(item);
        }
    }

    onSimpleSearch($event) {
        this.load();

        if (this.table) {
            this.table.first = 0;
        }
    }
}
