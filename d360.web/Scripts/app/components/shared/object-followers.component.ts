
import {Component, Input, Output, EventEmitter} from '@angular/core';
import { CompanySettingsService } from '../../services/settings.service';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-object-followers',
    template: `
            <div (click)="toggleDetails()" >
                <header i18n>Followers</header>
                <span class="governance-value">{{followerCount}}</span>
            </div>            
        `
})

export class ObjectFollowersComponent extends BaseComponent {
    @Input() followerCount: number = 0;

    @Input() showDetails: boolean = false;
    @Output() showDetailsChange = new EventEmitter();

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    toggleDetails() {
        this.showDetails = !this.showDetails;
        this.showDetailsChange.emit(this.showDetails);
    }
}