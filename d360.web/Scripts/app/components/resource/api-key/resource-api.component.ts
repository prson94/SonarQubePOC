import { Component, Input, Output, EventEmitter, ChangeDetectorRef } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { ResourceAPICredentials } from '../../../models/resource.model';
import { ResourcesService } from '../../../services/resources.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { CompanySettingsService } from '../../../services/settings.service';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-resource-api',
    templateUrl: 'resource-api.component.html',
    providers: [ResourcesService],
    styleUrls: ['resource-api.component.less']
})
export class ResourceApiComponent extends BaseComponent {
    @Input() isVisible = false;
    @Output() onClose = new EventEmitter();

    private resource: ResourceAPICredentials;

    isSaving = false;

    constructor(
        private resourcesService: ResourcesService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
    }

    ngOnInit() {
        this.isLoading = true;
        this.secondaryNavService.showHeader(false);
        this.resourcesService.getApiKeys()
            .subscribe(r => {
                this.resource = r;
                this.isLoading = false;
            });
    }

    regenerateKeys() {
        this.isSaving = true;
        this.resourcesService.regenerateApiKeys(this.resource)
            .subscribe((res) => {
                this.resource = res;
                this.isSaving = false;
                this.cdRef.markForCheck();
            });
    }
}
