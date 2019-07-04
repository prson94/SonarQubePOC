import { Component, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, ElementRef, AfterViewInit, HostListener } from '@angular/core';
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
                            <li class="header-item"><div class="mini-menu-line"><div class="text"><a target="_blank" (click)="popup(item2)">About Data3Sixty® Govern</a></div></div></li>
                       </ul>
                    </div>
                    <div #item2 class="modal-overlay about" tabindex=0 (keydown)="checkKey($event,item2)" >
                    <div class="modal-dialog">
                        <div class="title-bar">
                            <h1>About Data3Sixty® Govern</h1>
                            <span class="grow"></span>
                            <button (click)="closePopUp(item2)" class="light bar button close" title="Close"><i class="fa fa-times"></i></button>
                        </div>
                        <div class="content">
                            <div class="flex row">
                                <img class="about-image" src="../../../../../Content/images/aboutLogo.png"/>
                                <div class="about-info">
                                    <ul>
                                        <li><b>Build Version:</b> {{this.versionNumber}}</li>
                                        <li><b>Build Date:</b> {{this.buildDate}}</li>
                                        <li><b>Support:</b> <a href="http://support.infogix.com" target="_blank">http://support.infogix.com</a></li>
                                    </ul>
                                    <p>© 2005-2019 Infogix. All rights reserved.</p>
                                    <p>Confidential - Limited distribution to authorized persons only, pursuant to the terms of Infogix Inc. license agreement. This software is protected as an unpublished work and constitutes a trade secret of Infogix Inc.</p>
                                </div>
                            </div>
                        </div>
                        <div class="action-bar">
                            <span class="grow"></span>
                            <button focus-me="focusInput" (click)="closePopUp(item2)" class="primary button close">Close</button>
                        </div>
                    </div>
                </div>
            </span>`,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderHelpComponent implements AfterViewInit{
    public active: boolean = false;
    private hideHandle: number = 0;
    display: boolean = false;

    public userGuide = CurrentEnvironmentSettings.HelpBaseUri + "Default.htm#c-user-guide/user-guide.htm%3FTocPath%3DUser%2520guide%7C_____0";
    public adminGuide = CurrentEnvironmentSettings.HelpBaseUri + "Default.htm#d-admin/admin-intro.htm%3FTocPath%3DAdministration%2520guide%7C_____0";
    public whatIsNew = CurrentEnvironmentSettings.HelpBaseUri + "Default.htm#b-release-notes/whats-new.htm%3FTocPath%3DWhat";
    public community = "https://support.infogix.com/hc/en-us/community/topics/360000029388-Data3Sixty-Govern";
    buildDate: string = __BUILD_DATE;
    versionNumber: string = VersionNumber;

    @ViewChild("item2") popupBox: ElementRef;

    constructor(
        private ref: ChangeDetectorRef
    ) { }

    ngAfterViewInit(): void {
       
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

    popup(item) {
        this.display = true;
        item.focus();
        item.className = "modal-overlay about";
        item.className = item.className + " show";
    }

    closePopUp(item) {
        this.display = false;
        item.className = "modal-overlay about";
        item.className = item.className + " begin-hide";
    }

    @HostListener('wheel', ['$event'])
    handleWheelEvent(event) {
        if (this.display == true) {
            event.preventDefault();
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

    checkKey(event,popupBox) {
        if (event.keyCode) {
            if (event.keyCode == 27 || event.keyCode == 13)
                this.closePopUp(popupBox);
        }
    }
}