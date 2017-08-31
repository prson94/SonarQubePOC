import { Component, OnChanges, SimpleChange, Input } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { QualifierService } from '../../services/qualifier.service';
import { MessagesService } from '../../services/messages.service';
import { QualifierType } from '../../models/qualifier.model';
import { RuleImplementation } from '../../models/rule.model';

@Component({
    selector: 'd3s-rule-implementation-summary',
    template: ` 
            <p-tabView *ngIf="implementation" styleClass="pillTabs">
                <p-tabPanel header="Definition">
                    <d3s-object-detail [objectID]="implementation?.ID" [objectType]="'RuleImplementation'"></d3s-object-detail>
                </p-tabPanel>
                <p-tabPanel header="Results">
                    <d3s-rule-results-grid [implementationId]="implementation?.ID" [showTitle]="false"></d3s-rule-results-grid> 
                </p-tabPanel>
                <p-tabPanel header="Qualifiers">
                   <d3s-rule-qualifier-grid [implementationId]="implementation?.ID" [showTitle]="false"></d3s-rule-qualifier-grid>
                </p-tabPanel>                
            </p-tabView>
          `,
    providers: [QualifierService],
})

export class RuleImplementationSummaryComponent extends BaseComponent {
    @Input() implementation: RuleImplementation;
    
}