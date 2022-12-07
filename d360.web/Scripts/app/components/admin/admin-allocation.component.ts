import { Component, Input } from '@angular/core';
import { CompanySettingsService } from '../../services/settings.service';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-admin-allocation',
    providers: [],
    templateUrl: 'admin-allocation.component.html'
})

export class AdminAllocationComponent extends BaseComponent {
    @Input() objectID: number;
    @Input() objectType: string;

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    public rows = [0];

    public allocations: any[] = [{ Name: $localize`Grammatic Type Allocation` }];
}