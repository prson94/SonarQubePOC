import { Component, Input, OnChanges, SimpleChange, Output, EventEmitter} from '@angular/core';
import { FormGroup } from '@angular/forms';
import { StatisticType, StatisticCheckTypes} from '../../../models/statistic.model';
import { SelectItem } from 'primeng/primeng';
import { StatisticService } from '../../../services/statistics.service';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-admin-statistic-checktype-input',
    template: `             
                  <d3s-loading [isLoading]="isLoading"></d3s-loading>
                  <span *ngIf="!isLoading" [ngSwitch]="statistic?.CheckType">                                                
                        <div *ngSwitchCase="statisticCheckTypes.Existence" class="col s12">
                            <div class="FieldName">{{title()}}</div>
                            <div>                                
                                <select required name="object" #target="ngModel" name="Target" [ngModel]="statistic.CheckObjectCombined" (ngModelChange)="objectChangedCheckObject($event);statistic.CheckObjectCombined=$event;statisticChange.emit(statistic);" style="width:100%">
                                    <option></option>
                                    <option *ngFor="let p of targetTypes" [value]="p.value">{{p.label}}</option>                                    
                                </select>            
                            </div>
                            <div [hidden]="target.valid || target.pristine" class="errorMessage">Target is required</div>
                        </div>                 
                        <div *ngSwitchCase="statisticCheckTypes.Count" class="col s12">                            
                            <div class="FieldName">{{title()}}</div>
                            <div>
                                <select required name="object" #target="ngModel" name="Target" [ngModel]="statistic.CheckObjectCombined" (ngModelChange)="objectChangedCheckObject($event);statistic.CheckObjectCombined=$event;statisticChange.emit(statistic);" style="width:100%">
                                    <option></option>
                                    <option *ngFor="let p of targetTypes" [value]="p.value">{{p.label}}</option>                                    
                                </select>            
                            </div>
                            <div [hidden]="target.valid || target.pristine" class="errorMessage">Target is required</div>
                        </div>
                        <span *ngSwitchCase="statisticCheckTypes.PropertyValueCheck">
                            <div  class="col l6 s12">
                                <div class="FieldName">{{title()}}</div>
                                <div>                                    
                                    <select required name="object" #target="ngModel" name="Target" [ngModel]="statistic.PropertyName" (ngModelChange)="statistic.PropertyName=$event;statisticChange.emit(statistic);" style="width:100%">
                                        <option></option>
                                        <option *ngFor="let p of targetTypes" [value]="p.value">{{p.label}}</option>                                    
                                    </select>   
                                </div>                                
                                <div [hidden]="target.valid || target.pristine" class="errorMessage">Target is required</div>
                            </div>
                            <div  class="col l6 s12">
                                <div class="FieldName">Object Property Value</div>
                                <div><input required style="width: 100%;" name="name" type="string" [ngModel]="statistic.PropertyValue" (ngModelChange)="statistic.PropertyValue=$event;statisticChange.emit(statistic);" ></div>
                            </div>
                        </span>
                        <div *ngSwitchCase="statisticCheckTypes.PropertyPopulated" class="col s12">                            
                            <div class="FieldName">{{title()}}</div>
                            <div>                                
                                <select required name="object" #target="ngModel" name="Target" [ngModel]="statistic.PropertyName" (ngModelChange)="statistic.PropertyName=$event;statisticChange.emit(statistic);" style="width:100%">
                                    <option></option>
                                    <option *ngFor="let p of targetTypes" [value]="p.value">{{p.label}}</option>                                    
                                </select>  
                            </div>
                            <div [hidden]="target.valid || target.pristine" class="errorMessage">Target is required</div>
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.Relationship" class="col s12">                                                        
                            <div class="FieldName">{{title()}}</div>
                            <div><p-multiSelect name="Target" [options]="targetTypes" [ngModel]="statistic.CheckObjects" (ngModelChange)="statistic.CheckObjects=$event;statisticChange.emit(statistic);" [style]="{width:'100%'}"></p-multiSelect></div>
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.FusionOwnership">                            
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.ScoreRollupViaRelationship" class="col s12">
                            <div class="FieldName">{{title()}}</div>
                            <div>                                    
                                <select required name="object" #target="ngModel" name="Target" [ngModel]="statistic.CheckObjectCombined" (ngModelChange)="objectChangedCheckObject($event);statistic.CheckObjectCombined=$event;statisticChange.emit(statistic);" style="width:100%">
                                    <option></option>
                                    <option *ngFor="let p of targetTypes" [value]="p.value">{{p.label}}</option>                                    
                                </select>  
                            </div>
                            <div [hidden]="target.valid || target.pristine" class="errorMessage">Target is required</div>
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.ScoreRollupViaOwnership" class="col s12">                            
                            <div class="FieldName">{{title()}}</div>
                            <div>                                
                                <select required name="object" #target="ngModel" name="Target" [ngModel]="statistic.CheckObjectCombined" (ngModelChange)="objectChangedCheckObject($event);statistic.CheckObjectCombined=$event;statisticChange.emit(statistic);" style="width:100%">
                                    <option></option>
                                    <option *ngFor="let p of targetTypes" [value]="p.value">{{p.label}}</option>                                    
                                </select>
                            </div>
                            <div [hidden]="target.valid || target.pristine" class="errorMessage">Target is required</div>
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.EventMetric">
                            <div  class="col l6 s12">
                                <div class="FieldName">Valid Field Count Name</div>
                                <div><input required style="width: 100%;" #validCnt="ngModel" name="name" type="string" [ngModel]="statistic.ValidField"  (ngModelChange)="statistic.ValidField=$event;statisticChange.emit(statistic);"></div>
                                <div [hidden]="validCnt.valid || validCnt.pristine" class="errorMessage">Valid Field Count Name is required</div>
                            </div>
                            <div  class="col l6 s12">
                                <div class="FieldName">Invalid Field Count Name</div>
                                <div><input required #invalid="ngModel" style="width: 100%;" name="name" type="string" [ngModel]="statistic.InvalidField" (ngModelChange)="statistic.InvalidField=$event;statisticChange.emit(statistic);"></div>
                                <div [hidden]="invalid.valid || invalid.pristine" class="errorMessage">Invalid Field Count Name is required</div>
                            </div>
                            <div  class="col l6 s12">
                                <div class="FieldName">Threshold (0.00)</div>
                                <div><input required #thres="ngModel" style="width: 100%;" name="name" type="number" [ngModel]="statistic.Threshold" (ngModelChange)="statistic.Threshold=$event;statisticChange.emit(statistic);"></div>
                                <div [hidden]="thres.valid || thres.pristine" class="errorMessage">Threshold is required</div>
                            </div>
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.PredicateMetric" class="col s12">                            
                            <div class="FieldName">{{title()}}</div>
                            <div>                                
                                <select required name="object" #target="ngModel" name="Target" [ngModel]="statistic.Predicate" (ngModelChange)="statistic.Predicate=$event;statisticChange.emit(statistic);" style="width:100%">
                                    <option></option>
                                    <option *ngFor="let p of targetTypes" [value]="p.value">{{p.label}}</option>                                    
                                </select>
                            </div>
                            <div [hidden]="target.valid || target.pristine" class="errorMessage">Predicate is required</div>
                        </div>
                        <div *ngSwitchDefault></div>
                  </span>                                   
                `
})
    

