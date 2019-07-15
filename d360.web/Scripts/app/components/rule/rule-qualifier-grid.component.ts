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
                                    <p-table  #dt [value]="qualifierTypes" selectionMode="single" [rows]="25" paginator="true" [(selection)]="selectedQualifierType" [rowsPerPageOptions]="defaultPagingOptions"  [pageLinks]="3" >
                                        <ng-template pTemplate="header">
                                            <tr>
                                                <th>Name</th>
                                                <th>Resolution Object</th>
                                                <th>Resolution Field</th>
                                                <th></th>
                                            </tr>
                                        </ng-template>
                                        <ng-template pTemplate="body" let-item let-i="rowIndex">
                                            <tr [pSelectableRow]="item">
                                                <td>{{item.Name}}</td>
                                                <td>{{item.ResolutionObjectName}}</td>
                                                <td>{{item.ResolutionFieldTypeName}}</td>
                                                <td>
                                                        <div class="RowTools">
                                                            <a *ngIf="i > 0" (click)="move(item,true)"><i class="fa fa-caret-up"></i></a>
                                                            <a *ngIf="i < (qualifierTypes.length - 1)" (click)="move(item,false)"><i class="fa fa-caret-down"></i></a>
                                                            <a (click)="edit(item)"><i class="fa fa-pencil"></i></a>
                                                            <a (click)="delete(item)"><i class="fa fa-trash-o"></i></a>
                                                        </div>
                                                </td>
                                            </tr>
                                        </ng-template>
                                        <ng-template pTemplate="summary">
                                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                        </ng-template>
                                    </p-table>    
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
            .subscribe(r => {
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

    move(item: QualifierType, up: boolean) {
        this.selectedQualifierType = item;
        this.qualifierService.putMoveQualifierType(this.selectedQualifierType.ID, up)
            .subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                this.load();
            });
    }
    
    add() {
        this.formMode = FormMode.Adding;
    }
}