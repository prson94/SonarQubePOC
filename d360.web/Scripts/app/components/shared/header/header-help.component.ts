import { Component, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, ElementRef, OnInit, HostListener } from "@angular/core";
import { CompanySettingsService } from "../../../services/settings.service";
import { ResourcesService } from "../../../services/resources.service";
import { HelpMenuService } from '../../shared/helpmenu/helpmenu.service';
import { HelpResource } from "../../../models/resource.model";
import { Observable } from "rxjs";
import { environment } from '../../../../environments/environment';
import { AuthenticationService } from "../../../services/authentication.service";
import { HelpMenu } from "../../../models/helpmenu.model";

@Component({
    selector: 'd3s-header-help',
    templateUrl: `header-help.component.html`,
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [HelpMenuService],
    styles: [`
        .licence-info{
            list-style: disc;
            padding-left:18px;
            line-height: 14px;
        }
        .licence-info li{
            list-style: none;
            padding-left: 16px;
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
  
    isModalVisible: boolean = false;
    @ViewChild("popupBox", { static: false }) popupBox: ElementRef;

    licenceData: any;

    public items: HelpMenu[] = [];
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
