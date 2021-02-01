import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { MapRuleItemDetail } from '../../models/fusion.model';
import { SecondaryNavItem } from '../../models/secondaryNav.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-fusion-list',        
    template: ` 
                    <div class="row">
                        <div class="col l6 s12">
                            <d3s-fusion-configuration></d3s-fusion-configuration>
                        </div>
                        <div class="col l6 s12">
                            <div class="row">
                                <div class="col s12">   
                                    <d3s-fusion-statistics></d3s-fusion-statistics>                                    
                                </div>
                                <div class="col s12">   
                                    <d3s-fusion-agent-history></d3s-fusion-agent-history>
                                </div>
                                <div class="col s12">   
                                    <d3s-fusion-execution-history></d3s-fusion-execution-history>
                                </div>
                            </div>
                        </div>
                    </div>
                `
})

export class FusionListComponent extends BaseComponent implements OnInit, OnDestroy {
    results: any[] = [];
    result: any;
    sub: any;
    

    constructor(protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService, secondaryNavService: SecondaryNavService ) {
        super();
        this.secondaryNavService = secondaryNavService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Fusion');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.getFolderTitle('#Fusion').then((res) => {
            let areaBreadcrumb = new Breadcrumb(res ? res : 'Fusion', `${SiteUrlHelpers.SITE_URL_FUSION_ROOT}`);
            this.headerBreadcrumbService.showBreadcrumb(areaBreadcrumb);
            this.headerBreadcrumbService.getFolderIcon(areaBreadcrumb.text).subscribe(icon => {
                this.clearSidebar();

                this.secondaryNavService.setCurrentArea(areaBreadcrumb.text, icon, 'Fusion List');
                this.secondaryNavService.showHeader(true);
            });
        });
        


    }

    ngOnDestroy() {
        this.clearSidebar();
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
    
}