import { Input, Component, EventEmitter, Output, OnInit, OnChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricsService } from '../../../services/metrics.service';
import { MetricAssetVersionConditionViewModel, MetricFieldTypeViewModel, MetricFieldTypeValueViewModel } from '../../../models/metrics.model';
import { FormMode } from '../../../models/form.model';
import { MessagesService } from '../../../services/messages.service';

@Component({
    selector: 'd3s-admin-metric-condition-list',
    template: ` 
                <header *ngIf="formMode == FormMode.Default">
                    &nbsp;
                    <d3s-tile-actions hasAdd="true" (addClick)="add()"></d3s-tile-actions>   
                </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
               <div *ngIf="!isLoading">
                    <div [ngSwitch]="formMode">
                        <div *ngSwitchCase="FormMode.Default">
                            <p-table #dt [value]="conditions" selectionMode="single" [(selection)]="selection">
                                <ng-template pTemplate="header">
                                    <tr>
                                        <th>Field</th>
                                        <th>Operator</th>
                                        <th>Value</th>
                                        <th style="width: 100px"></th>
                                    </tr>
                                </ng-template>
                                <ng-template pTemplate="body" let-item let-i="rowIndex">
                                    <tr [pSelectableRow]="item">
                                        <td>{{item.FieldTypeName}}</td>
                                        <td>{{item.OperatorText}}</td>
                                        <td>{{item.ValuesText}}</td>
                                        <td>
                                        <div class="RowTools">                                
                                            <a style="cursor:pointer;" (click)="selection = item; edit(i)"><i class="fa fa-pencil"></i></a>   
                                            <a style="cursor:pointer;" (click)="selection = item; delete(i)"><i class="fa fa-trash-o"></i></a>   
                                        </div> 
                                        </td>
                                    </tr>
                                </ng-template>
                            </p-table>
                        </div>
                        <div *ngSwitchCase="FormMode.Adding">
                            <d3s-admin-metric-condition-editor 
                                [uid]="metricUid" 
                                [metricConditionEditorFieldTypes]="metricConditionListFieldTypes"
                                [assetTypeUid]="assetTypeUid"
                                [(condition)]="selection"
                                (onCancel)="formMode = FormMode.Default; formModeChange.emit(formMode);"
                                (onSave)="formMode = FormMode.Default; formModeChange.emit(formMode); save($event);">
                            </d3s-admin-metric-condition-editor>
                        </div>
                        <div *ngSwitchCase="FormMode.Editing">
                            <d3s-admin-metric-condition-editor 
                                [uid]="metricUid" 
                                [metricConditionEditorFieldTypes]="metricConditionListFieldTypes"
                                [assetTypeUid]="assetTypeUid"
                                [(condition)]="selection"
                                (onCancel)="formMode = FormMode.Default; formModeChange.emit(formMode);"
                                (onSave)="formMode = FormMode.Default; formModeChange.emit(formMode); save($event);">
                            </d3s-admin-metric-condition-editor>
                        </div>
                        <div *ngSwitchCase="FormMode.Deleting">
                            <div class="row">
                                <div class="col s12">
                                    Are you sure you want to delete this condition?
                                </div>
                            </div>
                            <div class="row">
                                <div class="col s12" style="padding-top: 15px">
                                    <button pButton type="button" label="Delete" (click)="confirmDelete()" style="float: right"></button>
                                    <button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default; formModeChange.emit(formMode);" style="float: right"></button>
                                </div>
                            </div> 
                        </div>
                    </div>    
                </div>
                `,
    providers: [MetricsService]
})

export class AdminMetricConditionListComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() metricUid: string;
    @Input() assetTypeUid: string;
    @Input() conditions = [];
    @Input() metricConditionListFieldTypes: MetricFieldTypeViewModel[] = [];

    @Output() editClick = new EventEmitter();
    @Output() deleteClick = new EventEmitter();
    @Output() addClick = new EventEmitter();
    @Output() conditionsChange = new EventEmitter();

    @Output() formModeChange = new EventEmitter();
    
    private selection: MetricAssetVersionConditionViewModel = null;
    private selectedIndex = -1;
    private formMode = FormMode.Default;
    FormMode = FormMode;

    private operators = [
        { value: 'eq', label: '=' },
        { value: 'neq', label: '!=' },
        { value: 'lt', label: '<' },
        { value: 'lte', label: '<=' },
        { value: 'gt', label: '>' },
        { value: 'gte', label: '>=' },
    ];

    constructor(private metricsService: MetricsService, protected messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges() {
        this.load();
    }

    load(): Promise<any> {
        this.isLoading = true;

        this.conditions.forEach(c => {
            c.OperatorText = this.operators.find(o => o.value == c.Operator).label;

            let field = this.metricConditionListFieldTypes.find(f => f.ID == c.FieldTypeID);
            if (field != null) {
                c.FieldTypeName = field.Name;

                if (field.Values) {
                    if (field.Values.length > 0) {
                        let valueModel: MetricFieldTypeValueViewModel = field.Values.find(o => o.Value == c.Values);
                        valueModel = field.Values.find(o => o.Value == c.Values);
                        if (valueModel) {
                            c.ValuesText = valueModel.Text;
                        }
                    }
                }

                if (!c.ValuesText) {
                    c.ValuesText = c.Values;
                }
            }
        });
        this.isLoading = false;

        return Promise.resolve();
    }

    add() {
        this.selection = new MetricAssetVersionConditionViewModel();
        this.selection.IsEditMode = false;
        //this.selection. = this.mapId;
        this.formMode = FormMode.Adding;
        this.formModeChange.emit(this.formMode);
    }

    edit(e: any) {
        this.selection.IsEditMode = true;
        this.formMode = FormMode.Editing;
        this.formModeChange.emit(this.formMode);
    }

    delete(i: number) {
        this.selectedIndex = i;
        this.formMode = FormMode.Deleting;
        this.formModeChange.emit(this.formMode);
    }

    confirmDelete() {
        this.conditions.splice(this.selectedIndex, 1).slice();
        this.conditionsChange.emit(this.conditions);
        this.formMode = FormMode.Default;
        this.formModeChange.emit(this.formMode);
    }

    save(e: MetricAssetVersionConditionViewModel) {
        e.OperatorText = this.operators.find(o => o.value == e.Operator).label;

        if (!e.IsEditMode) {
            this.conditions.push(e);
        }

        this.conditions.slice();
        this.conditionsChange.emit(this.conditions);
        this.formMode = FormMode.Default;
        this.formModeChange.emit(this.formMode);
    }
};