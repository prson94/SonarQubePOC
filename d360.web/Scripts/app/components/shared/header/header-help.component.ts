import { Component, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, ElementRef, OnInit, HostListener } from "@angular/core";
import { CurrentEnvironmentSettings } from "../../../static/environment-settings";
import { CompanySettingsService } from "../../../services/settings.service";
import { ResourcesService } from "../../../services/resources.service";
import { HelpMenuService } from '../../shared/helpmenu/helpmenu.service';
import { HelpResource } from "../../../models/resource.model";
import { Observable } from "rxjs";
import { HelpMenu } from "../../../models/helpmenu.model";
import { AuthenticationService } from "../../../services/authentication.service";
declare var __BUILD_DATE: string;
declare var VersionNumber: string;

@Component({
    selector: 'd3s-header-help',
    template: ` <span #item class="header-search header-table" [ngClass]="{'header-search-active':active}" (mouseenter)="show(item)" (mouseleave)="hide(item)">
                    <div class="header-button"><i class="fa fa-question-circle"></i></div>
                    <div class="header-help search-child header-profile-panel">
                       <ul class="header-help-dropdown">      
                            <ng-container *ngFor="let i of items">
                                    <li *ngIf="i.visibilty == 1 && i.Url != 'about'" class="header-item" pTooltip="{{i.Description}} "tooltipPosition="left" tooltipStyleClass="ig-tooltip"><div class="mini-menu-line"><div class="text" ><a target="_blank" [href]="i.Url">{{i.Name}}</a></div></div></li>
                                    <li *ngIf="i.visibilty == 2 && isAdmin && i.Url != 'about'" class="header-item" pTooltip="{{i.Description}} "tooltipPosition="left" tooltipStyleClass="ig-tooltip"><div class="mini-menu-line"><div class="text"><a target="_blank" [href]="i.Url">{{i.Name}}</a></div></div></li>
                                    <li *ngIf="i.visibilty == 1 && (i.Url == 'about' && i.isSystem == 1)" class="header-item" pTooltip="{{i.Description}} "tooltipPosition="left" tooltipStyleClass="ig-tooltip"><div class="mini-menu-line"><div class="text"><a target="_blank" (click)="showAbout()">{{i.Name}}</a></div></div></li>
                                    <li *ngIf="i.visibilty == 2 && (i.Url == 'about' && i.isSystem == 1) && isAdmin" class="header-item" pTooltip="{{i.Description}} "tooltipPosition="left" tooltipStyleClass="ig-tooltip"><div class="mini-menu-line"><div class="text"><a target="_blank" (click)="showAbout()">{{i.Name}}</a></div></div></li>
                            </ng-container> 
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
                                        <li><b>Build Version:</b> {{this.versionNumber}}</li>
                                        <li><b>Build Date:</b> {{this.buildDate | date:'short'}}</li>
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
                                    <p>© 2005-{{this.buildDate | date:'yyyy'}} Infogix. All rights reserved.</p>
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
    providers: [CompanySettingsService, HelpMenuService],
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

    buildDate: string = __BUILD_DATE;
    versionNumber: string = VersionNumber;
    isModalVisible: boolean = false;
    @ViewChild("popupBox", { static: false }) popupBox: ElementRef;

    licenceData: any;

    private items: HelpMenu[] = [];
    isAdmin: boolean = false;

    constructor(
        private ref: ChangeDetectorRef,
        private settingService: CompanySettingsService,
        private helpMenuService: HelpMenuService,
        protected resourceService: ResourcesService,
        protected authenticationService: AuthenticationService,
    ) { }


    ngOnInit(): void {
        this.helpMenuService.getHelpMenuItems()
            .subscribe((r) => {
                this.items = r;
                this.items.sort((a, b) => (a.order < b.order ? -1 : 1));
            });
        this.authenticationService.checkCurrentUserAdmin().subscribe((a) => {
            this.isAdmin = a;
        });
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