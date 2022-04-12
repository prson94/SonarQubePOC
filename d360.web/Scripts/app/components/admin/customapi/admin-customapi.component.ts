import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { ApiService } from '../../../models/custom-api.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CustomAPIService } from '../../../services/custom-api.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-customapi',
    providers: [CustomAPIService],
    templateUrl: './admin-customapi.component.html'
})

export class AdminCustomAPIComponent extends AdminBaseComponent implements OnInit {

    public selected: ApiService = null;
    public services: ApiService[] = [];
    public showEditor: boolean = false;
    public showDelete: boolean = false;

    searchText = "Search...";
    theDeleteCallback: Function;

    constructor(
        protected customAPIService: CustomAPIService,
        secondaryNavService: SecondaryNavService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        titleService: Title,
        private router: Router
    ) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);

        this.areaName = StringConstants.Section_CustomApi;
        this.adminHeading = $localize`Integration`;
        this.setCommonItems();
        this.clearSidebar();
        this.theDeleteCallback = this.deleteService.bind(this);
    }

    ngOnInit(): void {
        this.secondaryNavService.clearItems();
        this.load();
    }

    private load(): void {
        this.isLoading = true;

        this.customAPIService.getServices().subscribe(
            res => {
                this.services = res.sort((a, b) => a.Name.localeCompare(b.Name));
                this.selected = this.services[0];
                this.isLoading = false;
            }
        );
    }

    public saveService(data): void {
        this.showEditor = false;

        this.customAPIService.saveService(data.item).subscribe(
            res => {
                this.showMessageForResult(this.messagesService, res);
                this.load();
            }
        );
    }

    public showService(item: ApiService): void {
        this.router.navigateByUrl(`admin/customapi/${item.ID}/details`);
    }

    deleteService(id: number) {
        this.customAPIService.deleteService(id).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                this.load();
            }
        );
    }

    get deleteModalTitle(): string {
        return $localize`Are you sure you want to delete the api service [${this.selected?.Name}']?`;
    }
}
