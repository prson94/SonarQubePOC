///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { NgSwitch, NgSwitchCase, NgSwitchDefault } from '@angular/common';
import { DataTable, Column } from 'primeng/primeng';
import { GroupResourceInfo, IGroupService } from '../../models/group.model';
import { GroupService } from '../../services/group.service';
import { TileActionsComponent } from './tile-actions.component';
import { DeleteForm } from '../forms/delete.form';
import { FormMode } from '../../models/form.model';

@Component({
    selector: 'd3s-group-members-tile',
    directives: [DataTable, Column, DeleteForm, TileActionsComponent, NgSwitch, NgSwitchCase, NgSwitchDefault ],
    templateUrl: 'scripts/app/components/tiles/group-members.tile.html',
    providers: [GroupService]
})

export class GroupMembersTile implements OnChanges {
    @Input() id: number;
    @Input() title: string = 'Members';

    private groupItems = new Array<GroupResourceInfo>();
    private selectedRow = new GroupResourceInfo();
    private isLoading = false;
    private formMode: FormMode = FormMode.Default;
    private FormMode = FormMode;


    constructor(private groupService: GroupService) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id') {
                this.load();
            }

        }
    }

    load(): void {
        if (!this.id) {
            return;
        }
        this.isLoading = true;
        this.groupService.getGroupResourceList(this.id)
            .then(d => {
                this.groupItems = d;
                this.isLoading = false;
            });

    }

    add(): void {
        this.formMode = FormMode.Adding;
    }

    delete(id: number): void {
        this.formMode = FormMode.Deleting;
        this.selectedRow = this.groupItems.find(f => f.ResourceID == id);
    }
}


