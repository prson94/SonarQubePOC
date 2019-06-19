import {Component, OnInit} from '@angular/core';
import {Router} from '@angular/router';
import {Title} from '@angular/platform-browser';

import {ApiService} from '../../../models/custom-api.model';

import {HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import {MessagesService} from '../../../services/messages.service';
import {CustomAPIService} from '../../../services/custom-api.service';
import {RightSidebarService} from '../../../services/right-sidebar.service';

import {AdminBaseComponent} from '../admin-base.component';

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

    theDeleteCallback: Function;

    constructor(
        protected customAPIService: CustomAPIService,
        rightSidebarService: RightSidebarService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private messagesService: MessagesService,
        titleService: Title,
        private router: Router
    ) {
        super(headerBreadcrumbService, titleService, rightSidebarService);

        this.areaName = "Custom API";
        this.adminHeading = 'Integration';
        this.setCommonItems();
        this.clearSidebar();
        this.theDeleteCallback = this.deleteService.bind(this);
    }

    ngOnInit(): void {
        this.rightSidebarService.clearItems();
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
}
