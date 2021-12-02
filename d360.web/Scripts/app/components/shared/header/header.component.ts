import { Component, ChangeDetectionStrategy, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { CompanySettingsService } from '../../../services/settings.service';
import { CompanySettingEnum } from '../../../models/settings.model';

@Component({
    selector: 'd3s-header',
    template: ` <div class="navbar-fixed header-container">
                    <nav class="top">  
                        <div class="logo" routerLink="/home"> <img [src]="imageSource" alt="logo"> </div>   
                        <d3s-header-back-button *ngIf="showBackButton"></d3s-header-back-button>
                        <d3s-header-breadcrumb [controlWidth]="controlWidth"  [showBackButton]="showBackButton">></d3s-header-breadcrumb>                                          
                        <d3s-header-actions class="header-action" (controlWidthChange)="controlWidth = $event"></d3s-header-actions>
                    </nav>
                </div>
              `,
    providers: []
})

export class HeaderComponent extends BaseComponent implements OnInit, OnDestroy {
    public controlWidth: number = 0;
    imageSource: string = '/Content/images/govern-small-white.svg';
    showBackButton: boolean = false;
    subParams: any;

    constructor(private router: Router,
        private route: ActivatedRoute,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    ngOnInit(): void {
        let logoSetting = this.settingsService.getSettingById(CompanySettingEnum.CompanyLogo);
        if (logoSetting.StringSetting && logoSetting.StringSetting.Value != "") {
            this.imageSource = logoSetting.StringSetting.Value;
        }

        this.subParams = this.route.queryParams.subscribe((params) => {
            if (params['showbackbutton'] != null) {
                this.showBackButton = params['showbackbutton'].toLocaleLowerCase() === 'true';
            }
        });
    }   

    ngOnDestroy() {
        if (this.subParams) {
            this.subParams.unsubscribe();
        }
    }
}

