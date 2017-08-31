import { Component, OnInit, Input, SimpleChange, OnChanges} from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { QualifierService } from '../../services/qualifier.service';
import { MessagesService } from '../../services/messages.service';
import { QualifierType } from '../../models/qualifier.model';
import { FormMode } from '../../models/form.model';

@Component({
    selector: 'd3s-rule-qualifier-grid',
    template: `         <span [ngSwitch]="formMode">
                                <span *ngSwitchDefault>
                                    <header>
                                        <span *ngIf="showTitle; else noTitle">Rule Qualifiers</span>
                                        <ng-template #noTitle>&nbsp;</ng-template>
                                        <d3s-tile-actions hasAdd="true" (addClick)="add()"></d3s-tile-actions>
                                    </header>
                                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                                    <p-dataTable *ngIf="!isLoading" [value]="qualifierTypes" selectionMode="single"  rows="25" paginator="true" [(selection)]="selectedQualifierType" [rowsPerPageOptions]="defaultPagingOptions" >                            
                                        <p-column field="Name" header="Name"></p-column>
                                        <p-column field="ResolutionObjectName" header="Resolution Object"></p-column>
                                        <p-column field="ResolutionFieldTypeName" header="Resolution Field"></p-column>
                                        <p-column>
                                            <ng-template let-i="rowIndex" let-item="rowData" pTemplate="body">
                                                <div class="RowTools">
                                                    <a *ngIf="i > 0" (click)="moveUp(item)"><i class="fa fa-caret-up"></i></a>
                                                    <a *ngIf="i < (qualifierTypes.length - 1)" (click)="moveDown(item)"><i class="fa fa-caret-down"></i></a>
                                                    <a (click)="edit(item)"><i class="fa fa-pencil"></i></a>
                                                    <a (click)="delete(item)"><i class="fa fa-trash-o"></i></a>
                                                </div>
                                            </ng-template>
                                        </p-column>
                                    </p-dataTable>     
                                </span>
                                <span *ngSwitchCase="FormMode.Adding">
                                    <d3s-rule-qualifier-editor [implementationId]="implementationId" (onClose)="formMode = FormMode.Default" (onSave)="formMode = FormMode.Default; load()"></d3s-rule-qualifier-editor>
                                </span>
                                <span *ngSwitchCase="FormMode.Editing">
                                    <d3s-rule-qualifier-editor [qualifier]="selectedQualifierType" (onClose)="formMode = FormMode.Default" (onSave)="formMode = FormMode.Default; load()"></d3s-rule-qualifier-editor>
                                </span>
                                <span *ngSwitchCase="FormMode.Deleting">
                                    <header>Delete Qualifier</header>
                                    <d3s-delete-form
                                        [uri]="'form/DeleteQualifierType?id=' + selectedQualifierType.ID"
                                        (onDeleteSuccess)="load()"
                                        (onDeleteComplete)="formMode = FormMode.Default"
                                        (onCancel)="formMode = FormMode.Default"
                                        method="delete"
                                        prompt="Are you sure you want to delete this qualifier type?" >
                                    </d3s-delete-form>
                                </span>
                            </span>
                
          `,
    providers: [QualifierService],
})

export class RuleQualifierGridComponent extends BaseComponent implements OnInit { //, OnChanges
    @Input() implementationId: number;    
    @Input() showTitle: boolean = true;
    
    private qualifierTypes: QualifierType[] = [];
    private selectedQualifierType;
    private formMode: FormMode = FormMode.Default;
    
    FormMode = FormMode;

    constructor(
        private qualifierService: QualifierService,
        private messagesService: MessagesService,        
    ) {
        super();
    }
    
    ngOnInit() {        
        if (this.implementationId > 0) {
            this.load();
        }
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['implementationId'] && this.implementationId) {            
            this.load();
        }
    }
    
    private load() {
        this.isLoading = true;
        this.qualifierService.getQualifierTypes(this.implementationId)
            .then(r => {
                this.qualifierTypes = r;
                if (this.qualifierTypes != null)
                    this.selectedQualifierType = this.qualifierTypes[0];
                this.isLoading = false;
            });
    }

    edit(item: QualifierType) {
        this.selectedQualifierType = item;
        this.formMode = FormMode.Editing;
    }

    delete(item: QualifierType) {
        this.selectedQualifierType = item;
        this.formMode = FormMode.Deleting;
    }

    moveUp(item: QualifierType) {
        this.selectedQualifierType = item;
        this.qualifierService.putMoveQualifierType(this.selectedQualifierType.ID, true)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.load();
            });
    }

    moveDown(item: QualifierType) {
        this.selectedQualifierType = item;
        this.qualifierService.putMoveQualifierType(this.selectedQualifierType.ID, false)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.load();
            });
    }

    add() {
        this.formMode = FormMode.Adding;
    }
}