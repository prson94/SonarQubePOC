import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
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
	public resourceId: number = typeof CurrentResourceID === "undefined" ? -1 : CurrentResourceID;
	public resourceUid: string = typeof CurrentResourceUid === "undefined" ? "" : CurrentResourceUid;
	public singleSignOn: boolean = typeof SingleSignOn === "undefined" ? true : SingleSignOn;
	public userName: string = typeof ResourceName === "undefined" ? "" : ResourceName;
    public userEmail: string = typeof ResourceEmail === "undefined" ? "" : ResourceEmail;
    showAllUsersAPIKey: boolean = false;
	isApiKeysPopupVisible: boolean = false;
	isLanguageSettingModalVisible = false;

    private isAdminSub: Subscription;
    constructor(
        private router: Router,
		private ref: ChangeDetectorRef,
        private authenticationService: AuthenticationService,
        protected settingsService: CompanySettingsService
    ) { }

	ngOnInit() {
		if (this.resourceId === -1) {
			this.settingsService.getUserVariables().subscribe((res) => {
				this.resourceId = res.CurrentResourceID;
				this.resourceUid = res.CurrentResourceUid;
				this.singleSignOn = res.SingleSignOn;
				this.userName = res.ResourceName;
				this.userEmail = res.ResourceEmail;
				this.ref.markForCheck();
			});
		}

        const showApiKey = this.settingsService.getSettingById(CompanySettingEnum.ShowAllUsersAPIKey).BooleanSetting.Value;

        this.isAdminSub = this.authenticationService.isAdmin$.subscribe((x) => {
            const isAdmin: boolean = x;
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
		this.router.navigateByUrl(SiteUrlHelpers.federateUrl(SiteUrlHelpers.getUserUrl(CurrentResourceUid)));
    }

    show(item) {
        const menuPanel = item.children[1].nextElementSibling;
        const minimizedMenuItem = item.children[0].nextElementSibling;
        const dims = minimizedMenuItem.getBoundingClientRect();
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

