import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    OnDestroy,
    ViewChild,
    ViewEncapsulation
} from '@angular/core';
import { IOutputData } from 'angular-split';
import { isEqual } from "lodash-es";
import { LazyLoadEvent } from 'primeng/api';
import { Table } from 'primeng/table';
import { Subject, Subscription, forkJoin } from 'rxjs';
import { V2ApiFilters } from '../../../models/asset-search.model';
import { SortOrder } from '../../../models/enums.model';
import { GridColumn, GridField } from '../../../models/grid-definition.model';
import { GroupApiModel } from '../../../models/group.model';
import { GridDefinitionService } from '../../../services/grid-definition.service';
import { GroupService } from '../../../services/group.service';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { SidePanelService } from '../../../services/side-panel.service';
import { AssetDetailComponent } from '../../shared/asset-detail/asset-detail.component';
import { AssetEditorComponent } from '../../shared/asset-editor/asset-editor.component';
import { GroupBasePage } from './_base';

@Component({
    selector: 'group-list',
    providers: [GroupService],
    changeDetection: ChangeDetectionStrategy.Default,
    templateUrl: './list.html',
    styleUrls: ['list.less'],
    encapsulation: ViewEncapsulation.None
})

export class GroupList extends GroupBasePage implements OnDestroy {
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
	rowsPerPage: number = 25;
	defaultInitialItemsPerPage: number = 10;

	private destroy = new Subject<void>();

    @ViewChild('dynamicEditor', { static: false }) dynamicEditor: AssetEditorComponent;
    @ViewChild('dt', { static: false }) table: Table;
    @ViewChild('assetDetail', { static: false }) assetDetail: AssetDetailComponent;

    menuItems = [
        { title: $localize`Edit` },
        { title: $localize`Delete` },
    ];

	numberOfRowsStorageKey = 'AdminGroupsRowsPerPage';

    constructor(
        private groupService: GroupService,
        private gridDefinitionService: GridDefinitionService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef,
        public sidePanelService: SidePanelService,
        private linkClickInterceptor: LinkClickInterceptor
    ) {
		super();
		this.sidePanelStorageKey = 'list_Group_' + this.settingsService.CurrentResourceID;

        this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
            this.linkClickInterceptor.handleEvent(this, ev);
        });
	}

	loadRowsPerPage(): void {
		const rowsPerPageStorage = localStorage.getItem(this.numberOfRowsStorageKey);
		this.rowsPerPage = rowsPerPageStorage != null ? +rowsPerPageStorage : 25;
	}

	setRowsPerPage($event) {
		if ($event && $event.rows) {
			localStorage.setItem(this.numberOfRowsStorageKey, $event.rows);
		}
	}

	ngOnInit() {
		this.loadRowsPerPage();
		this.load();
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
				let gridDefinition = res[0];
				const groups = res[1];

                if (this.columns.length === 0 && this.fields.length === 0) {
                    this.columns = gridDefinition.Columns.filter((x) => x.datafield !== 'Name');
                    this.fields = gridDefinition.Fields;
                }

				this.totalRecords = groups.Total;
				this.groupItems = groups.items;

				if (this.selectedRow) {
					const sItem = this.groupItems.filter((item) => item.Uid === this.selectedRow.Uid);
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
		const params = new V2ApiFilters();

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
				this.messagesService.showMessageForResult(result);
            }
        );
    }

    clickMenuItem(event: any, item: any) {
        const key = event.value.toLowerCase();
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
