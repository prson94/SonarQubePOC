import { Component, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

@Component({
    selector: 'd3s-header-help',
    template: ` <span #item style="display:table;" class="header-search" [ngClass]="{'header-search-active':active}" (mouseenter)="show(item)" (mouseleave)="hide(item)">
                    <a><i class="fa fa-question-circle" style="color:#d32f2f"></i></a>
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

    private userGuide = SiteUrlHelpers.SITE_URL_STATIC_USER_GUIDE;
    private adminGuide = SiteUrlHelpers.SITE_URL_STATIC_ADMIN_GUIDE;
    private whatIsNew = SiteUrlHelpers.SITE_URL_STATIC_WHAT_IS_NEW;

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

