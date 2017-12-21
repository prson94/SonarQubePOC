import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricsService } from '../../../services/metrics.service';
import { Condition } from '../../../models/metrics.model';
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
                            <p-dataTable #dt [value]="conditions" selectionMode="single"  [(selection)]="selection">
                                <p-column field="fieldName" header="Field"></p-column>
                                <p-column field="operatorName" header="Operator">
                                </p-column>
                                <p-column field="Value" header="Value">
                                </p-column> 
                                <p-column field="andOrName" header="And Or">
                                </p-column>                          
                                <p-column  [style]="{width:'40px'}">
                                    <ng-template let-condition="rowData" pTemplate type="body">
                                        <div class="RowTools">                                
                                            <a style="cursor:pointer;" (click)="selection = condition; delete()"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </ng-template>
                                </p-column> 
                            </p-dataTable>  
                        </div>
                        <div *ngSwitchCase="FormMode.Adding">
                            <d3s-admin-metric-condition-editor 
                                [mapId]="mapId" 
                                [fieldId]="0"
                                (onCancel)="formMode = FormMode.Default; formModeChange.emit(formMode);"
                                (onSave)="formMode = FormMode.Default; formModeChange.emit(formMode); load()">
                            </d3s-admin-metric-condition-editor>
                        </div>
                        <div *ngSwitchCase="FormMode.Editing">
                            <d3s-admin-metric-condition-editor 
                                [mapId]="mapId" 
                                [fieldId]="selection?.FieldTypeID"
                                (onCancel)="formMode = FormMode.Default; formModeChange.emit(formMode);"
                                (onSave)="formMode = FormMode.Default; formModeChange.emit(formMode); load()">
                            </d3s-admin-metric-condition-editor>
                        </div>
                        <div *ngSwitchCase="FormMode.Deleting">
                            <d3s-delete-form
                                [uri]="'form/MetricCondition?mapId=' + selection?.MapID + '&fieldTypeId=' + selection?.FieldTypeID"
                                [method]="'delete'"
                                [prompt]="'Are you sure you want to delete this condition?'"                                         
                                (onCancel)="formMode = FormMode.Default"
                                (onDeleteSuccess)="formMode = FormMode.Default; formModeChange.emit(formMode);load();"
                                (onDeleteFail)="formMode = FormMode.Default; formModeChange.emit(formMode);">
                            </d3s-delete-form> 
                        </div>
                    </div>    
                </div>
                `,
    providers: [MetricsService]
})

export class AdminMetricConditionListComponent extends BaseComponent implements OnInit {
    @Input() mapId: number;
    @Output() editClick = new EventEmitter();
    @Output() deleteClick = new EventEmitter();
    @Output() addClick = new EventEmitter();

    @Output() formModeChange = new EventEmitter();

    private conditions = [];
    private selection = null;
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

    private andOr = [
        { value: 'a', label: 'And' },
        { value: 'o', label: 'Or' },
    ];

    constructor(private metricsService: MetricsService, protected messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    load(): Promise<any> {
        this.isLoading = true;
        return this.metricsService.getConditions(this.mapId)
            .then(r => {
                this.conditions = r;
                this.conditions.forEach(c => {
                    c.operatorName = this.operators.find(o => o.value == c.Operator).label;
                    c.andOrName = this.andOr.find(o => o.value == c.AndOr).label;
                })
                //console.log(this.items, r);
                this.isLoading = false;
            });
    }

    add() {
        this.selection = null;
        this.formMode = FormMode.Adding;
        this.formModeChange.emit(this.formMode);
    }

    edit(e: any) {
        this.formMode = FormMode.Editing;
        this.formModeChange.emit(this.formMode);
    }

    delete(e: any) {
        this.formMode = FormMode.Deleting;
        this.formModeChange.emit(this.formMode);
    }
};