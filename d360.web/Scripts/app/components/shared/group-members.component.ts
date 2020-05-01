import {Input, Output, Component, OnChanges, SimpleChange} from '@angular/core';
import { GroupResourceInfo, IGroupService, GroupSearchResultModel, ResourceGroup, ResourceGroupInfo, AddUserToGroup } from '../../models/group.model';
import {GroupService} from '../../services/group.service';
import {FormMode, FormHelper, SelectItem} from '../../models/form.model';
import {BaseComponent} from '../shared/base.component';
import {JsonResult} from '../../models/jsonresult.model';
import {EditorField} from '../../models/editor-field.model';
import * as _ from 'lodash';
import {ResourcesService} from '../../services/resources.service';

@Component({
    selector: 'd3s-group-members',
    templateUrl: './group-members.component.html',
    providers: [GroupService, ResourcesService]
})

export class GroupMembersComponent extends BaseComponent implements OnChanges {
    @Input() groupId: number;
    @Input() groupName: string;
    @Input() title: string = 'Members';
    @Input() groupUid: string;
    field: EditorField;
    private groupItems = new Array<GroupResourceInfo>();
    private selectedRow = new GroupResourceInfo();
    private formMode: FormMode = FormMode.Default;
    private FormMode = FormMode;
    private selectedResource: string;
    private members = new Array<AddUserToGroup>();


    constructor(private groupService: GroupService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'groupId' || p =='groupUid') {
                this.formMode = FormMode.Default;
                this.load();
            }

        }
    }

    load(): void {
        if (!this.groupId && this.groupId==0 && !this.groupUid) {
            return;
        }

        this.field = new EditorField();
        this.field.TypeaheadUri = `form/GetGroupUserList?id=${this.groupId}&uid=${this.groupUid}`;
        this.field.FieldName = "resources";
        this.field.MultiSelect = true;
        this.isLoading = true;
        this.groupService.getGroupUid(this.groupId).subscribe(
            g => {
                if (g.length > 0) {
                    this.groupUid = g[0].uid;
                    this.groupService.getGroupResourceList(this.groupUid).subscribe(
                        d => {
                            if (d != undefined)
                                this.groupItems = d.items;
                            if (this.groupItems.length > 0) {
                                this.selectedRow = this.groupItems[0];
                            }

                            this.isLoading = false;
                        }
                    );
                }
            })
        if (this.groupUid != undefined) {
            this.groupService.getGroupResourceList(this.groupUid).subscribe(
                d => {
                    if (d != undefined) {
                        this.groupItems = d.items;
                        if (this.groupItems.length > 0) {
                            this.selectedRow = this.groupItems[0];
                        }

                        this.isLoading = false;
                    }
                }
            );
        }
    }

    cancel() {
        this.formMode = FormMode.Default;
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
            r => {
                this.load();
                this.formMode = FormMode.Default;

                this.members = [];
                this.isLoading = false;
            }
        );
    }


    add(): void {
        this.isLoading = true;
        this.field = new EditorField();
        this.field.TypeaheadUri = `form/GetGroupUserList?id=${this.groupId}&uid=${this.groupUid}`;
        this.field.FieldName = "resources";
        this.field.MultiSelect = true;
        this.formMode = FormMode.Adding;

    }


    delete(id: number): void {
        this.formMode = FormMode.Deleting;
        this.selectedRow = this.groupItems.find(f => f.ResourceID == id);
    }

    confirmDelete() {
        this.formMode = FormMode.Default;
        this.load();
    }
}
