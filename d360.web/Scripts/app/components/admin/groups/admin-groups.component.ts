import {Component} from '@angular/core';
import {Router} from '@angular/router';
import {HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import {AdminBaseComponent} from '../admin-base.component';
import {GroupService} from '../../../services/group.service';
import {GroupApiModel, AddUserToGroup} from '../../../models/group.model';
import {FormMode} from '../../../models/form.model';
import {Title} from '@angular/platform-browser';
import {SiteUrlHelpers} from '../../../static/site-url-helpers';
import {StringConstants} from '../../../static/string-constants';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-admin-groups',
    providers: [GroupService],
    templateUrl: './admin-groups.component.html'
})

export class AdminGroupsComponent extends AdminBaseComponent {

    selectedRow: GroupApiModel;
    groupItems: GroupApiModel[];
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;
    theDeleteCallback: Function;
    groupUid: string;
    public showDelete: boolean = false;

    constructor(
        private router: Router,
        private groupService: GroupService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        secondaryNavService: SecondaryNavService,
        titleService: Title,
        protected messagesService: MessagesObservableService
    ) {
        super(headerBreadcrumbService, titleService, secondaryNavService);
        this.areaName = StringConstants.Section_Groups;
        this.adminHeading = StringConstants.SubArea_Security;
        this.setCommonItems();
        this.theDeleteCallback = this.deleteService.bind(this);
        this.buildSecondaryNavigationForObject(0, 'GroupType');
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;

        this.groupService.getGroups().subscribe(
            d => {
                this.groupItems = d.items;
                this.selectedRow = this.groupItems[0];

                this.isLoading = false;
            }
        );
    }

    add() {
        this.formMode = FormMode.Adding;
    }

    edit(Uid: string) {
        this.selectedRow = this.groupItems.find(i => i.Uid == Uid);
        this.formMode = FormMode.Editing;
    }

    cancel() {
        this.formMode = FormMode.Default;
    }

    delete(Uid: string) {
        this.groupUid = Uid;
        this.selectedRow = this.groupItems.find(i => i.Uid == Uid);
        this.formMode = FormMode.Deleting;
    }

    confirmDelete(e: any) {
        this.messagesService.showInfoMessage('Success', 'Item deleted successfully');
        this.formMode = FormMode.Default;
        this.load();
    }

    errorDelete(e: any) {
        if (e && e.result && e.result.type === "error") 
            this.messagesService.showError('Error', e.result.message);
        else
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
        this.showMessageForApiResponse(this.messagesService, e[0]);
        this.formMode = FormMode.Default;
        this.load();
    }

    error(e: any) {
        this.showMessageForApiResponse(this.messagesService, e[0]);
        this.formMode = FormMode.Default;
    }

    deleteService() {
        this.groupService.deleteGroupWithUid(this.groupUid).subscribe(
            result => {
                this.showDelete = false;
                this.formMode = FormMode.Default;
                this.load();
                this.showMessageForResult(this.messagesService, result);
            }
        );
    }
}
