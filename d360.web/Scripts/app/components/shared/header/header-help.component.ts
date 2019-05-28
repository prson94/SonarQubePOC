import { Component, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CurrentEnvironmentSettings } from '../../../static/environment-settings';

@Component({
    selector: 'd3s-header-help',
    template: ` <span #item class="header-search header-table" [ngClass]="{'header-search-active':active}" (mouseenter)="show(item)" (mouseleave)="hide(item)">
                    <a class="photo"><i class="fa fa-question-circle"></i></a>
                    <div class="header-help search-child header-search-panel">
                       <ul>
                            <li class="header-help-li"><a target="_blank" [href]="userGuide">User Guide</a></li>
                            <li class="header-help-li"><a target="_blank" [href]="adminGuide">Admin Guide</a></li>
                            <li class="header-help-li"><a target="_blank" [href]="whatIsNew">What's New</a></li>
                            <li class="header-help-li"><a target="_blank" [href]="community">Community</a></li>
                       </ul>
                    </div>
                <span>`,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderHelpComponent {
    public active: boolean = false;
    private hideHandle: number = 0;

    public userGuide = CurrentEnvironmentSettings.HelpBaseUri + "Default.htm#c-user-guide/user-guide.htm%3FTocPath%3DUser%2520guide%7C_____0";
    public adminGuide = CurrentEnvironmentSettings.HelpBaseUri + "Default.htm#d-admin/admin-intro.htm%3FTocPath%3DAdministration%2520guide%7C_____0";
    public whatIsNew = CurrentEnvironmentSettings.HelpBaseUri + "Default.htm#b-release-notes/whats-new.htm%3FTocPath%3DWhat";
    public community = "https://support.infogix.com/hc/en-us/community/topics/360000029388-Data3Sixty-Govern"; 

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