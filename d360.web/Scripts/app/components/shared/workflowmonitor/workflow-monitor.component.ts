import { Input, Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { WorkflowType } from '../../../models/workflow.model';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../../models/breadcrumb.model';


@Component({
    selector: 'd3s-workflow-monitor',
    template: ` 
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                                <div class="row">
                                    <div class="col s4">
                                        Select a Workflow
                                    </div>
                                    <div class="col s8">
                                        <d3s-workflow-diagram [id]="1" readonly="true"></d3s-workflow-diagram>
                                    </div>
                                </div>
                        </div>
                    </div>
                </div>                
                `,
})

export class WorkflowMonitorComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() objectID: number;
    @Input() objectType: string;
    
    
    constructor() {
        super();
    }

    ngOnInit() {
        
    }
    

    ngOnDestroy() {
        
    }

};