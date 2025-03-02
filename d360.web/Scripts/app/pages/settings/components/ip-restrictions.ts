import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CompanySettings, IpRestriction } from '../../../models/settings.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { LoadingComponent } from '../../../_shared/components/loading';
import { TilesModule } from '../../../components/shared/tiles/tiles.module';
import { FormsModule } from '@angular/forms';
import { BaseComponent } from '../../../components/shared/base.component';

@Component({
    selector: 'ip-restrictions',
	templateUrl: './ip-restrictions.html',
	standalone: true,
	imports: [FormsModule, LoadingComponent, TilesModule],
    styles: [
        `
        .remove {
            cursor: pointer; 
            color: maroon; 
            font-size: 1.5em;
            vertical-align: middle;
        }
        input[type=text] {
            width: 90%;
            height:25px;
        }
        `
    ]
})
export class IpRestrictions extends BaseComponent {
    @Input() companySettings: CompanySettings;
    @Output() companySettingsChange = new EventEmitter();

    constructor(
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
    }

    addIpRestriction(): void {
        this.companySettings.IpRestrictions.push(new IpRestriction());
        this.companySettingsChange.emit(this.companySettings);
    }

	removeIpRestriction(i: number): void {
        this.companySettings.IpRestrictions.splice(i, 1);
        this.companySettingsChange.emit(this.companySettings);
    }
}
