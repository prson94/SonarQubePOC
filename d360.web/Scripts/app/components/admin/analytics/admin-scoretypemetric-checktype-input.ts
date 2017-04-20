import { Component, Input, OnChanges, SimpleChange, Output, EventEmitter} from '@angular/core';
import { FormGroup } from '@angular/forms';
import { ScoreTypeMetric, MetricCheckTypes} from '../../../models/statistic.model';
import { SelectItem } from 'primeng/primeng';
import { StatisticService } from '../../../services/statistics.service';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-admin-metric-checktype-input',
    template: `             
                  <d3s-loading [isLoading]="isLoading"></d3s-loading>
                  <span *ngIf="!isLoading" [ngSwitch]="metric?.CheckType">                                                
                        <div *ngSwitchCase="statisticCheckTypes.Existence" class="col s12">
                            <div class="FieldName">{{title()}}</div>
                            <div>                                
                                <select required name="object" #target="ngModel" name="Target" [ngModel]="metric.CheckObjectCombined" (ngModelChange)="objectChangedCheckObject($event);metric.CheckObjectCombined=$event;statisticChange.emit(metric);" style="width:100%">
                                    <option></option>
                                    <option *ngFor="let p of targetTypes" [value]="p.value">{{p.label}}</option>                                    
                                </select>            
                            </div>
                            <div [hidden]="target.valid || target.pristine" class="errorMessage">Target is required</div>
                        </div>                 
                        <div *ngSwitchCase="statisticCheckTypes.Count" class="col s12">                            
                            <div class="FieldName">{{title()}}</div>
                            <div>
                                <select required name="object" #target="ngModel" name="Target" [ngModel]="metric.CheckObjectCombined" (ngModelChange)="objectChangedCheckObject($event);metric.CheckObjectCombined=$event;statisticChange.emit(metric);" style="width:100%">
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
                                    <select required name="object" #target="ngModel" name="Target" [ngModel]="metric.PropertyName" (ngModelChange)="metric.PropertyName=$event;statisticChange.emit(metric);" style="width:100%">
                                        <option></option>
                                        <option *ngFor="let p of targetTypes" [value]="p.value">{{p.label}}</option>                                    
                                    </select>   
                                </div>                                
                                <div [hidden]="target.valid || target.pristine" class="errorMessage">Target is required</div>
                            </div>
                            <div  class="col l6 s12">
                                <div class="FieldName">Object Property Value</div>
                                <div><input required style="width: 100%;" name="name" type="string" [ngModel]="metric.PropertyValue" (ngModelChange)="metric.PropertyValue=$event;statisticChange.emit(metric);" ></div>
                            </div>
                        </span>
                        <div *ngSwitchCase="statisticCheckTypes.PropertyPopulated" class="col s12">                            
                            <div class="FieldName">{{title()}}</div>
                            <div>                                
                                <select required name="object" #target="ngModel" name="Target" [ngModel]="metric.PropertyName" (ngModelChange)="metric.PropertyName=$event;statisticChange.emit(metric);" style="width:100%">
                                    <option></option>
                                    <option *ngFor="let p of targetTypes" [value]="p.value">{{p.label}}</option>                                    
                                </select>  
                            </div>
                            <div [hidden]="target.valid || target.pristine" class="errorMessage">Target is required</div>
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.Relationship" class="col s12">                                                        
                            <div class="FieldName">{{title()}}</div>
                            <div><p-multiSelect name="Target" [options]="targetTypes" [ngModel]="metric.CheckObjects" (ngModelChange)="metric.CheckObjects=$event;statisticChange.emit(metric);" [style]="{width:'100%'}"></p-multiSelect></div>
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.FusionOwnership">                            
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.ScoreRollupViaRelationship" class="col s12">
                            <div class="FieldName">{{title()}}</div>
                            <div>                                    
                                <select required name="object" #target="ngModel" name="Target" [ngModel]="metric.CheckObjectCombined" (ngModelChange)="objectChangedCheckObject($event);metric.CheckObjectCombined=$event;statisticChange.emit(metric);" style="width:100%">
                                    <option></option>
                                    <option *ngFor="let p of targetTypes" [value]="p.value">{{p.label}}</option>                                    
                                </select>  
                            </div>
                            <div [hidden]="target.valid || target.pristine" class="errorMessage">Target is required</div>
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.ScoreRollupViaOwnership" class="col s12">                            
                            <div class="FieldName">{{title()}}</div>
                            <div>                                
                                <select required name="object" #target="ngModel" name="Target" [ngModel]="metric.CheckObjectCombined" (ngModelChange)="objectChangedCheckObject($event);metric.CheckObjectCombined=$event;statisticChange.emit(metric);" style="width:100%">
                                    <option></option>
                                    <option *ngFor="let p of targetTypes" [value]="p.value">{{p.label}}</option>                                    
                                </select>
                            </div>
                            <div [hidden]="target.valid || target.pristine" class="errorMessage">Target is required</div>
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.EventMetric">
                            <div  class="col l6 s12">
                                <div class="FieldName">Valid Field Count Name</div>
                                <div><input required style="width: 100%;" #validCnt="ngModel" name="name" type="string" [ngModel]="metric.ValidField"  (ngModelChange)="metric.ValidField=$event;statisticChange.emit(metric);"></div>
                                <div [hidden]="validCnt.valid || validCnt.pristine" class="errorMessage">Valid Field Count Name is required</div>
                            </div>
                            <div  class="col l6 s12">
                                <div class="FieldName">Invalid Field Count Name</div>
                                <div><input required #invalid="ngModel" style="width: 100%;" name="name" type="string" [ngModel]="metric.InvalidField" (ngModelChange)="metric.InvalidField=$event;statisticChange.emit(metric);"></div>
                                <div [hidden]="invalid.valid || invalid.pristine" class="errorMessage">Invalid Field Count Name is required</div>
                            </div>
                            <div  class="col l6 s12">
                                <div class="FieldName">Threshold (0.00)</div>
                                <div><input required #thres="ngModel" style="width: 100%;" name="name" type="number" [ngModel]="metric.Threshold" (ngModelChange)="metric.Threshold=$event;statisticChange.emit(metric);"></div>
                                <div [hidden]="thres.valid || thres.pristine" class="errorMessage">Threshold is required</div>
                            </div>
                        </div>
                        <div *ngSwitchCase="statisticCheckTypes.PredicateMetric" class="col s12">                            
                            <div class="FieldName">{{title()}}</div>
                            <div>                                
                                <select required name="object" #target="ngModel" name="Target" [ngModel]="metric.Predicate" (ngModelChange)="metric.Predicate=$event;statisticChange.emit(metric);" style="width:100%">
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
    

export class AdminScoreTypeMetricCheckTypeInput extends BaseComponent implements OnChanges{
    @Input() metric: ScoreTypeMetric;
    @Output() statisticChange = new EventEmitter();
    targetTypes: SelectItem[] = [];    
    statisticCheckTypes = MetricCheckTypes;
    
    constructor(private statisticService: StatisticService) { super(); }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {    
        if (this.metric != null && this.metric.CheckType != null && this.metric.ObjectID > 0 && this.metric.Object != null) this.load();
    }

    title() {
        if (this.metric == null) return "";
        switch (this.metric.CheckType) {
            case MetricCheckTypes.External:
                return "";
            case MetricCheckTypes.Existence:
                return "Item To Check For";            
            case MetricCheckTypes.PredicateMetric:
                return "Predicate To Check For";
            case MetricCheckTypes.ScoreRollupViaOwnership:
                return "Owned Type To Check For";
            case MetricCheckTypes.PropertyPopulated:
                return "Field Populated";            
            case MetricCheckTypes.PropertyValueCheck:
            case MetricCheckTypes.ScoreRollupViaRelationship:
            case MetricCheckTypes.Relationship:
                return "Related Type To Check For";
        }
    }
        
    load() {        
        this.isLoading = true;
        this.statisticService
            .getMetricCheckObjects(this.metric.Object, this.metric.ObjectID, this.metric.CheckType)
            .then(targets => {
                this.targetTypes = [];
                for (let target of targets) {
                    this.targetTypes.push({
                        label: target.title, value: target.value
                    });
                }
                if (this.metric.CheckObject && this.metric.CheckObjectID) {
                    this.metric.CheckObjectCombined = this.metric.CheckObject + '|' + this.metric.CheckObjectID.toString();
                    this.statisticChange.emit(this.metric);
                }

                this.isLoading = false;
            });            
    }

    objectChangedCheckObject(event) {
        if (event) {
            let parts = event.split('|');
            if (parts.length < 2) return;

            this.metric.CheckObject = parts[0];
            this.metric.CheckObjectID = parts[1];
            this.statisticChange.emit(this.metric);
        }
        else {
            console.log("[WARNING] INVALID DATA PASSED TO OBJECTCHANGEDCHECKOBJECT METHOD.");
        }
    }

}
