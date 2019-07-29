import {Component, NgZone, OnInit} from '@angular/core';
import {Router} from '@angular/router';
import {Breadcrumb} from '../../../models/breadcrumb.model';
import {HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import {AdminBaseComponent} from '../admin-base.component';
import {GroupService} from '../../../services/group.service';
import {GroupSearchResultModel, Group, ResourceGroup, GroupEditorModel} from '../../../models/group.model';
import {FormMode} from '../../../models/form.model';
import {Title} from '@angular/platform-browser';
import {SiteUrlHelpers} from '../../../static/site-url-helpers';
import {StringConstants} from '../../../static/string-constants';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-admin-groups',
    providers: [GroupService],
    templateUrl: './admin-groups.component.html'
})

export class AdminGroupsComponent extends AdminBaseComponent {

    private selectedRow: GroupSearchResultModel;
    private groupItems: GroupSearchResultModel[];
    private formMode: FormMode = FormMode.Default;
    private FormMode = FormMode;

    constructor(
        private router: Router,
        private groupService: GroupService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        rightSidebarService: RightSidebarService,
        titleService: Title,
        protected messagesService: MessagesObservableService
    ) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.areaName = "Groups";
        this.adminHeading = "Security";
        this.setCommonItems();
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;

        this.groupService.getGroupList().subscribe(
            d => {
                this.groupItems = d;
                this.selectedRow = this.groupItems[0];

                this.isLoading = false;
            }
        );
    }

    add() {
        this.formMode = FormMode.Adding;
    }

    edit(id: number) {
        this.selectedRow = this.groupItems.find(i => i.ID == id);
        this.formMode = FormMode.Editing;
    }

    cancel() {
        this.formMode = FormMode.Default;
    }

    delete(id: number) {
        this.selectedRow = this.groupItems.find(i => i.ID == id);
        this.formMode = FormMode.Deleting;
    }

    confirmDelete(e: any) {
        this.messagesService.showInfoMessage('Success', 'Item deleted successfully');
        this.formMode = FormMode.Default;
        this.load();
    }

    errorDelete(e: any) {
        this.messagesService.showError('Error', 'An error occurred');
        this.formMode = FormMode.Default;
        this.load();
    }

    select(e) {
        this.selectedRow = e.data;
    }

    private groupUrl(id: number) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl(StringConstants.ObjectGroup, id));
    }

    success(e: any) {
        this.showMessageForResult(this.messagesService, e);
        this.formMode = FormMode.Default;
        this.load();
    }

    error(e: any) {
        this.showMessageForResult(this.messagesService, e);
        this.formMode = FormMode.Default;
    }
}
