import { Component, EventEmitter, Input, Output } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettings, IpRestriction } from '../../../models/settings.model';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-ip-restriction',
    templateUrl: './admin-ip-restriction.component.html',
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

export class AdminIpRestrictionComponent extends AdminBaseComponent {
    @Input() companySettings: CompanySettings;
    @Output() companySettingsChange = new EventEmitter();

    constructor(
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
    ) {
        super(headerBreadcrumbService, titleService, settingsService);
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
