import { Input, Output, Component, OnChanges, SimpleChange, ChangeDetectionStrategy, ChangeDetectorRef, ViewEncapsulation } from '@angular/core';

import * as _ from 'lodash';
import { forkJoin } from 'rxjs';
import { EditorField } from '../../../models/editor-field.model';
import { FormMode } from '../../../models/form.model';
import { AddUserToGroup, Group, GroupResourceInfo } from '../../../models/group.model';
import { GroupService } from '../../../services/group.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { ResourcesService } from '../../../services/resources.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { BaseComponent } from '../base.component';

@Component({
    selector: 'd3s-group-members',
    templateUrl: './group-members.component.html',
    providers: [GroupService, ResourcesService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ['group-members.component.less'],
    encapsulation: ViewEncapsulation.None
})

export class GroupMembersComponent extends BaseComponent implements OnChanges {
    @Input() groupUid: string;

    groupName: string;
    title: string = 'Members';
    groupIsActiveDirectory: boolean = false;

    private groupItems = new Array<GroupResourceInfo>();
    private selectedRow = new GroupResourceInfo();

    private selectedResource: string;
    private members = new Array<AddUserToGroup>();
    theDeleteCallback: Function;
    showDelete: boolean = false;
    showAddMembers: boolean = false;


    loadedGroup: Group;
    field: EditorField;

    constructor(
        private groupService: GroupService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private cdref: ChangeDetectorRef) {
        super(settingsService);
        this.theDeleteCallback = this.deleteService.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'groupId' || p == 'groupUid') {
                this.showAddMembers = false;
                this.showDelete = false;
                this.load();
            }

        }
    }

    load(): void {
        this.field = new EditorField();
        this.field.TypeaheadUri = `form/GetGroupUserList?uid=${this.groupUid}&id=0`;
        this.field.FieldName = "resources";
        this.field.MultiSelect = true;

        this.groupIsActiveDirectory = false;
        this.isLoading = true;

        var subs = forkJoin(this.groupService.getGroupByUid(this.groupUid),
            this.groupService.getGroupResourceList(this.groupUid, this.maxExportRows));

        subs.subscribe((data) => {
            this.loadedGroup = data[0].items[0];

            this.groupName = this.loadedGroup.Name;
            this.groupIsActiveDirectory = this.loadedGroup.IsActiveDirectoryGroup;

            var members = data[1];

            if (members != undefined) {
                this.groupItems = members.items;
            }

            this.groupItems.forEach((gm) => {
                gm.Name = gm.LastName + ", " + gm.FirstName;
            });

            if (this.groupItems.length > 0) {
                this.selectedRow = this.groupItems[0];
            }

            this.isLoading = false;
            this.cdref.markForCheck();
        });
    }

    cancel() {
        this.showDelete = this.showAddMembers = false;
    }

    save() {
        if (!(this.field.Value != null && this.field.Value.length > 0)) return;

        this.isLoading = true;
        try {
            this.field.Value.forEach(x => {
                var user = new AddUserToGroup();
                user.Uid = x.split('|')[3];
                this.members.push(user);
            })

        } catch (e) {
            this.isLoading = false;
        }
        this.groupService.addUsersToGroup(this.groupUid, this.members).subscribe(
            (r) => {
                this.load();
                this.cancel();

                this.members = [];
                this.isLoading = false;
            }
        );
    }


    add(): void {
        this.showAddMembers = true;
        this.isLoading = true;
        this.field = new EditorField();
        this.field.TypeaheadUri = `form/GetGroupUserList?uid=${this.groupUid}&id=0`;
        this.field.FieldName = "resources";
        this.field.MultiSelect = true;
    }


    delete(id: number): void {
        this.showDelete = true;
        this.selectedRow = this.groupItems.find(f => f.ResourceID == id);
    }

    error(e: any) {
        this.cancel();
    }
    errorDelete(e: any) {
        this.cancel();
    }

    confirmDelete() {
        this.cancel();
        this.load();
    }

    deleteService() {
        this.groupService.deleteUsersFromGroup(this.groupUid, this.selectedRow.uid).subscribe(
            (result) => {
                this.showDelete = false;
                this.cancel();
                this.load();
                this.showMessageForResult(this.messagesService, result);
            }
        );
    }
}
