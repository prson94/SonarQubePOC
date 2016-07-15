///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { NgSwitch, NgSwitchCase, NgSwitchDefault } from '@angular/common';
import { PageHeader } from '../../services/page-header.service';
import { ObjectDetailTile } from '../tiles/object-detail.tile';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { LoadDetail } from '../../models/load.model';
import { DataTable, Column } from 'primeng/primeng';
import { AdminBaseComponent} from './admin-base.component';
import { FormMode } from '../../models/form.model';
import { DeleteForm } from '../forms/delete.form';
import { LoadService } from '../../services/load.service';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { LoadItemTile } from '../tiles/load-item.tile';
import { LoadForm } from '../forms/load.form';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-admin-load',
    providers: [LoadService],
    directives: [
        ObjectDetailTile,
        TileActionsComponent,
        LoadItemTile,
        DataTable,
        Column,
        NgSwitch,
        NgSwitchCase,
        NgSwitchDefault,
        DeleteForm,
        LoadForm
    ],
    templateUrl: 'scripts/app/components/admin/admin-load.component.html',
})

export class AdminLoadComponent extends AdminBaseComponent {
    loads: LoadDetail[];
    selectedRow: LoadDetail;
    objectType = 'Load';
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    constructor(pageHeader: PageHeader, headerBreadcrumbService: HeaderBreadcrumbService, private loadService: LoadService, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService);
        this.areaDescription = "You can bulk load almost any piece of content contained within the Data3Sixty platform.";
        this.areaName = "Bulk Loading";
        this.setCommonItems();

        this.load();
    }

    load() {

        this.isLoading = true;
        this.loadService.getLoads()
            .then(data => {
                this.loads = data;
                this.selectedRow = this.loads[0];
                this.isLoading = false;
            });
    }
}