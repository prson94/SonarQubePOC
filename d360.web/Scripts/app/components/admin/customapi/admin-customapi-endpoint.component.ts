import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {ApiEndpoint, ApiService} from '../../../models/custom-api.model';
import {CustomAPIService} from '../../../services/custom-api.service';
import {BaseComponent} from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-api-endpoints',
    providers: [CustomAPIService],
    templateUrl: './admin-customapi-endpoint.component.html'
})

export class AdminCustomAPIEndpointsComponent extends BaseComponent implements OnInit {
    @Input() service: ApiService;
    public showEditor: boolean = false;
    public endpoints: ApiEndpoint[] = [];
    public selected: ApiEndpoint = null;
    public showDelete: boolean = false;

    theDeleteCallback: Function;

    @Input() numberOfEndpoints: number = 0;
    @Output() numberOfEndpointsChange = new EventEmitter();

    constructor(
        protected customAPIService: CustomAPIService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private route: ActivatedRoute,
        private router: Router,
    ) {
        super(settingsService);
        this.theDeleteCallback = this.deleteService.bind(this);
    }

    ngOnInit(): void {
        this.load();
    }

    private load(): void {
        this.isLoading = true;

        this.customAPIService.getEndpoints(this.service.ID).subscribe(
            res => {
                this.isLoading = false;
                this.endpoints = res;
                this.numberOfEndpoints = this.endpoints.length;
                this.numberOfEndpointsChange.emit(this.numberOfEndpoints);
            }
        );
    }

    private saveEndpoint(data): void {
        this.customAPIService.saveEndpoint(data.item).subscribe(
            res => {
                this.showMessageForResult(this.messagesService, res);
                this.load();
                this.showEditor = false;
            }
        );
    }

    public showEndpoint(item: ApiEndpoint): void {
        this.router.navigateByUrl(`admin/customapi/${this.service.ID}/details/${item.ID}/details`);
    }

    deleteService(id: number) {
        this.customAPIService.deleteEndpoint(id).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.load();
                this.showDelete = false;
            }
        );
    }
}
