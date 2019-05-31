import { Component, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { CurrentEnvironmentSettings } from '../../../static/environment-settings';

@Component({
    selector: 'd3s-header-help',
    template: ` <span #item class="header-search header-table" [ngClass]="{'header-search-active':active}" (mouseenter)="show(item)" (mouseleave)="hide(item)">
                    <div class="header-button"><i class="fa fa-question-circle"></i></div>
                    <div class="header-help search-child header-search-panel">
                       <ul>
                            <li class="header-help-li"><a target="_blank" [href]="userGuide">User Guide</a></li>
                            <li class="header-help-li"><a target="_blank" [href]="adminGuide">Admin Guide</a></li>
                            <li class="header-help-li"><a target="_blank" [href]="whatIsNew">What's New</a></li>
                       </ul>
                    </div>
                <span>`,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderHelpComponent {
    public active: boolean = false;
    private hideHandle: number = 0;

    private userGuide = CurrentEnvironmentSettings.HelpBaseUri + "Default.htm#c-user-guide/user-guide.htm%3FTocPath%3DUser%2520guide%7C_____0";
    private adminGuide = CurrentEnvironmentSettings.HelpBaseUri + "Default.htm#d-admin/admin-intro.htm%3FTocPath%3DAdministration%2520guide%7C_____0";
    private whatIsNew = CurrentEnvironmentSettings.HelpBaseUri + "Default.htm#b-release-notes/whats-new.htm%3FTocPath%3DWhat";

    constructor(
        private ref: ChangeDetectorRef
    ) { }

    show(item) {
        // check for any pending hides and cancel them
        if (this.hideHandle > 0) {
            window.clearTimeout(this.hideHandle);
            this.hideHandle = 0;
        }
        let panel = item.children[0].nextElementSibling;
        if (panel) {
            this.active = true;

            panel.style.zIndex = 1000;

            panel.style.top = (item.offsetHeight - 1) + 'px'; // -1 for the border so it blends
            panel.style.right = '0px';
        }
    }

    hide(item) {
        if (this.hideHandle > 0) return; //pending hide ignore new request
        //queue up a request to hide the window.
        this.hideHandle = window.setTimeout(() => {
            this.active = false;
            this.ref.markForCheck();
        },
            500);
    }
}

