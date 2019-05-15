import { Component, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-header',
    template: ` <div class="navbar-fixed header-container">
                    <nav class="top">  
                        <div class="logo" routerLink="/home"> <img [src]="imageSource" alt="logo"> </div>                                 
                        <d3s-header-breadcrumb [controlWidth]="controlWidth"></d3s-header-breadcrumb>                                          
                        <d3s-header-actions style="margin-left:auto;" (controlWidthChange)="controlWidth = $event"></d3s-header-actions>
                    </nav>
                </div>
              `,
    providers: [CompanySettingsService]
})

export class HeaderComponent extends BaseComponent implements OnInit {
    public controlWidth: number = 0;
    private imageSource: string = '/Content/images/logo.new.png';

    constructor(private router: Router,
        private route: ActivatedRoute,
        private settings: CompanySettingsService) {
        super();
    }

    ngOnInit(): void {
        this.settings.getSettings()
            .then(data => {
                if (data.CurrentCompanyLogoPath != "")
                    this.imageSource = data.CurrentCompanyLogoPath;
            });
    }   


}

