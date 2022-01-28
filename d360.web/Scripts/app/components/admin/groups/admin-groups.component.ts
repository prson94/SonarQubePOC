import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, ViewChild, ViewEncapsulation } from '@angular/core';
import { Router } from '@angular/router';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { AdminBaseComponent } from '../admin-base.component';
import { GroupService } from '../../../services/group.service';
import { GroupApiModel } from '../../../models/group.model';
import { Title } from '@angular/platform-browser';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { StringConstants } from '../../../static/string-constants';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { GridDefinitionService } from '../../../services/grid-definition.service';
import { GridColumn, GridField } from '../../../models/grid-definition.model';
import { forkJoin, Subscription } from 'rxjs';
import { AssetTypeClass } from '../../../models/asset.model';
import { AssetEditorComponent } from '../../shared/asset-editor/asset-editor.component';
import { Table } from 'primeng/table';

declare var CurrentResourceID;
@Component({
    selector: 'd3s-admin-groups',
    providers: [GroupService],
    changeDetection: ChangeDetectionStrategy.OnPush,
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

    simpleTextFilter: string = '';

    columns: GridColumn[] = [];
    fields: GridField[] = [];

    showEditor: boolean = false;
    loadSub: Subscription;

    deleteInProgress: boolean = false;

    @ViewChild('dynamicEditor', { static: false }) dynamicEditor: AssetEditorComponent;
    @ViewChild('dt', { static: false }) table: Table;

    menuItems = [
        { title: 'Edit' },
        { title: 'Delete' },
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
        private cdRef: ChangeDetectorRef
    ) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.areaName = StringConstants.Section_Groups;
        this.adminHeading = StringConstants.SubArea_Security;
        this.setCommonItems();
        this.buildSecondaryNavigationForObject(0, 'GroupType');

        this.sidePanelStorageKey = 'list_' + AssetTypeClass.Group + '_' + CurrentResourceID;
    }

    ngOnInit() {
        this.load();
    }

    ngOnDestroy() {
        if (this.loadSub) {
            this.loadSub.unsubscribe();
        }
    }

    load() {
        this.isLoading = true;

        if (this.loadSub) {
            this.loadSub.unsubscribe();
        }

        this.loadSub = forkJoin(this.gridDefinitionService.getGridDefinition(1, "GroupType"), this.groupService.getGroups(this.simpleTextFilter))
            .subscribe((res) => {
                var result = res[0];
                var d = res[1];

                this.columns = result.Columns.filter((x) => x.datafield !== 'Name');
                this.fields = result.Fields;

                this.groupItems = d.items;

                this.isLoading = false;
                this.cdRef.markForCheck();
            });
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
    }

    private groupUrl(id: number) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl(StringConstants.ObjectGroup, id));
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
    }

    deleteGroup(item) {
        this.selectedRow = item;
        this.showDelete = true;
    }

    delete() {
        this.deleteInProgress = true;
        this.groupService.deleteGroupWithUid(this.selectedRow.Uid).subscribe(
            result => {
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
        if (key === 'edit') {
            this.edit(item);
        } else if (key === 'delete') {
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
