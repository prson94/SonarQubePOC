import { Input, Component, EventEmitter, Output, OnInit, OnChanges } from '@angular/core';
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
                                    <ng-template let-condition="rowData" let-i="rowIndex" pTemplate type="body">
                                        <div class="RowTools">                                
                                            <a style="cursor:pointer;" (click)="selection = condition; delete(i)"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </ng-template>
                                </p-column> 
                            </p-dataTable>  
                        </div>
                        <div *ngSwitchCase="FormMode.Adding">
                            <d3s-admin-metric-condition-editor 
                                [mapId]="mapId" 
                                [fieldId]="0"
                                [objectType]="objectType"
                                [objectId]="objectId"
                                [condition]="selection"
                                (onCancel)="formMode = FormMode.Default; formModeChange.emit(formMode);"
                                (onSave)="formMode = FormMode.Default; formModeChange.emit(formMode); save($event);">
                            </d3s-admin-metric-condition-editor>
                        </div>
                        <div *ngSwitchCase="FormMode.Editing">
                            <d3s-admin-metric-condition-editor 
                                [mapId]="mapId" 
                                [fieldId]="selection?.FieldTypeID"
                                [objectType]="objectType"
                                [objectId]="objectId"
                                [condition]="selection"
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
    @Input() mapId: number;
    @Input() objectType: string;
    @Input() objectId: number;
    @Input() conditions = [];
    @Output() editClick = new EventEmitter();
    @Output() deleteClick = new EventEmitter();
    @Output() addClick = new EventEmitter();
    @Output() conditionsChange = new EventEmitter();

    @Output() formModeChange = new EventEmitter();
    
    private selection = null;
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

    ngOnChanges() {
        this.load();

    }

    load(): Promise<any> {
        this.isLoading = true;

        this.conditions.forEach(c => {
            c.operatorName = this.operators.find(o => o.value == c.Operator).label;
            c.andOrName = this.andOr.find(o => o.value == c.AndOr).label;
        });
        this.isLoading = false;

        return Promise.resolve();
    }

    add() {
        this.selection = new Condition();
        this.selection.MapID = this.mapId;
        this.formMode = FormMode.Adding;
        this.formModeChange.emit(this.formMode);
    }

    edit(e: any) {
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

    save(e: any) {
        e.operatorName = this.operators.find(o => o.value == e.Operator).label;
        e.andOrName = this.andOr.find(o => o.value == e.AndOr).label;

        this.conditions.push(e);

        this.conditions.slice();
        this.conditionsChange.emit(this.conditions);
        this.formMode = FormMode.Default;
        this.formModeChange.emit(this.formMode);
    }
};