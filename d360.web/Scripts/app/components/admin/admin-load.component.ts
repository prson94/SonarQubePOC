
import { Component, NgZone } from '@angular/core';
import { PageHeader } from '../../services/page-header.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { LoadDetail } from '../../models/load.model';
import { AdminBaseComponent } from './admin-base.component';
import { FormMode } from '../../models/form.model';
import { LoadService } from '../../services/load.service';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-admin-load',
    providers: [LoadService],
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