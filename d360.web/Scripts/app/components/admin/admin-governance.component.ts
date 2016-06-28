///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { NgSwitch, NgSwitchCase, NgSwitchDefault } from '@angular/common';
import { PageHeader} from '../../services/page-header.service';
import { ObjectDetailTile } from '../tiles/object-detail.tile';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { DataTable, Column } from 'primeng/primeng';
import { ResponsibilityType, IResponsibilityTypeService } from '../../models/responsibility-type.model';
import { ResponsibilityTypeService } from '../../services/responsibility-type.service';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { FormMode } from '../../models/form.model';
import { ResponsibilityTypeForm } from '../forms/responsibility-type.form';
import { DeleteForm } from '../forms/delete.form';

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
        DeleteForm
    ],
    templateUrl: 'scripts/app/components/admin/admin-governance.component.html',
})

export class AdminGovernanceComponent {
    private isLoading = false; 

    private formMode = FormMode.Default;
    private FormMode = FormMode;

    private responsibilityTypeItems = new Array<ResponsibilityType>();
    private selectedRow = new ResponsibilityType();

    constructor(private responsibilityTypeService: ResponsibilityTypeService, private pageHeader: PageHeader, private headerBreadcrumbService: HeaderBreadcrumbService) {
        this.pageHeader.title = 'Responsibility Types';
        this.pageHeader.description = 'Assign which objects can be owned, and whether groups, users or both may own them. You may also define application and licensing source types.';

        headerBreadcrumbService.clearBreadcrumbs();
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Administration", ""));
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Responsibility Types", ""));

        this.load();
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