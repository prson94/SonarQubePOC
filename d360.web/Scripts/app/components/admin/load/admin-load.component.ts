import {Component, OnInit} from '@angular/core';
import {HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import {LoadDetail} from '../../../models/load.model';
import {AdminBaseComponent} from '../admin-base.component';
import {FormMode} from '../../../models/form.model';
import {LoadService} from '../../../services/load.service';
import {Title} from '@angular/platform-browser';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';
import '@angular/localize/init';

@Component({
    selector: 'd3s-admin-load',
    providers: [LoadService],
    templateUrl: './admin-load.component.html',
})

export class AdminLoadComponent extends AdminBaseComponent implements OnInit {
    loads: LoadDetail[] = [];
    selectedRow: LoadDetail;
    objectType = 'Load';
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    constructor(
        headerBreadcrumbService: HeaderBreadcrumbService,
        secondaryNavService: SecondaryNavService,
        private loadService: LoadService,
        protected settingsService: CompanySettingsService,
        titleService: Title
    ) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);

        this.areaName = StringConstants.Section_Bulk;
        this.adminHeading = $localize`Integration`;
        this.setCommonItems();
    }

    ngOnInit() {
        this.load();
    }

    load() {

        this.isLoading = true;
        this.loadService.getLoads().subscribe(
            (data) => {
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
