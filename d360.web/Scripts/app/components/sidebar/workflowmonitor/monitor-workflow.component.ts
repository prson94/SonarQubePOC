import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { SecondaryNavService } from '../../../services/right-sidebar.service';

@Component({
    selector: 'd3s-workflow-monitor',
    template: ` 
               <div>
                    <d3s-monitor [objectType]="objectType" [objectId]="objectID"></d3s-monitor>
                </div>
                `
})

export class MonitorWorkflowComponent extends BaseComponent implements OnInit {
    sub: any;
    objectType: string;
    objectID: number;

    constructor(
        private route: ActivatedRoute,
        secondaryNavService: SecondaryNavService
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId'];
            this.objectType = params['objectType'];
        });

        this.checkSecondaryNavLocalStorage();
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
};