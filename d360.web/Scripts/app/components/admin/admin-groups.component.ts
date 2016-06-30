///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone, OnInit } from '@angular/core';
import { NgSwitch, NgSwitchCase, NgSwitchDefault } from '@angular/common';
import { PageHeader } from '../../services/page-header.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { AdminBaseComponent } from './admin-base.component';
import { DataTable, Column } from 'primeng/primeng';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { GroupMembersTile } from '../tiles/group-members.tile';
import { GroupService } from '../../services/group.service';
import { GroupSearchResultModel, Group, ResourceGroup, GroupEditorModel } from '../../models/group.model';
import { FormMode } from '../../models/form.model';


@Component({
    selector: 'd3s-admin-groups',
    directives: [DataTable, Column, TileActionsComponent, NgSwitch, NgSwitchCase, NgSwitchDefault, GroupMembersTile ],
    providers: [ GroupService ],
    templateUrl: 'scripts/app/components/admin/admin-groups.component.html' 
})

export class AdminGroupsComponent extends AdminBaseComponent {

    private selectedRow: GroupSearchResultModel;
    private groupItems: GroupSearchResultModel[];
    private formMode: FormMode = FormMode.Default;
    private FormMode = FormMode;

    constructor(private groupService: GroupService, pageHeader: PageHeader, headerBreadcrumbService: HeaderBreadcrumbService) {
        super(headerBreadcrumbService, pageHeader);
        this.areaDescription = "Here you will find groups and membership.";
        this.areaName = "Groups";
        this.setCommonItems();
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;
        this.groupService.getGroupList()
            .then(d => {
                this.groupItems = d;
                this.selectedRow = this.groupItems[0];
                this.isLoading = false;
            });
    }

    add() {
        this.formMode = FormMode.Adding;
    }

    edit(id: number) {
        this.formMode = FormMode.Editing;
        this.selectedRow = this.groupItems.find(i => i.ID == id);
    }

    delete(id: number) {
        this.formMode = FormMode.Deleting;
        this.selectedRow = this.groupItems.find(i => i.ID == id);
    }

    confirmDelete() {
        this.formMode = FormMode.Default;
        this.load();
    }
}