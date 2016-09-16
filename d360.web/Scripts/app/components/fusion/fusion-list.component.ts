///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';

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
                                <div class="col s12">   
                                    <d3s-fusion-promotion-history></d3s-fusion-promotion-history>
                                </div>
                            </div>
                        </div>
                    </div>
                `
})

export class FusionListComponent extends BaseComponent implements OnInit {
    results: any[] = [];
    result: any;

    constructor(protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Fusion');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Fusion'));        
    }
    
};