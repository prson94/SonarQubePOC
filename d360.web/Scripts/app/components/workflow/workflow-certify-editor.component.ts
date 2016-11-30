import { Input, Component, EventEmitter, Output } from '@angular/core';
import { FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/index';
import { CertifyItem } from '../../models/workflow.model';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-certify-editor',
    template: ` 
                <form (ngSubmit)="onSubmit()" #certifyEditorForm="ngForm">
                <header>Certify Artifact</header>
                <div id="FormDescription" class="form-instructions">The workflow that is triggered when an owner must certify an artifact to validate that all data is correct.</div>                
                <div class="row">                    
                    <div class="col s12 l6">
                        <div class="FieldName">Type</div>
                        <div [innerHtml]="certify?.TypeName"></div>
                        <div class="FieldName"><d3s-tooltip [objectType]="'Artifact'" [objectId]="certify.ID" [tooltipType]="'preview'">{{certify.Name}}</d3s-tooltip></div>
                        <div>{{certify?.Name}}</div>
                        <div class="FieldName">Start Date</div>
                        <div>{{certify?.StartDate | date: 'medium'}}</div>
                        <div class="FieldName">Start Date</div>
                        <div>{{certify?.DueDate | date: 'medium'}}</div>
                    </div>
                    <div class="col s12 l6">      
                        <div id="PoolMessage">
                           By clicking the Certify button below, I certify that all data on this item is correct.
                        </div>                                                                                                                 
                    </div>                    
                </div>
                <div class="row">
                    <div class="col s12">&nbsp;</div>
                    <div class="col s12">
                        <button pButton type="submit" [disabled]="!certifyEditorForm.form.valid" style="width: 150px;" label="Certify"></button>                            
                        <button pButton type="button" (click)="closeClick.emit();" label="Close" style="width: 150px;"></button>
                    </div>
                </div>
                </form>
                `,
    providers: [WorkflowService],
})

export class WorkflowCertifyEditorComponent extends BaseComponent {
    @Input() certify: CertifyItem;

    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    
    constructor(private workflowService: WorkflowService) { super(); }
        
    onSubmit() {
        this.isLoading = true;        
        this.workflowService.certifyArtifact(this.certify).then(
            res => {
                this.isLoading = false;
                this.saveClick.emit();
            });
    }
};