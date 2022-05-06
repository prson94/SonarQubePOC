import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiEndpoint, ApiVersion } from '../../../models/custom-api.model';
import { CustomAPIService } from '../../../services/custom-api.service';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-api-endpoint-versions',
    providers: [CustomAPIService],
    templateUrl: './admin-customapi-endpoint-version.component.html'
})

export class AdminCustomAPIEndpointVersionsComponent extends BaseComponent implements OnInit {
    @Input() endpoint: ApiEndpoint;
    public showEditor: boolean = false;
    public versions: ApiVersion[] = [];
    public showDelete: boolean = false;
    theDeleteCallback: Function;
    @Input() selected: ApiVersion = null;
    @Output() selectedChange = new EventEmitter();

    @Input() numberOfVersions: number = 0;
    @Output() numberOfVersionsChange = new EventEmitter();

    searchText = $localize`Search...`;
    editorTitle = $localize`Version`;

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

        this.customAPIService.getEndpointVersions(this.endpoint.ID).subscribe(
            res => {
                this.versions = res;

                if (this.versions && this.versions.length > 0) {
                    this.selected = this.versions[0];
                    this.selectedChange.emit(this.selected);
                }

                this.numberOfVersions = (res != null && res.length > 0) ? res.length : 0;
                this.numberOfVersionsChange.emit(this.numberOfVersions);

                this.isLoading = false;
            }
        );
    }

    private saveVersion(data): void {
        this.customAPIService.saveVersion(data.item).subscribe(
            res => {
                this.showMessageForResult(this.messagesService, res);
                this.load();

                this.showEditor = false;
            }
        );
    }

    deleteService(id: number) {
        this.customAPIService.deleteEndpointVersion(id).subscribe(
            (result) => {
                this.showMessageForResult(this.messagesService, result);
                this.load();

                this.showDelete = false;
            }
        );
    }

    get deleteModalTitle(): string {
        return $localize`Are you sure you want to delete the version [${this.selected?.UriPrefix}]?`;
    }
}
