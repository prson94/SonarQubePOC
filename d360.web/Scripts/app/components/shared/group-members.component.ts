///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { GroupResourceInfo, IGroupService, GroupSearchResultModel, ResourceGroup } from '../../models/group.model';
import { GroupService } from '../../services/group.service';
import { FormMode, JsonResult, FormHelper, SelectItem } from '../../models/form.model';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-group-members',
    templateUrl: 'scripts/app/components/shared/group-members.component.html',
    providers: [GroupService]
})

export class GroupMembersComponent extends BaseComponent implements OnChanges {
    @Input() groupId: number;
    @Input() groupName: string;
    @Input() title: string = 'Members';

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
        this.isLoading = true;
        this.groupService.getGroupResourceList(this.groupId)
            .then(d => {
                this.groupItems = d;       
                if (this.groupItems.length > 0) this.selectedRow = this.groupItems[0];         
                this.isLoading = false;
            });

    }

    cancel() {
        this.formMode = FormMode.Default;
    }

    save() {
        if (this.selectedResource == "")
            return;
        this.isLoading = true;
        try {
            var rg = new ResourceGroup();
            rg.GroupID = this.groupId;
            rg.IsOwner = false;
            rg.ResourceID = parseInt(this.selectedResource);
        } catch (e) {
            this.isLoading = false;
        }

        this.groupService.postResourceGroup(rg)
            .then(r => {
                this.load();
                this.formMode = FormMode.Default;
                this.isLoading = false;
            });
    }

    

    add(): void {
        this.isLoading = true;
        this.groupService.getGroupUserList(this.groupId)
            .then(d => {
                this.resourceList = d.resourceList;                
                FormHelper.mapSelectItems(this.resourceList);                
                this.formMode = FormMode.Adding;
                this.isLoading = false;
            });
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