export class AdminStatisticCheckTypeInput extends BaseComponent implements OnChanges{
    @Input() statistic: StatisticType;
    @Output() statisticChange = new EventEmitter();
    targetTypes: SelectItem[] = [];    
    statisticCheckTypes = StatisticCheckTypes;
    
    constructor(private statisticService: StatisticService) { super(); }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {    
        if (this.statistic != null && this.statistic.CheckType != null && this.statistic.ObjectID > 0 && this.statistic.Object != null) this.load();
    }

    title() {
        if (this.statistic == null) return "";
        switch (this.statistic.CheckType) {
            case StatisticCheckTypes.Count:
            case StatisticCheckTypes.Existence:
                return "Item To Check For";            
            case StatisticCheckTypes.PredicateMetric:
                return "Predicate To Check For";
            case StatisticCheckTypes.ScoreRollupViaOwnership:
                return "Owned Type To Check For";
            case StatisticCheckTypes.PropertyPopulated:
                return "Field Populated";            
            case StatisticCheckTypes.PropertyValueCheck:
            case StatisticCheckTypes.ScoreRollupViaRelationship:
            case StatisticCheckTypes.Relationship:
                return "Related Type To Check For";
        }
    }
        
    load() {        
        this.isLoading = true;
        this.statisticService
            .getStatisticCheckObjects(this.statistic.Object, this.statistic.ObjectID, this.statistic.CheckType)
            .then(targets => {
                this.targetTypes = [];
                for (let target of targets) {
                    this.targetTypes.push({
                        label: target.title, value: target.value
                    });
                }
                if (this.statistic.CheckObject && this.statistic.CheckObjectID) {
                    this.statistic.CheckObjectCombined = this.statistic.CheckObject + '|' + this.statistic.CheckObjectID.toString();
                    this.statisticChange.emit(this.statistic);
                }

                this.isLoading = false;
            });            
    }

    objectChangedCheckObject(event) {
        if (event) {
            let parts = event.split('|');
            if (parts.length < 2) return;

            this.statistic.CheckObject = parts[0];
            this.statistic.CheckObjectID = parts[1];
            this.statisticChange.emit(this.statistic);
        }
        else {
            console.log("[WARNING] INVALID DATA PASSED TO OBJECTCHANGEDCHECKOBJECT METHOD.");
        }
    }

}
