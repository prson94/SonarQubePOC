import { Component, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CurrentEnvironmentSettings } from '../../../static/environment-settings';
//import { CurrentEnvironmentSettings } from '../../../../../Content/images/';


@Component({
    selector: 'd3s-header-help',
    template: ` <span #item class="header-search header-table" [ngClass]="{'header-search-active':active}" (mouseenter)="show(item)" (mouseleave)="hide(item)">
                    <div class="header-button"><i class="fa fa-question-circle"></i></div>
                    <div class="header-help search-child header-search-panel">
                       <ul>
                            <li class="header-help-li"><a target="_blank" [href]="userGuide">User Guide</a></li>
                            <li class="header-help-li"><a target="_blank" [href]="adminGuide">Admin Guide</a></li>
                            <li class="header-help-li"><a target="_blank" [href]="whatIsNew">What's New</a></li>
                            <li class="header-help-li"><a target="_blank" [href]="community">Community</a></li>
                            <li class="header-help-li"><a target="_blank" (click)="popup()">About Data3Sixty</a></li>
                       </ul>
                    </div>
                <span>
                <div><p-dialog header="About" [(visible)]="display" [responsive]="true" [width]="700" [height]="350" [baseZIndex]="10000" img src="../../../../../Content/images/logo.new.color.png">
                        <div><b>Data3Sixty Govern v2019.4.0</b>
                              <br><b>Build Version:</b> 2019.4.5.0
                              <br><b>Build Date:</b> 24-04-2019 7:17:27 PM</div>
                              <div><b>Support:</b> http://support.infogix.com</div>
                              <p>© 2005-2019 Infogix. All rights reserved.<br>
                              Confidential - Limited distribution to authoroized persons only, pursuant to the teams of Infogix Inc. license agreement. This software is protected
                              as an unpublished work and constitutes a trade secret of Infogix Inc.</p>
                </p-dialog></div>`,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderHelpComponent {
    public active: boolean = false;
    private hideHandle: number = 0;
    display: boolean = false;

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

    popup() {
        this.display = true;
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