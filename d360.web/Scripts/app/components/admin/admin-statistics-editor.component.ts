///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output } from '@angular/core';
import { NgForm } from '@angular/forms';
import { SelectItem } from 'primeng/primeng';
import { StatisticService } from '../../services/index';
import { StatisticType, StatisticCheckTypes } from '../../models/statistic.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-statistic-editor',
    template: ` 
                <header>{{action}} Analytic Type</header>                
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading">
                    <form (ngSubmit)="onSubmit()" #statisticEditorForm="ngForm">                        
                        <div class="col s12">
                            <div class="FieldName">Name</div>
                            <div><input required style="width: 100%;" name="name" type="string" [(ngModel)]="editedStatistic.Name" #name="ngModel" maxlength="250"></div>                            
                            <div [hidden]="name.valid || name.pristine" class="errorMessage">Name is required</div>
                        </div>                        
                        <div class="col s12">
                            <div class="FieldName">Type to assign analytic to</div>
                            <div>
                                <select required name="object" [ngModel]="editedStatistic.ObjectCombined" (ngModelChange)="editedStatistic.ObjectCombined=$event;objectChanged($event);" style="width:100%" #object="ngModel">
                                    <option></option>
                                    <option *ngFor="let p of sourceTypes" [value]="p.value">{{p.label}}</option>                                    
                                </select>                                
                            </div>
                            <div [hidden]="object.valid || object.pristine" class="errorMessage">Type is required</div>
                        </div>
                        <div class="col l6 s12">
                            <div class="FieldName">Part of Scoring?</div>
                            <div><input name="partOfScore" type="checkbox" [(ngModel)]="editedStatistic.PartOfScore"></div>                            
                        </div>                                        
                        <div class="col l6 s12" *ngIf="editedStatistic?.PartOfScore">
                            <div class="FieldName">Score</div>
                            <div><input style="width: 100%;" name="score" type="number" [(ngModel)]="editedStatistic.Score" #score="ngModel"></div>                            
                            <div [hidden]="score.valid || score.pristine" class="errorMessage">Score is required</div>
                        </div>                                        
                        <div class="col s12">
                            <div class="FieldName">Check Type</div>
                            <div>                                
                                <select required name="CheckType" [(ngModel)]="editedStatistic.CheckType" (ngModelChange)="changeCheckType($event);" style="width:100%" #checkType="ngModel">
                                    <option></option>
                                    <option *ngFor="let p of checkTypes" [value]="p.value">{{p.label}}</option>                                    
                                </select>                                
                            </div>
                            <div [hidden]="checkType.valid || checkType.pristine"  class="errorMessage">Check Type is required</div>
                        </div>                        
                        <d3s-admin-statistic-checktype-input [(statistic)]="editedStatistic"></d3s-admin-statistic-checktype-input>                                                                  
                        <div class="col l12 s12">
                            <div class="FieldName">Description</div>
                            <div><p-editor name="Description" [style]="{'height':'150px'}" [ngModel]="editedStatistic?.Description" (ngModelChange)="editedStatistic.Description=$event" ></p-editor></div>                            
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

export class AdminStatisticEditor {
    @Input() statisticID: number = 0;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    error: any;
    editedStatistic: StatisticType;
    checkTypes: SelectItem[] = [];
    sourceTypes: SelectItem[] = [];
    isLoading: boolean = false;
    

    constructor(private statisticService: StatisticService) {   }

    ngOnInit() {        

        if (this.statisticID > 0) {
            this.isLoading = true;
            this.statisticService.getStatistic(this.statisticID).then(result => {                
                this.editedStatistic = result;                
                this.isLoading = false;
            });
        }
        else {
            this.editedStatistic = new StatisticType();           
            this.action = "New";
        }        
        this.getCheckTypes();
        this.getObjectOptions();        
    }

    getObjectOptions() {
        this.statisticService
            .getStatisticObjects()
            .then(sources => {
                this.sourceTypes = [];
                for (let source of sources) {
                    this.sourceTypes.push({
                        label: source.title, value: source.value
                    });
                }                
                this.editedStatistic.ObjectCombined = this.editedStatistic.Object + '|' + this.editedStatistic.ObjectID.toString();
            })
            .catch(error => this.error = error);
    }
    
    getCheckTypes() {
        this.statisticService
            .getStatisticCheckTypes()
            .then(checktypes => {
                this.checkTypes = [];
                
                for (let checktype of checktypes) {                    
                    this.checkTypes.push({
                        label: checktype.title, value: Number(checktype.value)
                    });
                }
                var check = this.editedStatistic.CheckType;
                this.editedStatistic.CheckType = 0;
                this.editedStatistic.CheckType = check;
            })
            .catch(error => this.error = error);
    }

    objectChanged(event) {        
        var info = event.split("|");
        if (info.length < 2) return;
        this.editedStatistic.Object = info[0];
        this.editedStatistic.ObjectID = Number(info[1]);     
        this.editedStatistic = _.cloneDeep(this.editedStatistic);    //cloning for child component immutability
    }

    private GetObjectName(value: string): string {
        for (var i = 0; i < this.sourceTypes.length; i++) {
            if (this.sourceTypes[i].value == value) return this.sourceTypes[i].label;
        }
        return "";
    }

    onSubmit() {
        //populate objectname        
        this.editedStatistic.ObjectName = this.GetObjectName(this.editedStatistic.ObjectCombined);

        //save the item back to the save or edit url        
        this.saveClick.emit({ statistic: this.editedStatistic, action: this.statisticID > 0 ? "new" : "edit" });
    }      
    
    changeCheckType(checkType) {
        this.editedStatistic.CheckType = Number(checkType);
        this.editedStatistic = _.cloneDeep(this.editedStatistic);    //cloning for child component immutability        
    }
};