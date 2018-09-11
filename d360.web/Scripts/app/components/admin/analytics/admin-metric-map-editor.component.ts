import { Input, Component, EventEmitter, Output, OnInit, ViewChild, ElementRef } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { Map, MapForm } from '../../../models/metrics.model';
import { BaseComponent } from '../../shared/base.component';
import { MessagesService } from '../../../services/messages.service';
import { FormMode } from "../../../models/form.model";
import { FormHelpers } from '../../../static/form-helpers';

@Component({
    selector: 'd3s-admin-metric-map-editor',
    template: ` 
                <header>{{verb}} Mapping</header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading">
                    <div class="row">
                        <div class="col s6">
                            <div class="FieldName">
                                Weight
                            </div>
                            <div class="directions">Weight must be a value between 0 and 1</div>
                            <div>
                                <input #weight type="text" style="width: 95%" [ngModel]="model.Map.Weight" (ngModelChange)="model.Map.Weight = clamp($event, 0, 1, 3)" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col s6">
                            <div class="FieldName">
                                Measure
                            </div>
                            <div>
                                <p-dropdown 
                                    [filter]="true" 
                                    filterBy="label,value.Text"  
                                    optionLabel="Text" 
                                    [options]="model.Items" 
                                    [ngModel]="metricItem"
                                    (ngModelChange)="changeMetricItem($event)"
                                    [style]="{ 'width' : '95%' }">
                                </p-dropdown>
                            </div>
                        </div>
                        <div class="col s6">
                            <div class="FieldName">
                                Object Type
                            </div>
                            <div>
                                <p-dropdown 
                                    [filter]="true" 
                                    filterBy="label,value.Text"  
                                    optionLabel="Text" 
                                    dataKey="Value"
                                    [options]="model.AssetTypes" 
                                    [ngModel]="assetType" 
                                    (ngModelChange)="changeAssetType($event)" 
                                    [style]="{ 'width' : '95%' }">
                                </p-dropdown>
                            </div>
                        </div>   
                        <div class="col s6">
                            <div class="FieldName">
                                Effective Date
                            </div>
                            <div>
                                <p-calendar [(ngModel)]="model.Map.EffectiveDate" [showTime]="false" [dateFormat]="getLocaleDateString()" [style]="{'width':'100%'}" [inputStyle]="{'width':'95%'}"></p-calendar>
                            </div>
                        </div>
                        <div class="col s12" *ngIf="model?.Map?.AssetTypeID != null && model?.Map?.AssetTypeID > 0">
                            <div class="FieldName">
                                Conditions
                            </div>
                            <div>
                                <d3s-admin-metric-condition-list [(conditions)]="model.Conditions" [mapId]="model?.Map?.ID || 0" [assetTypeId]="model.Map.AssetTypeID" (formModeChange)="conditionFormMode = $event">
                                </d3s-admin-metric-condition-list>
                            </div>
                        </div> 
                        <div class="col s12" style="padding-top: 15px">
                            <button pButton type="button" label="Save" [disabled]="!valid() || conditionFormMode != FormMode.Default" (click)="save()"></button>
                            <button pButton type="button" label="Cancel" (click)="cancel()" [disabled]="conditionFormMode != FormMode.Default"></button>
                        </div>
                    </div>
                </div>
                `,
    providers: [MetricsService]
})

export class AdminMetricMapEditorComponent extends BaseComponent implements OnInit {
    @Input() mapId: number = -1;
    @Input() groupId: number = -1;
    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    @ViewChild('weight') weightInput: ElementRef;

    verb = "Add";

    model: MapForm = null;
    assetType: any = null;
    metricItem: any = null;
    conditionFormMode = FormMode.Default;
    FormMode = FormMode;

    constructor(private metricsService: MetricsService, protected messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    load() {
        if (this.mapId > 0) {
            this.verb = "Edit"
            this.isLoading = true;
            this.metricsService.getMapFormModel(this.mapId)
                .then(r => {
                    this.model = r;
                    this.assetType = this.model.AssetTypes.find(o => o.Value == r.Map.AssetTypeID.toString());
                    this.metricItem = this.model.Items.find(i => i['Value'] == this.model.Map.ItemID);

                    //add timezone offset
                    if (this.model.Map.EffectiveDate != null) {
                        this.model.Map.EffectiveDate = new Date(<string>this.model.Map.EffectiveDate);
                        this.model.Map.EffectiveDate.setMinutes(this.model.Map.EffectiveDate.getMinutes() + this.model.Map.EffectiveDate.getTimezoneOffset());
                    }
                        
                    this.isLoading = false;
                });
        } else {
            this.verb = "Add";
            this.model = new MapForm();
            this.model.Map = new Map();
            this.model.Map.GroupID = this.groupId;
            this.isLoading = true;
            this.metricsService.getMapFormModel(-1)
                .then(r => {
                    this.model.Items = r.Items;
                    this.model.AssetTypes = r.AssetTypes;

                    this.isLoading = false;
                });

        }
    }

    valid() {
        let valid = true;

        if (this.model == null || this.model.Map == null) {
            valid = false;
        } else {
            if (this.model.Map.AssetTypeID == null)
                valid = false;
            if (this.model.Map.ItemID == null || this.model.Map.ItemID < 1)
                valid = false;
            if ((<any>this.model.Map).Weight == "" || this.model.Map.Weight == null || this.model.Map.Weight < 0 || this.model.Map.Weight > 1)
                valid = false;
            if (this.model.Map.EffectiveDate == null)
                valid = false;
        }

        return valid;
    }

    save() {
        this.isLoading = true;

        if (this.model.Map.EffectiveDate != null)
            this.model.Map.EffectiveDate = new Date(<string>this.model.Map.EffectiveDate).toISOString();

        this.metricsService.saveMap(this.model)
            .then(r => {
                this.showMessageForResult(this.messagesService, r);
                this.isLoading = false;
                this.onSave.emit();
            });
    }

    cancel() {
        this.onCancel.emit();
    }

    changeAssetType(e: any) {
        this.assetType = e;
        if (this.assetType != null && this.assetType.Value != null) {
            this.model.Map.AssetTypeID = +this.assetType.Value;
        } else {
            this.model.Map.AssetTypeID = null;
        }
    }

    changeMetricItem(e: any) {
        this.metricItem = e;
        if (e != null) {
            this.model.Map.ItemID = isNaN(+e.Value) ? null : +e.Value;
        } else {
            this.model.Map.ItemID = null;
        }
    }

    getUTCDate(date: Date): Date {
        date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
        return date;
    }

    getLocaleDateString(): string {
        return FormHelpers.getLocaleDateString();
    }

    private clamp(val: any, min: number, max: number, precision: number): any {
        let newVal = FormHelpers.clamp(val, min, max, precision);

        if (this.weightInput != null && this.weightInput.nativeElement != null)
            this.weightInput.nativeElement.value = newVal;

        return newVal;
    }
};