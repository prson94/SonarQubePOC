import { Component, ChangeDetectionStrategy, ChangeDetectorRef, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { AuthenticationService } from '../../../services/authentication.service';
import { Subscription } from 'rxjs';
import { CompanySettingsService } from '../../../services/settings.service';
import { CompanySettingEnum } from '../../../models/settings.model';

declare var CurrentResourceID;
declare var CurrentResourceUid;
declare var SingleSignOn;
declare var ResourceName;
declare var ResourceEmail;

@Component({
    selector: 'd3s-header-profile',
    templateUrl: 'header-profile.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderProfileComponent implements OnInit, OnDestroy {

    public active: boolean = false;
    public resourceId: number = CurrentResourceID;
    public resourceUid: string = CurrentResourceUid;
    public singleSignOn: boolean = SingleSignOn;
    public userName: string = ResourceName;
    public userEmail: string = ResourceEmail;
    showAllUsersAPIKey: boolean = false;
    isApiKeysPopupVisible: boolean = false;
    private isAdminSub: Subscription;
    constructor(
        private router: Router,
        private ref: ChangeDetectorRef,
        private authenticationService: AuthenticationService,
        protected settingsService: CompanySettingsService
    ) { }

    ngOnInit() {
        let showApiKey = this.settingsService.getSettingById(CompanySettingEnum.ShowAllUsersAPIKey).BooleanSetting.Value;

        this.isAdminSub = this.authenticationService.isAdmin$.subscribe(x => {
            let isAdmin: boolean = x;
            this.showAllUsersAPIKey = isAdmin || showApiKey;
        });

        if (this.authenticationService.isAdmin || showApiKey) {
            this.showAllUsersAPIKey = true;
        }
    }

    ngOnDestroy(): void {
        if (this.isAdminSub) {
            this.isAdminSub.unsubscribe();
        }
    }

    public signOut() {
        window.location.href = '/slo';
    }

    public viewProfile() {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('Resource', this.resourceId));
    }

    show(item) {
        let menuPanel = item.children[1].nextElementSibling;
        let minimizedMenuItem = item.children[0].nextElementSibling;
        let dims = minimizedMenuItem.getBoundingClientRect();
        if (menuPanel) {
            this.active = true;

            menuPanel.style.zIndex = 1000;
            menuPanel.style.top = 40 + 'px'; // -1 for the border so it blends
            menuPanel.style.right = (dims.width) + 'px';
            menuPanel.style.position = 'fixed';
            if (dims.width > 0) {
                menuPanel.style.top = dims.top + 'px';
                menuPanel.style['border-right'] = 'none';
            }
        }
    }

    hide(item) {
        this.active = false;
        this.ref.markForCheck();
    }

}

