import {Component, OnInit} from '@angular/core';
import {Title} from '@angular/platform-browser';

import {LoadDetail} from '../../../models/load.model';
import {FormMode} from '../../../models/form.model';

import {HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import {AdminLoadService} from './admin-load.service';

import {AdminBaseComponent} from '../admin-base.component';

@Component({
    selector: 'd3s-admin-load',
    templateUrl: './admin-load.component.html',
    providers: [AdminLoadService]
})

export class AdminLoadComponent extends AdminBaseComponent implements OnInit {
    loads: LoadDetail[] = [];
    selectedRow: LoadDetail;
    objectType = 'Load';
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    constructor(
        headerBreadcrumbService: HeaderBreadcrumbService,
        private adminLoadService: AdminLoadService,
        titleService: Title
    ) {
        super(headerBreadcrumbService, titleService);

        this.areaName = "Bulk Loading";
        this.setCommonItems();
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;

        this.adminLoadService.getLoads().subscribe(
            data => {
                this.loads = data;

                this.selectedRow = this.loads.length > 0 ? this.loads[0] : null;

                this.isLoading = false;
            }
        );
    }

    refreshGrid() {
        this.selectedRow = null;
        this.load();
    }
}
