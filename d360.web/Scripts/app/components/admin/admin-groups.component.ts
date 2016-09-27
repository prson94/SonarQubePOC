
import { Component, NgZone, OnInit } from '@angular/core';
import { PageHeader } from '../../services/page-header.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { AdminBaseComponent } from './admin-base.component';
import { GroupService } from '../../services/group.service';
import { GroupSearchResultModel, Group, ResourceGroup, GroupEditorModel } from '../../models/group.model';
import { FormMode } from '../../models/form.model';
import { Title } from '@angular/platform-browser';


@Component({
    selector: 'd3s-admin-groups',
    providers: [ GroupService ],
    templateUrl: 'scripts/app/components/admin/admin-groups.component.html'  
})

export class AdminGroupsComponent extends AdminBaseComponent {

    private selectedRow: GroupSearchResultModel;
    private groupItems: GroupSearchResultModel[];
    private formMode: FormMode = FormMode.Default;
    private FormMode = FormMode;

    constructor(private groupService: GroupService, pageHeader: PageHeader, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService);
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
        this.selectedRow = this.groupItems.find(i => i.ID == id);
        //console.log(id);
        //console.log(this.groupItems);
        //console.log(this.selectedRow);
        this.formMode = FormMode.Editing;

    }

    cancel() {
        this.formMode = FormMode.Default;
    }
    delete(id: number) {
        this.selectedRow = this.groupItems.find(i => i.ID == id);
        this.formMode = FormMode.Deleting;
    }

    confirmDelete() {
        this.formMode = FormMode.Default;
        this.load();
    }

    select(e) {
        this.selectedRow = e.data;
        //console.log(this.selectedRow);
    }
}