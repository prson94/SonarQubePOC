import { Component, Input, OnInit } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { BaseComponent } from '../../shared/base.component';
import { Dashboard } from '../../../models/dashboard.model'
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-sagacity-viewer',
    template: ` 
                <header>{{dashboard?.Name}}<d3s-tile-actions [hasNewWindow]="true"  (newWindowClick)="openWindow()" ></d3s-tile-actions></header>
                <div class="row">
                    <div class="col s12">
                        <iframe style="width:100%;height:700px;border:0;" [src]="sagacityUrl" frameborder="0"></iframe>
                    </div>
                </div>
            `,    
})

export class SagacityViewerComponent extends BaseComponent implements OnInit {
    @Input() dashboard: Dashboard;    

    sagacityUrl: any;

    constructor(private sanitizer: DomSanitizer,
        protected settingsService: CompanySettingsService) {
        super(settingsService);        
    }

    ngOnInit() {
        this.sagacityUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.dashboard.Url);
    }

    openWindow() {
       window.open(this.dashboard.Url, "_blank");
    }
}