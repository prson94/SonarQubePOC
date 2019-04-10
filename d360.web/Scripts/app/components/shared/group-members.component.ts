import {Input, Output, Component, OnChanges, SimpleChange} from '@angular/core';
import {GroupResourceInfo, IGroupService, GroupSearchResultModel, ResourceGroup} from '../../models/group.model';
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
    field: EditorField;
    private groupItems = new Array<GroupResourceInfo>();
    private selectedRow = new GroupResourceInfo();
    private formMode: FormMode = FormMode.Default;
    private FormMode = FormMode;
    private resourceList: SelectItem[];
    private selectedResource: string;


    constructor(private groupService: GroupService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'groupId') {
                this.formMode = FormMode.Default;
                this.load();
            }

        }
    }

    load(): void {
        if (!this.groupId) {
            return;
        }
        this.field = new EditorField();
        this.field.TypeaheadUri = `form/GetGroupUserList?id=${this.groupId}`;
        this.field.FieldName = "resources";
        this.field.MultiSelect = true;
        this.isLoading = true;
        this.groupService.getGroupResourceList(this.groupId).subscribe(
            d => {
                this.groupItems = d;
                if (this.groupItems.length > 0) {
                    this.selectedRow = this.groupItems[0];
                }

                this.isLoading = false;
            }
        );
    }

    cancel() {
        this.formMode = FormMode.Default;
    }

    save() {

        if (!(this.field.Value != null && this.field.Value.length > 0)) return;

        this.isLoading = true;
        let resources: ResourceGroup[] = [];
        try {
            this.field.Value.forEach(x => {
                var rg = new ResourceGroup();
                rg.GroupID = this.groupId;
                rg.IsOwner = false;
                rg.ResourceID = parseInt(x.split('|')[1]);
                resources.push(rg);

            })

        } catch (e) {
            this.isLoading = false;
        }

        this.groupService.postResourceGroup(resources).subscribe(
            r => {
                this.load();
                this.formMode = FormMode.Default;

                this.isLoading = false;
            }
        );
    }


    add(): void {
        this.isLoading = true;
        this.field = new EditorField();
        this.field.TypeaheadUri = `form/GetGroupUserList?id=${this.groupId}`;
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
