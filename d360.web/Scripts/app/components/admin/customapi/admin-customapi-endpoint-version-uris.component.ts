import { Component, Input, OnInit, SimpleChange } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiUri, ApiVersion } from '../../../models/custom-api.model';
import { CustomAPIService } from '../../../services/custom-api.service';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-api-endpoint-version-uritypes',
    providers: [CustomAPIService],
    templateUrl: './admin-customapi-endpoint-version-uris.component.html'
})

export class AdminCustomAPIEndpointVersionUriTypesComponent extends BaseComponent implements OnInit {
    @Input() version: ApiVersion;
    public showEditor: boolean = false;
    public uris: ApiUri[] = [];
    public selected: ApiUri = null;
    public showDelete: boolean = false;
    theDeleteCallback: Function;

    searchText = $localize`Search...`;

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

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if ((changes['version'] || this.version != null)) {
            this.load();
        }
    }

    private load(): void {
        this.isLoading = true;
        this.customAPIService.getEndpointVersionUris(this.version.ID).subscribe(
            res => {
                this.uris = res;
                this.isLoading = false;
            });
    }

    private saveUri(data): void {
        this.customAPIService.saveEndpointUri(data.item).subscribe(
            res => {
                this.showMessageForResult(this.messagesService, res);
                this.load();
                this.showEditor = false;
            }
        );
    }

    deleteService(id: number) {
        this.customAPIService.deleteEndpointUri(id).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                this.load();
            }
        );
    }

    get deleteModalTitle(): string {
        return $localize`Are you sure you want to delete the uri [${this.selected?.Format}]?`;
    }
}
