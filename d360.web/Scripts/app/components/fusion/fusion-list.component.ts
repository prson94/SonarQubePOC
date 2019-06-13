import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { MapRuleItemDetail } from '../../models/fusion.model';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-fusion-list',        
    template: ` 
                    <div class="row" *ngIf="!showTechnicalMappings">
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
                                <div class="col s12">   
                                    <d3s-fusion-promotion-history></d3s-fusion-promotion-history>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="row" *ngIf="showTechnicalMappings">
                        <div class="col s12">   
                            <d3s-fusion-technical-mappings></d3s-fusion-technical-mappings>
                        </div>
                    </div>
                `
})

export class FusionListComponent extends BaseComponent implements OnInit, OnDestroy {
    results: any[] = [];
    result: any;
    showTechnicalMappings = false;
    sub: any;
    

    constructor(protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService, rightSidebarService: RightSidebarService ) {
        super();
        this.rightSidebarService = rightSidebarService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Fusion');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.getFolderTitle('#Fusion').then((res) => {
            let areaBreadcrumb = new Breadcrumb(res ? res : 'Fusion', `${SiteUrlHelpers.SITE_URL_FUSION_ROOT}`);
            this.headerBreadcrumbService.showBreadcrumb(areaBreadcrumb);
        });
        

        this.clearSidebar();
        //this.rightSidebarService.showItem(new RightSidebarItem('Technical Mappings','technical'));

        this.sub = this.rightSidebarService.rightSidebarClicked$.subscribe(s => {
            if (s.tag == 'technical')
                this.showTechnicalMappings = s.active
        });

    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();
    }
    
};