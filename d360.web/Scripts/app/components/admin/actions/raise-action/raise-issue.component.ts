import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Router } from '@angular/router';
import { CompanySettingsService } from '../../../../services/settings.service';
import { SiteUrlHelpers } from '../../../../static/site-url-helpers';
import { BaseComponent } from '../../../shared/base.component';


@Component({
    selector: 'd3s-raise-issue',
    template: `           
        <button type="button" igButton class="ig-button-accent" (click)="raiseIssue()" i18n>Take Action</button>
        `,
    styles: [`
        :host{
            float:right;
        }
    `],
    changeDetection: ChangeDetectionStrategy.OnPush,
})

export class RaiseIssueComponent extends BaseComponent {

    constructor(
        protected settingsService: CompanySettingsService,
        private router: Router) {
        super(settingsService);
    }

    public raiseIssue() {
		this.router.navigateByUrl(this.federateUrl(`${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_RAISE_ISSUE}`));
    }
}