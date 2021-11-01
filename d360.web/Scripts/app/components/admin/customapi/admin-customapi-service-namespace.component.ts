import {Component, OnInit, SimpleChange} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {ActivatedRoute, Router} from '@angular/router';
import {Breadcrumb} from '../../../models/breadcrumb.model';
import {ApiNamespace, ApiService} from '../../../models/custom-api.model';
import {HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import {CustomAPIService} from '../../../services/custom-api.service';
import {SecondaryNavService} from '../../../services/right-sidebar.service';
import {AdminBaseComponent} from '../admin-base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-customapi-service-namespace',
    providers: [CustomAPIService],
    templateUrl: './admin-customapi-service-namespace.component.html'
})

export class AdminCustomAPIServiceNamespaceComponent extends AdminBaseComponent implements OnInit {
    serviceId: number;
    public showEditor: boolean = false;
    public showDelete: boolean = false;
    public fields: ApiNamespace[] = [];
    public selected: ApiNamespace = null;
    theDeleteCallback: Function;
    private sub: any;
    public service: ApiService = null;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        protected customAPIService: CustomAPIService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        secondaryNavService: SecondaryNavService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        titleService: Title
    ) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.areaName = "Custom API";
        this.theDeleteCallback = this.deleteItem.bind(this);
    }

    ngOnInit(): void {
        this.sub = this
            .route
            .params
            .subscribe(
                params => {
                    this.serviceId = +params['serviceId']; // (+) converts string 'id' to a number
                    this.isLoading = true;

                    this.customAPIService.getService(this.serviceId).subscribe(
                        res => {
                            this.service = res;

                            this.load();

                            this.headerBreadcrumbService.clearBreadcrumbs();

                            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Administration'));
                            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Custom API', '/admin/customapi'));
                            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(`${this.service.Name}`));
                        });
                });
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['serviceId']) {
            this.load();
        }
    }

    private load(): void {
        this.isLoading = true;
        this.customAPIService.getNamespaces(this.serviceId).subscribe(
            res => {
                this.fields = res;

                this.isLoading = false;
            });
    }

    private saveField(data): void {
        this.customAPIService.saveNamespace(data.item).subscribe(
            res => {
                this.showMessageForResult(this.messagesService, res);
                this.load();

                this.showEditor = false;
            }
        );
    }

    deleteItem(id: number) {
        this.customAPIService.deleteNamespace(id).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.load();

                this.showDelete = false;
            }
        );
    }
}
