import { Input, Component, EventEmitter, Output } from '@angular/core';
import { NgForm } from '@angular/forms';
import { SelectItem } from 'primeng/primeng';
import { StatisticService } from '../../../services/statistics.service';
import { ScoreTypeMetric, StatisticCheckTypes } from '../../../models/statistic.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-scoretypemetric-editor',
    template: ` 
                <header>{{action}} Metric Type</header>                
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading">
                    <form (ngSubmit)="onSubmit()" #statisticEditorForm="ngForm">                        
                        <div class="col s12">
                            <div class="FieldName">Name</div>
                            <div><input required style="width: 100%;" name="name" type="string" [(ngModel)]="metric.Name" #name="ngModel" maxlength="250"></div>                            
                            <div [hidden]="name.valid || name.pristine" class="errorMessage">Name is required</div>
                        </div>                        
                        <div class="col s12">
                            <div class="FieldName">Type to assign metric to</div>
                            <div>
                                <select required name="object" [(ngModel)]="metric.ObjectCombined" (ngModelChange)="metric.ObjectCombined=$event;objectChanged($event);" style="width:100%" #object="ngModel">
                                    <option></option>
                                    <option *ngFor="let p of sourceTypes" [value]="p.value">{{p.label}}</option>                                    
                                </select>                                
                            </div>
                            <div [hidden]="object.valid || object.pristine" class="errorMessage">Type is required</div>
                        </div>
                        <div class="col l6 s12">
                            <div class="FieldName">Maximum Score</div>
                            <div><input style="width: 100%;" name="score" type="number" [(ngModel)]="metric.MaximumScore" #score="ngModel"></div>                            
                            <div [hidden]="score.valid || score.pristine" class="errorMessage">Score is required</div>
                        </div>                                        
                        <div class="col s12">
                            <div class="FieldName">Check Type</div>
                            <div>                                
                                <select required name="CheckType" [(ngModel)]="metric.CheckType" (ngModelChange)="changeCheckType($event);" style="width:100%" #checkType="ngModel">
                                    <option></option>
                                    <option *ngFor="let p of checkTypes" [value]="p.value">{{p.label}}</option>                                    
                                </select>                                
                            </div>
                            <div [hidden]="checkType.valid || checkType.pristine"  class="errorMessage">Check Type is required</div>
                        </div>                        
                        <d3s-admin-metric-checktype-input [(metric)]="metric"></d3s-admin-metric-checktype-input>                                                                  
                        <div class="col l12 s12">
                            <div class="FieldName">Description</div>
                            <div><p-editor name="Description" [style]="{'height':'150px'}" [ngModel]="metric?.Description" (ngModelChange)="metric.Description=$event" ></p-editor></div>                            
                        </div>                        
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton type="submit" [disabled]="!statisticEditorForm.form.valid" label="Save"></button>                            
                            <button pButton type="button" (click)="closeClick.emit();" label="Close"></button>
                        </div>                    
                    </form>                           
                </div>
                `,
    providers: [StatisticService],
})

export class AdminScoreTypeMetricEditorComponent {
    @Input() scoreTypeID: number = 0;
    @Input() metricID: number = 0;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    error: any;
    metric: ScoreTypeMetric;
    checkTypes: SelectItem[] = [];
    sourceTypes: SelectItem[] = [];
    isLoading: boolean = false;
    

    constructor(private statisticService: StatisticService) {   }

    ngOnInit() {        

        if (this.metricID > 0) {
            this.isLoading = true;
            this.statisticService.getScoreTypeMetric(this.metricID).then(result => {                
                this.metric = result;                
                this.isLoading = false;
            });
        }
        else {
            console.log(this.scoreTypeID);
            this.metric = new ScoreTypeMetric();   
            this.metric.ScoreTypeID = this.scoreTypeID;        
            this.action = "New";
        }        
        this.getCheckTypes();
        this.getObjectOptions();        
    }

    getObjectOptions() {
        this.statisticService
            .getMetricObjects()
            .then(sources => {
                this.sourceTypes = [];
                for (let source of sources) {
                    this.sourceTypes.push({
                        label: source.title, value: source.value
                    });
                }                
                this.metric.ObjectCombined = this.metric.Object + '|' + this.metric.ObjectID.toString();
            })
            .catch(error => this.error = error);
    }
    
    getCheckTypes() {
        this.statisticService
            .getMetricCheckTypes()
            .then(checktypes => {
                this.checkTypes = [];
                
                for (let checktype of checktypes) {                    
                    this.checkTypes.push({
                        label: checktype.title, value: Number(checktype.value)
                    });
                }
                var check = this.metric.CheckType;
                this.metric.CheckType = 0;
                this.metric.CheckType = check;
            })
            .catch(error => this.error = error);
    }

    objectChanged(event) {        
        var info = event.split("|");
        if (info.length < 2) return;
        this.metric.Object = info[0];
        this.metric.ObjectID = Number(info[1]);     
        this.metric = _.cloneDeep(this.metric);    //cloning for child component immutability
    }

    private GetObjectName(value: string): string {
        for (var i = 0; i < this.sourceTypes.length; i++) {
            if (this.sourceTypes[i].value == value) return this.sourceTypes[i].label;
        }
        return "";
    }

    onSubmit() {
        //populate objectname        
        this.metric.ObjectName = this.GetObjectName(this.metric.ObjectCombined);

        //save the item back to the save or edit url        
        this.saveClick.emit({ metric: this.metric, action: this.metricID > 0 ? "new" : "edit" });
    }      
    
    changeCheckType(checkType) {
        this.metric.CheckType = Number(checkType);
        this.metric = _.cloneDeep(this.metric);    //cloning for child component immutability        
    }
};