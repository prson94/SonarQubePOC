import { Component, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { CurrentEnvironmentSettings } from '../../../static/environment-settings';

@Component({
    selector: 'd3s-header-help',
    template: ` <span #item style="display:table;" class="header-search" [ngClass]="{'header-search-active':active}" (mouseenter)="show(item)" (mouseleave)="hide(item)">
                    <a class="photo hide-on-med-and-down"><i class="fa fa-question-circle"></i></a>
                    <div class="show-on-medium-and-down hide-on-med-and-up">Help <i class="fa fa-caret-right"></i></div>
                    <div class="search-child header-search-panel" style="background-color: white; padding: 0; width: 175px">
                       <ul>
                            <li style="width:100%;padding:10px;display:inline-block"><a target="_blank" [href]="userGuide">User Guide</a></li>
                            <li style="width:100%;padding:10px;display:inline-block"><a target="_blank" [href]="adminGuide">Admin Guide</a></li>
                            <li style="width:100%;padding:10px;display:inline-block"><a target="_blank" [href]="whatIsNew">What's New</a></li>
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
        let menuPanel = item.children[1].nextElementSibling;
        let minimizedMenuItem = item.children[0].nextElementSibling;
        let dims = minimizedMenuItem.getBoundingClientRect();
        if (menuPanel) {
            this.active = true;

            menuPanel.style.zIndex = 1000;

            menuPanel.style.top = (item.offsetHeight - 1) + 'px'; // -1 for the border so it blends
            menuPanel.style.right = (dims.width + 11) + 'px';
            if (dims.width > 0) {
                menuPanel.style.top = '-10px';
                menuPanel.style['border-right'] = 'none';
                menuPanel.style.right = (dims.width + 10) + 'px';
                menuPanel.style['border-top'] = '1px solid #54a4da ';
            }

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

