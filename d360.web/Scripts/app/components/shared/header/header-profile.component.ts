import { Component, ChangeDetectionStrategy, ChangeDetectorRef, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { AuthenticationService } from '../../../services/authentication.service';
import { Subscription } from 'rxjs';

declare var CurrentResourceID;
declare var SingleSignOn;
declare var ResourceName;
declare var ResourceEmail;
declare var CompanySettings;

@Component({
    selector: 'd3s-header-profile',
    template: ` <span #item style="display:table;" class="header-search" [ngClass]="{'header-search-active':active}" (mouseenter)="show(item)" (mouseleave)="hide(item)" >
                    <a [routerLink]="resourceUrl()" class="photo" title="Go to your profile"><img [src]="'/resources/image/' + resourceId + '?size=25'" height="25" width="25" /></a>
                    <div class="search-child header-profile-panel">                        
                        <div class="row">           
                            <div class="col s2"><a [routerLink]="resourceUrl()" class="photo" title="Go to your profile"><img [src]="'/resources/image/' + resourceId + '?size=25'" height="25" width="25" /></a></div>
                            <div class="col s10">
                                <div class="row">
                                    <div class="col s12"><h4>{{userName}}</h4></div>
                                    <div class="col s12"><h5>{{userEmail}}</h5></div>
                                </div>
                            </div>                                                        
                        </div>
                        <div class="row">
                                <div class="col s12" *ngIf="!singleSignOn">&nbsp;</div>
                                <div class="col s12" *ngIf="!singleSignOn"><a [routerLink]="'/resource/'+resourceId+'/changepassword'"><i class="fa fa-pencil" aria-hidden="true"></i>&nbsp;Change Password</a></div>
                                <div class="col s12">&nbsp;</div>
                                <div class="col s12"  *ngIf="showAllUsersAPIKey"><a [routerLink]="'/resource/my/apikey'"><i class="fa fa-key" aria-hidden="true"></i>&nbsp;API Key</a></div>
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

