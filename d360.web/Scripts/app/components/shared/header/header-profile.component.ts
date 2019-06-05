import { Component, ChangeDetectionStrategy, ChangeDetectorRef, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { AuthenticationService } from '../../../services/authentication.service';
import { Subscription } from 'rxjs';
import { retry } from 'rxjs/operators';

declare var CurrentResourceID;
declare var SingleSignOn;
declare var ResourceName;
declare var ResourceEmail;
declare var CompanySettings;

@Component({
    selector: 'd3s-header-profile',
    template: ` <span #item class="header-search header-table" [ngClass]="{'header-search-active':active}" (mouseenter)="show(item)" (mouseleave)="hide(item)" >
                    <a class="photo hide-on-med-and-down"><img [src]="'/resources/image/' + resourceId + '?size=25'" height="25" width="25" /></a>
                    <div class="show-on-medium-and-down hide-on-med-and-up">
                        <div class="mini-menu-line">
                            <div class="check-gutter"></div><div class="text">My Account</div><div class="expand-gutter"><i class="fa fa-caret-right"></i></div>
                        </div>
                    </div>
                    <div class="search-child header-profile-panel">                        
                        <div class="row">          
                            <ul>
                                <li class="header-item label">
                                    <div class="mini-menu-line">
                                        <div class="text">
                                            <span>
                                                {{userName}} <br>
                                                {{userEmail}}
                                            </span>
                                        </div>
                                    </div>
                                </li>
                                <li [routerLink]="resourceUrl()" class="header-item">
                                    <div class="mini-menu-line">
                                        <div class="text">View Profile</div>
                                    </div>
                                </li>
                                <li *ngIf="showAllUsersAPIKey" [routerLink]="'/resource/my/apikey'" class="header-item">
                                    <div class="mini-menu-line">
                                        <div class="text">API Key</div>
                                    </div>                                
                                </li>
                                <li *ngIf="!singleSignOn"  [routerLink]="'/resource/'+resourceId+'/changepassword'" class="header-item">
                                    <div class="mini-menu-line">
                                        <div class="text">Change Password</div>
                                    </div>                                
                                </li>
                                <li class="header-item" (click)="signOut()">
                                    <div class="mini-menu-line">
                                        <div class="text">Sign Out</div>
                                    </div>                                
                                </li>
                            </ul>                                                    
                        </div>
                    </div>
                <span>`,    
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderProfileComponent implements OnInit , OnDestroy{    

    public active: boolean = false;
    private hideHandle: number = 0;
    public resourceId: number = CurrentResourceID;
    public singleSignOn: boolean = SingleSignOn;
    public userName: string = ResourceName;
    public userEmail: string = ResourceEmail;
    private showAllUsersAPIKey: boolean = false;
    private isAdminSub: Subscription;
    constructor(
        private router: Router,        
        private ref: ChangeDetectorRef,
        private authenticationService: AuthenticationService
    ) { }
    
    ngOnInit() {

        this.isAdminSub = this.authenticationService.isAdmin$.subscribe(x => {
            let isAdmin: boolean = x; 
            if (isAdmin || (CompanySettings != null && CompanySettings.ShowAllUsersAPIKey != null && CompanySettings.ShowAllUsersAPIKey.toString() == 'true'))
                this.showAllUsersAPIKey = true;
            else
                this.showAllUsersAPIKey = false;
        });
       
        if (this.authenticationService.isAdmin || (CompanySettings != null && CompanySettings.ShowAllUsersAPIKey != null && CompanySettings.ShowAllUsersAPIKey.toString() == 'true'))
            this.showAllUsersAPIKey = true;
    }

    ngOnDestroy(): void {
        this.isAdminSub.unsubscribe();
    }
    public resourceUrl() {
        return SiteUrlHelpers.getObjectUrl('Resource', this.resourceId);
    }
    public signOut() {
        window.location.href = '/slo';
    }

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
            menuPanel.style.right = (dims.width) + 'px';
            if (dims.width > 0) {
                menuPanel.style.top = '0px';
                menuPanel.style['border-right'] = 'none';
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

