import { Component, Input, OnInit } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { BaseComponent } from '../../shared/base.component';
import { Dashboard, DashboardTokens } from '../../../models/dashboard.model'

@Component({
    selector: 'd3s-sagacity-viewer',
    template: ` 
                <header>{{dashboard?.Name}}<d3s-tile-actions [hasFullScreen]="false"></d3s-tile-actions></header>
                <div class="row">
                    <div class="col s12">
                        <iframe style="width:100%;height:100%;border:0;" [src]="sagacityUrl" frameborder="0"></iframe>
                    </div>
                </div>
            `,    
})

export class SagacityViewerComponent extends BaseComponent implements OnInit {
    @Input() dashboard: Dashboard;    

    private sagacityUrl: any;

    constructor(private sanitizer: DomSanitizer) {
        super();        
    }

    ngOnInit() {
        this.sagacityUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.dashboard.Url);
    }
}