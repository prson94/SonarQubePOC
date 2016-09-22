///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, RightSidebarService, FusionService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { MapRuleItemDetail } from '../../models/fusion.model';

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
                        <d3s-fusion-technical-mappings></d3s-fusion-technical-mappings>
                    </div>
                `
})

export class FusionListComponent extends BaseComponent implements OnInit, OnDestroy {
    results: any[] = [];
    result: any;
    showTechnicalMappings = false;
    sub: any;
    

    constructor(protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService, protected rightSidebarService: RightSidebarService ) {
        super();
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Fusion');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Fusion'));    


        this.rightSidebarService.showItem({
            title: 'Technical Mappings',
            active: false,
            tag: null
        });

       this.sub =  this.rightSidebarService.rightSidebarClicked$.subscribe(s => {
           this.showTechnicalMappings = s.active
        });

    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
    
};