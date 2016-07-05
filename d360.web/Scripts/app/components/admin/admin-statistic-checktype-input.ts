import { Component, Input, OnChanges, SimpleChange } from '@angular/core';
import { FormGroup, REACTIVE_FORM_DIRECTIVES } from '@angular/forms';
import { StatisticType, StatisticCheckTypes} from '../../models/statistic.model';
import {Button, Editor, InputText, Dropdown, SelectItem, InputMask} from 'primeng/primeng';
import { StatisticService} from '../../services/index';

@Component({
    selector: 'd3s-admin-statistic-checktype-input',
    template: `                        
                  <div [ngSwitch]="checkType" class="col s12">
                        <div class="FieldName">{{title()}}</div>
                        <div *ngSwitchCase="statisticCheckTypes.Existence">                            
                            <div><p-dropdown required name="Target" [id]="'Target'" [options]="targetTypes" [(ngModel)]="CheckObjects" [style]="{width:'100%'}"></p-dropdown></div>                            
                        </div>                 
                        <div *ngSwitchWhen="statisticCheckTypes.Count">                            
                            <div><p-dropdown required name="Target" [id]="'Target'" [options]="targetTypes" [(ngModel)]="CheckObjects" [style]="{width:'100%'}"></p-dropdown></div>                            
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.PropertyValueCheck">
                            Property value check
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.PropertyPopulated">                            
                            <div><p-dropdown required name="Target" [id]="'Target'" [options]="targetTypes" [(ngModel)]="CheckObjects" [style]="{width:'100%'}"></p-dropdown></div>                            
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.Relationship">                                                        
                            <div><p-dropdown required name="Target" [id]="'Target'" [options]="targetTypes" [(ngModel)]="CheckObjects" [style]="{width:'100%'}"></p-dropdown></div>                                                        
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.FusionOwnership">                            
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.ScoreRollupViaRelationship">
                            rollup via relationship
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.ScoreRollupViaOwnership">                            
                            <div><p-dropdown required name="Target" [id]="'Target'" [options]="targetTypes" [(ngModel)]="CheckObjects" [style]="{width:'100%'}"></p-dropdown></div>                            
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.EventMetric">
                            event metric
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.PredicateMetric">                            
                            <div><p-dropdown required name="Target" [id]="'Target'" [options]="targetTypes" [(ngModel)]="CheckObjects" [style]="{width:'100%'}"></p-dropdown></div>                                                        
                        </div>
                        <div *ngSwitchDefault>
                            Unknown / Unsupported statistic check type
                        </div>
                  </div>                                   
                `,
    directives: [REACTIVE_FORM_DIRECTIVES, Button, Editor, Dropdown]
})
    

export class AdminStatisticCheckTypeInput implements OnChanges{
    @Input() checkType: StatisticCheckTypes = StatisticCheckTypes.Count;
    @Input() objectID: number = 0;
    @Input() object: string;

    targetTypes: SelectItem[] = [];
    error: any;
    statisticCheckTypes = StatisticCheckTypes;
    CheckObjects: any;

    constructor(private statisticService: StatisticService) { }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {                
        if (this.checkType != null && this.objectID > 0 && this.object != null) this.load();
    }

    title() {
        switch (this.checkType) {
            case StatisticCheckTypes.Count:
                return "Item To Check For";
            case StatisticCheckTypes.Existence:
                return "Item To Check For";
            case StatisticCheckTypes.PredicateMetric:
                return "Predicate To Check For";
            case StatisticCheckTypes.ScoreRollupViaOwnership:
                return "Owned Type To Check For";
            case StatisticCheckTypes.Relationship:
                return "Related Type To Check For";
            case StatisticCheckTypes.PropertyPopulated:
                return "Field Populated";
            case StatisticCheckTypes.PropertyPopulated:
                return "Field Populated";
            case StatisticCheckTypes.PropertyValueCheck:
                return "Related Type To Check For";
        }
    }

    //get isValid() { return (this.field.Required && this.field.Value && this.field.Value.length > 0) || !this.field.Required || this.field.FieldType == 'Boolean'; }

    load() {
      //  if (this.statistic.ObjectCombined == null) return;
//        var typeInfo = this.statistic.ObjectCombined.split('|');
  //      if (typeInfo.length < 2) return;

        this.statisticService
            .getStatisticCheckObjects(this.object, this.objectID, this.checkType)
            .then(targets => {
                this.targetTypes = [];
                for (let target of targets) {
                    this.targetTypes.push({
                        label: target.title, value: target.value
                    });
                }
            })
            .catch(error => this.error = error);
    }

}
