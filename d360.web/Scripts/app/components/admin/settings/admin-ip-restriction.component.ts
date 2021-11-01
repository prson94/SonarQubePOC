import { Component, Input, Output, EventEmitter } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettings, IpRestriction } from '../../../models/settings.model';
import { SiteNav } from '../../../models/site-menu.model';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-ip-restriction',
    template: `
    <d3s-loading [isLoading]="isLoading"></d3s-loading>
    <div *ngIf="!isLoading">
        <header>
            IP Restrictions
            <d3s-tile-actions [hasAdd]="true" (addClick)="addIpRestriction()"></d3s-tile-actions>
        </header>
        <div class="directions">
            Restrictions will only apply if there are entries present.  If no entries are present, then users will be allowed to access this environment from any IP.
        </div>
        <div>
            <table class="responsive-table">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Start Range</th>
                        <th>End Range</th>
                        <th></th>
                    </tr>
                </thead>
                <tbody>
                    <tr *ngFor="let ipRestriction of companySettings.IpRestrictions;let i=index" style="margin-bottom: 10px" class="fadeIn">
                        <td style="vertical-align: middle">
                            <input [ngModel]="companySettings.IpRestrictions[i].Name" (ngModelChange)="companySettings.IpRestrictions[i].Name = $event; companySettingsChange.emit(companySettings)" type="text" />
                        </td>
                        <td style="vertical-align: middle">
                            <input [ngModel]="companySettings.IpRestrictions[i].Start" (ngModelChange)="companySettings.IpRestrictions[i].Start = $event; companySettingsChange.emit(companySettings)" type="text" />
                        </td>
                        <td style="vertical-align: middle">
                            <input [(ngModel)]="companySettings.IpRestrictions[i].End" (ngModelChange)="companySettings.IpRestrictions[i].End = $event; companySettingsChange.emit(companySettings)" type="text" />
                        </td>
                        <td style="width: 20px; text-align: right; vertical-align: top">
                            <a style="padding: 0 .5rem;" class="btn waves-effect waves-red btn-flat" title="Remove this restriction" (click)="removeIpRestriction(i)"><i class="fa fa-trash"></i></a>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>
`,
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
