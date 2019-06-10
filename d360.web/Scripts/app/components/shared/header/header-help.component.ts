import { Component, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CurrentEnvironmentSettings } from '../../../static/environment-settings';
declare var __BUILD_DATE: string;
declare var VersionNumber: string;

@Component({
    selector: 'd3s-header-help',
    template: ` <span #item class="header-search header-table" [ngClass]="{'header-search-active':active}" (mouseenter)="show(item)" (mouseleave)="hide(item)">
                    <div class="header-button"><i class="fa fa-question-circle"></i></div>
                    <div class="header-help search-child header-profile-panel">
                       <ul>
                            <li class="header-item"><div class="mini-menu-line"><div class="text"><a target="_blank" [href]="userGuide">User Guide</a></div></div></li>
                            <li class="header-item"><div class="mini-menu-line"><div class="text"><a target="_blank" [href]="adminGuide">Admin Guide</a></div></div></li>
                            <li class="header-item"><div class="mini-menu-line"><div class="text"><a target="_blank" [href]="whatIsNew">What's New</a></div></div></li>
                            <li class="header-item"><div class="mini-menu-line"><div class="text"><a target="_blank" [href]="community">Community</a></div></div></li>
							<li class="header-item"><div class="mini-menu-line"><div class="text"><a target="_blank" (click)="popup()" >About Data3Sixty</a></div></div></li>
                       </ul>
                    </div>
                <span>
                <p-dialog header="About" [(visible)]="display" [responsive]="true" [width]="700" [minWidth]="100" [minY]="70">
                        <p-header><img src="../../../../../Content/images/logo.new.color.png"></p-header>         
                        <span><b>Build Version:</b> {{this.versionNumber}}
                              <br /><b>Build Date:</b> {{this.buildDate}}
                              <br /><b>Support:</b> http://support.infogix.com
                              <p>© 2005-2019 Infogix. All rights reserved.<br />Confidential - Limited distribution to authoroized persons only, pursuant to the teams of Infogix Inc. license agreement. This software is protected as an unpublished work and constitutes a trade secret of Infogix Inc.</span>
                    <p-footer><button type="button" style="background-color: #1E90FF;color: white;" (click)="display=false">Close</button></p-footer>
                </p-dialog>`,
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
    buildDate: string = __BUILD_DATE;
    versionNumber: string = VersionNumber;

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