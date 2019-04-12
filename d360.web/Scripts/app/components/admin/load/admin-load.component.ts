import {Component, OnInit, ViewChild} from '@angular/core';
import {Title} from '@angular/platform-browser';

import {LoadDetail} from '../../../models/load.model';
import {FormMode} from '../../../models/form.model';

import {HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import {GetLoadService} from '../../../services/load/get-load.service';

import {AdminBaseComponent} from '../admin-base.component';

@Component({
    selector: 'd3s-admin-load',
    providers: [GetLoadService],
    templateUrl: './admin-load.component.html',
})

export class AdminLoadComponent extends AdminBaseComponent implements OnInit {
    loads: LoadDetail[] = [];
    selectedRow: LoadDetail;
    objectType = 'Load';
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    constructor(headerBreadcrumbService: HeaderBreadcrumbService, private getLoadService: GetLoadService, titleService: Title) {
        super(headerBreadcrumbService, titleService);
        this.areaName = "Bulk Loading";
        this.setCommonItems();
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;

        this.getLoadService.getLoads().subscribe(
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
