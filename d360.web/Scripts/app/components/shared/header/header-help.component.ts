import { Component, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, ElementRef, OnInit, HostListener } from "@angular/core";
import { CurrentEnvironmentSettings } from "../../../static/environment-settings";
import { CompanySettingsService } from "../../../services/settings.service";
import { ResourcesService } from "../../../services/resources.service";
import { HelpResource } from "../../../models/resource.model";
import { Observable } from "rxjs";
import { environment } from '../../../../environments/environment';

@Component({
    selector: 'd3s-header-help',
    template: ` <span #item class="header-search header-table" [ngClass]="{'header-search-active':active}" (mouseenter)="show(item)" (mouseleave)="hide(item)">
                    <div class="header-button"><i class="fa fa-question-circle"></i></div>
                    <div class="header-help search-child header-profile-panel">
                       <ul>       
                            <ng-container *ngIf="(customHelpResources$ | async) as list">
                                <li class="header-item" *ngFor="let help of list"><div class="mini-menu-line"><div class="text"><a target="_blank" [href]="help.Url">{{help.Name}}</a></div></div></li>                                
                                <li class="header-item" *ngIf="list?.length == 0"><div class="mini-menu-line"><div class="text"><a target="_blank" [href]="userGuide">User Guide</a></div></div></li>
                                <li class="header-item" *ngIf="list?.length == 0"><div class="mini-menu-line"><div class="text"><a target="_blank" [href]="adminGuide">Admin Guide</a></div></div></li>
                            </ng-container>                            
                            <li class="header-item"><div class="mini-menu-line"><div class="text"><a target="_blank" [href]="whatIsNew">What's New</a></div></div></li>
                            <li class="header-item"><div class="mini-menu-line"><div class="text"><a target="_blank" [href]="community">Community</a></div></div></li>
                            <li class="header-item"><div class="mini-menu-line"><div class="text"><a target="_blank" (click)="showAbout()">About Data360 Govern</a></div></div></li>
                       </ul>
                    </div>
                    <d3s-modal #popupBox [title]="'About Data360 Govern'" 
                                         additionalClasses="about medium-dialog" 
                                         (onClose)="closeAbout()" 
                                         [isVisible]="isModalVisible"
                                         (keydown)="checkKey($event)"
                                         tabindex=-1 >
                        <div class="content">
                            <div class="flex row">
                                <img class="about-image" src="../../../../../Content/images/aboutLogo.png"/>
                                <div class="about-info">
                                    <ul>
                                        <li><b>Build Version:</b> {{environment.version}}</li>
                                        <li><b>Build Date:</b> {{environment.timeStamp | date:'short'}}</li>
                                        <li><b>Support:</b> <a href="http://support.infogix.com" target="_blank">http://support.infogix.com</a></li>
                                        <li><a class="thirdPartyLicence" href="/Content/thirdpartylicenses.html" target="_blank">Third Party Licenses</a></li>
                                        <li><b>Usage information:</b> <br/></li>
                                        <ul class="licence-info" *ngIf="licenceData">
                                            <li>Asset count: {{numberWithCommas(licenceData.assets.count)}}</li>
                                            <li>User count: {{numberWithCommas(licenceData.users.total)}}</li>
                                            <li>Contributor count: {{numberWithCommas(licenceData.users.contributors)}}</li>
                                            <li>Administrator count: {{numberWithCommas(licenceData.users.administrators)}}</li>
                                        </ul>
                                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                                    </ul>
                                    <p>© 2005-{{environment.timeStamp | date:'yyyy'}} Infogix. All rights reserved.</p>
                                    <p>Confidential - Limited distribution to authorized persons only, pursuant to the terms of Infogix Inc. license agreement. This software is protected as an unpublished work and constitutes a trade secret of Infogix Inc.</p>
                                </div>
                            </div>
                        </div>
                        <div class="action-bar">
                            <span class="grow"></span>
                            <button focus-me="focusInput" (click)="closeAbout()" class="button close">Close</button>
                        </div>
                    </d3s-modal>
                </span>`,
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [CompanySettingsService],
    styles: [`
        .licence-info{
            list-style: disc;
            padding-left:18px;
            line-height: 14px;
        }
        .licence-info li{
            list-style: disc;

        }
        .thirdPartyLicence 
        {
            padding-left:0px;
        }
        `]
})

export class HeaderHelpComponent implements OnInit {
    public active: boolean = false;
    display: boolean = false;
    isLoading: boolean = false;
    customHelpResources: HelpResource[] = null;
    customHelpResources$: Observable<any>;
    environment= environment;
    

    public userGuide = CurrentEnvironmentSettings.HelpBaseUri + "Default.htm#c-user-guide/user-guide.htm%3FTocPath%3DUser%2520guide%7C_____0";
    public adminGuide = CurrentEnvironmentSettings.HelpBaseUri + "Default.htm#d-admin/admin-intro.htm%3FTocPath%3DAdministration%2520guide%7C_____0";
    public whatIsNew = CurrentEnvironmentSettings.HelpBaseUri + "Default.htm#b-release-notes/whats-new.htm%3FTocPath%3DWhat";
    public community = "https://support.infogix.com/hc/en-us/community/topics/360000029388-Data3Sixty-Govern";
    
    isModalVisible: boolean = false;
    @ViewChild("popupBox", { static: false }) popupBox: ElementRef;

    licenceData: any;

    constructor(
        private ref: ChangeDetectorRef,
        private settingService: CompanySettingsService,
        protected resourceService: ResourcesService,
    ) { }


    ngOnInit(): void {
        this.loadCustomHelp();
    }

    loadCustomHelp(): void {
        this.customHelpResources$ = this.resourceService.getHelpResources();
    }

    loadLicensingDetails(): void {
        this.licenceData = null;
        this.isLoading = true;
        this.settingService.getLicensingDetails().subscribe((x) => {
            if (x) {
                this.licenceData = x;
                this.isLoading = false;
                this.ref.markForCheck();
            }
        });
    }

    show(item) {
        let panel = item.children[0].nextElementSibling;
        if (panel) {
            this.active = true;

            panel.style.zIndex = 1000;

            panel.style.top = (item.offsetHeight - 1) + 'px'; // -1 for the border so it blends
            panel.style.right = '0px';
        }
    }
    numberWithCommas(x) {
        return x.toLocaleString();
    }
    showAbout() {
        this.isModalVisible = true;
        this.loadLicensingDetails();
    }

    closeAbout() {
        this.isModalVisible = false;
    }

    @HostListener('wheel', ['$event'])
    handleWheelEvent(event) {
        if (this.display == true) {
            event.preventDefault();
        }
    }

    hide(item) {
        this.active = false;
        this.ref.markForCheck();
    }

    checkKey(event) {
        if (event.keyCode) {
            if (event.keyCode == 27 || event.keyCode == 13)
                this.closeAbout();
        }
    }
}
