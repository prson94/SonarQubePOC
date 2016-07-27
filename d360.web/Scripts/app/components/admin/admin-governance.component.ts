///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone, OnDestroy } from '@angular/core';
import { NgSwitch, NgSwitchCase, NgSwitchDefault } from '@angular/common';
import { ObjectDetailTile } from '../tiles/object-detail.tile';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService, RightSidebarService, ResponsibilityTypeService, PageHeader } from '../../services/index';
import { DataTable, Column } from 'primeng/primeng';
import { ResponsibilityType, IResponsibilityTypeService } from '../../models/responsibility-type.model';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { FormMode } from '../../models/form.model';
import { ResponsibilityTypeForm } from '../forms/responsibility-type.form';
import { DeleteForm } from '../forms/delete.form';
import { AdminBaseComponent} from './admin-base.component';
import { Title } from '@angular/platform-browser';
import { AuditComponent} from '../shared/audit.component';

@Component({
    selector: 'admin-governance',
    providers: [ResponsibilityTypeService],
    directives: [
        ObjectDetailTile,
        DataTable,
        Column,
        TileActionsComponent,
        NgSwitch,
        NgSwitchCase,
        NgSwitchDefault,
        ResponsibilityTypeForm,
        DeleteForm,
        AuditComponent
    ],
    templateUrl: 'scripts/app/components/admin/admin-governance.component.html',
})

export class AdminGovernanceComponent extends AdminBaseComponent implements OnDestroy {    
    private formMode = FormMode.Default;
    private FormMode = FormMode;

    private responsibilityTypeItems = new Array<ResponsibilityType>();
    private selectedRow = new ResponsibilityType();

    constructor(rightSidebarService: RightSidebarService, private responsibilityTypeService: ResponsibilityTypeService, pageHeader: PageHeader, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService, rightSidebarService);
        this.areaDescription = 'Assign which objects can be owned, and whether groups, users or both may own them. You may also define application and licensing source types.';
        this.areaName = "Responsibility Types";
        this.setCommonItems();
        this.setCommonRightSideBar();
        this.load();
    }
    

    ngOnDestroy() {
        this.clearSidebar();
    }

    load(): void {
        this.responsibilityTypeService.getResponsibilityTypes()
            .then(data => {
                this.responsibilityTypeItems = data;
                this.selectedRow = this.responsibilityTypeItems[0];
            });
    }

    add(): void {
        this.formMode = FormMode.Adding;
    }

    edit(id: number): void {
        this.formMode = FormMode.Editing;
        //console.log(id);
        this.selectedRow = this.responsibilityTypeItems.find(i => i.ID == id);
        //console.log(this.selectedRow);
        //console.log(this.responsibilityTypeItems);

    }
     
    delete(id: number): void {
        this.formMode = FormMode.Deleting;
        this.selectedRow = this.responsibilityTypeItems.find(i => i.ID == id);
    }

    save() {
        this.formMode = FormMode.Default;
        this.load();
    }

    confirmDelete() {
        this.formMode = FormMode.Default;
        this.load();
    }

    cancel() {
        this.formMode = FormMode.Default;
    }
}