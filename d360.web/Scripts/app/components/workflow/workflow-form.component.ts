import { Input, Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute }       from '@angular/router';
import { FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { WorkflowService } from '../../services/workflow.service';


@Component({
    selector: 'd3s-workflow-form',
    template: ` 
                <div class="row">
                    <div class="col s12">
                        Workflow Form page placeholder
                    </div>
                </div>                
                `,
    providers: [WorkflowService]
})

export class WorkflowFormComponent extends BaseComponent implements OnInit, OnDestroy {    
    private sub: any;
    private workflowId: number;
    private workflowItemStepId: number;

    constructor(private route: ActivatedRoute,
            private location: Location,
            private router: Router,
            protected titleService: Title,
            protected headerBreadcrumbService: HeaderBreadcrumbService,
            protected workflowService: WorkflowService
        )
    {
        super();
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearCurrentObjectInfo();

        this.sub = this.route.params.subscribe(params => {            
            this.workflowId = +params['workflowId'];
            this.workflowItemStepId = +params['workflowItemStepId'];
            this.load();
        });
    }

    
    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    private load() {
        this.isLoading = true;
        this.workflowService.getWorkflowForm(this.workflowId, this.workflowItemStepId)
            .then(res => {
                this.isLoading = false;
                console.log(res);
            });
    }
};