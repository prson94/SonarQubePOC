import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../base.component';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-raise-issue-button',
    template: `           
        <button type="button"  class="issue-button" (click)="raiseIssue()">Take Action</button>
        `,    
    styles: [`
        :host{
            float:right;
        }
    `],
    changeDetection: ChangeDetectionStrategy.OnPush,
})

export class RaiseIssueButtonComponent extends BaseComponent {
    
    constructor(
        protected settingsService: CompanySettingsService,
        private router: Router) {
        super(settingsService);
    }

    public raiseIssue() {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_RAISE_ISSUE}`);
    }    
}