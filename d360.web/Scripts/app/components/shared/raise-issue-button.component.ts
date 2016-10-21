import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-raise-issue-button',
    template: `           
        <button type="button"  class="issue-button" (click)="raiseIssue()">Take Action</button>
        `,    
    styles: [`
        :host{
            float:right;
        }
    `]
})

export class RaiseIssueButtonComponent extends BaseComponent {
    
    constructor(private router: Router) {
        super();
    }

    private raiseIssue() {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_RAISE_ISSUE}`);
    }
    
}