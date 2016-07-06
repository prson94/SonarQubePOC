import { Component, Input, OnChanges, SimpleChange } from '@angular/core';
import { FormGroup, REACTIVE_FORM_DIRECTIVES } from '@angular/forms';
import { StatisticType, StatisticCheckTypes} from '../../models/statistic.model';
import {Button, Editor, InputText, Dropdown, SelectItem, InputMask, MultiSelect} from 'primeng/primeng';
import { StatisticService} from '../../services/index';

@Component({
    selector: 'd3s-admin-statistic-checktype-input',
    template: `             
                  <div *ngIf="isLoading">
                    <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                  </div>           
                  <span *ngIf="!isLoading" [ngSwitch]="statistic?.CheckType">                                                
                        <div *ngSwitchCase="statisticCheckTypes.Existence" class="col s12">
                            <div class="FieldName">{{title()}}</div>
                            <div><p-dropdown required name="Target" [id]="'Target'" (onChange)="objectChangedCheckObject($event);" [options]="targetTypes" [(ngModel)]="statistic.CheckObjectCombined" [style]="{width:'100%'}"></p-dropdown></div>
                        </div>                 
                        <div *ngSwitchCase="statisticCheckTypes.Count" class="col s12">                            
                            <div class="FieldName">{{title()}}</div>
                            <div><p-dropdown required name="Target" [id]="'Target'" (onChange)="objectChangedCheckObject($event);" [options]="targetTypes" [(ngModel)]="statistic.CheckObjectCombined" [style]="{width:'100%'}"></p-dropdown></div>
                        </div>
                        <span *ngSwitchCase="statisticCheckTypes.PropertyValueCheck">
                            <div  class="col l6 s12">
                                <div class="FieldName">{{title()}}</div>
                                <div><p-dropdown required name="Target" [id]="'Target'" [options]="targetTypes" [(ngModel)]="statistic.PropertyName" [style]="{width:'100%'}"></p-dropdown></div>
                            </div>
                            <div  class="col l6 s12">
                                <div class="FieldName">Object Property Value</div>
                                <div><input required style="width: 100%;" name="name" [type]="'string'" [(ngModel)]="statistic.PropertyValue"></div>
                            </div>
                        </span>
                        <div *ngSwitchCase="statisticCheckTypes.PropertyPopulated" class="col s12">                            
                            <div class="FieldName">{{title()}}</div>
                            <div><p-dropdown required name="Target" [id]="'Target'" [options]="targetTypes" [(ngModel)]="statistic.PropertyName" [style]="{width:'100%'}"></p-dropdown></div>
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.Relationship" class="col s12">                                                        
                            <div class="FieldName">{{title()}}</div>
                            <div><p-multiSelect name="Target" [options]="targetTypes" [(ngModel)]="statistic.CheckObjects" [style]="{width:'100%'}"></p-multiSelect></div>
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.FusionOwnership">                            
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.ScoreRollupViaRelationship">
                            <div class="FieldName">{{title()}}</div>
                            <div><p-dropdown required name="Target" [id]="'Target'" (onChange)="objectChangedCheckObject($event);" [options]="targetTypes" [(ngModel)]="statistic.CheckObjectCombined" [style]="{width:'100%'}"></p-dropdown></div>
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.ScoreRollupViaOwnership" class="col s12">                            
                            <div class="FieldName">{{title()}}</div>
                            <div><p-dropdown required name="Target" [id]="'Target'" (onChange)="objectChangedCheckObject($event);" [options]="targetTypes" [(ngModel)]="statistic.CheckObjectCombined" [style]="{width:'100%'}"></p-dropdown></div>
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.EventMetric">
                            <div  class="col l6 s12">
                                <div class="FieldName">Valid Field Count Name</div>
                                <div><input required style="width: 100%;" name="name" [type]="'string'" [(ngModel)]="statistic.ValidField"></div>
                            </div>
                            <div  class="col l6 s12">
                                <div class="FieldName">Invalid Field Count Name</div>
                                <div><input required style="width: 100%;" name="name" [type]="'string'" [(ngModel)]="statistic.InvalidField"></div>
                            </div>
                            <div  class="col l6 s12">
                                <div class="FieldName">Threshold (0.00)</div>
                                <div><input required style="width: 100%;" name="name" [type]="'number'" [(ngModel)]="statistic.Threshold"></div>
                            </div>
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.PredicateMetric" class="col s12">                            
                            <div class="FieldName">{{title()}}</div>
                            <div><p-dropdown required name="Target" [id]="'Target'" [options]="targetTypes" [(ngModel)]="statistic.Predicate" [style]="{width:'100%'}"></p-dropdown></div>
                        </div>
                        <div *ngSwitchDefault>
                            
                        </div>
                  </span>                                   
                `,
    directives: [REACTIVE_FORM_DIRECTIVES, Button, Editor, Dropdown, MultiSelect]
})
    

export class AdminStatisticCheckTypeInput implements OnChanges{
    @Input() statistic: StatisticType;
    @Input() objectID: number = 0;
    @Input() object: string;
    @Input() checkType: StatisticCheckTypes;

    targetTypes: SelectItem[] = [];
    error: any;
    isLoading: boolean = false;
    statisticCheckTypes = StatisticCheckTypes;
       

    constructor(private statisticService: StatisticService) { }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {        
        if (this.statistic != null && this.statistic.CheckType != null && this.objectID > 0 && this.object != null) this.load();
    }

    title() {
        if (this.statistic == null) return "";
        switch (this.statistic.CheckType) {
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
        console.log(3);
        this.isLoading = true;
        this.statisticService
            .getStatisticCheckObjects(this.object, this.objectID, this.statistic.CheckType)
            .then(targets => {
                this.targetTypes = [];
                for (let target of targets) {
                    console.log(target.value);                 
                    this.targetTypes.push({
                        label: target.title, value: target.value
                    });
                }
                if (this.statistic.CheckObject && this.statistic.CheckObjectID)
                    this.statistic.CheckObjectCombined = this.statistic.CheckObject + '|' + this.statistic.CheckObjectID.toString();
                                
                this.isLoading = false;
            })
            .catch(error => this.error = error);
    }

    objectChangedCheckObject(event) {
        let parts = event.value.split('|');
        if (parts.length < 2) return;

        this.statistic.CheckObject = parts[0];
        this.statistic.CheckObjectID = parts[1];
    }

}
