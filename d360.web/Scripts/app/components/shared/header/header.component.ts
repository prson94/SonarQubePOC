import { Component, ChangeDetectionStrategy, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-header',
    template: ` <div class="navbar-fixed header-container" *ngIf="hideHeader == false">
                    <nav class="top">  
                        <div class="logo" routerLink="/home"> <img [src]="imageSource" alt="logo"> </div>   
                        <d3s-header-back-button *ngIf="showBackButton"></d3s-header-back-button>
                        <d3s-header-breadcrumb [controlWidth]="controlWidth"></d3s-header-breadcrumb>                                          
                        <d3s-header-actions class="header-action" (controlWidthChange)="controlWidth = $event"></d3s-header-actions>
                    </nav>
                </div>
              `,
    providers: [CompanySettingsService]
})

export class HeaderComponent extends BaseComponent implements OnInit, OnDestroy {
    public controlWidth: number = 0;
    imageSource: string = '/Content/images/govern-small-white.svg';
    hideHeader: boolean = false;
    showBackButton: boolean = false;
    subParams: any;

    constructor(private router: Router,
        private route: ActivatedRoute,
        private settings: CompanySettingsService) {
        super();
    }

    ngOnInit(): void {
        this.settings.getSettings()
            .subscribe(data => {
                if (data.CurrentCompanyLogoPath != "")
                    this.imageSource = data.CurrentCompanyLogoPath;
            });

        this.subParams = this.route.queryParams.subscribe((params) => {
            if (params['noheader'] != null) {
                this.hideHeader = params['noheader'].toLocaleLowerCase() === 'true';
            }
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

