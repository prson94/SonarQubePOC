import { Component, Input, OnChanges, OnInit, SimpleChange } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiField, ApiVersion } from '../../../models/custom-api.model';
import { CustomAPIService } from '../../../services/custom-api.service';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-api-endpoint-version-fields',
    providers: [CustomAPIService],
    templateUrl: './admin-customapi-endpoint-version-fields.component.html'
})

export class AdminCustomAPIEndpointVersionFieldsComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() version: ApiVersion;
    public showEditor: boolean = false;
    public showDelete: boolean = false;
    public fields: ApiField[] = [];
    public selected: ApiField = null;
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
        this.theDeleteCallback = this.deleteItem.bind(this);
    }

    ngOnInit(): void {
        this.load();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if ((changes['version'] && this.version != null)) {
            this.load();
        }
    }

    private load(): void {
        this.isLoading = true;
        this.customAPIService.getEndpointVersionFields(this.version.ID).subscribe(
            res => {
                this.fields = res;
                this.isLoading = false;
            }
        );
    }

    private saveField(data): void {
        data.item.EntityID = this.version.EntityID;

        this.customAPIService.saveField(data.item).subscribe(
            res => {
                this.showMessageForResult(this.messagesService, res);
                this.load();
                this.showEditor = false;
            }
        );
    }

    deleteItem(id: number) {
        this.customAPIService.deleteField(id).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                this.load();
            }
        );
    }

    get deleteModalTitle(): string {
        return `Are you sure you want to delete the  field [${this.selected?.Name}]?`;
    }
}
