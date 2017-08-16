import { Input, Output, Component, EventEmitter, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FusionConfiguration, FusionType, FusionFilter, FusionAttributeTypeCustomQuery } from '../../../models/fusion.model';
import { FusionService } from '../../../services/fusion.service';
import { BaseComponent } from '../../shared/base.component';
import { MessagesService } from '../../../services/messages.service';
 
@Component({
    selector: 'd3s-fusion-attribute-type-custom-query',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading && !showEditor && !showDelete">            
            <header>
                Override Queries For Attribute Types
                <d3s-tile-actions hasClose="true" (closeClick)="onClose.emit()" [hasAdd]="true" (addClick)="selected=null;showEditor=true;" [hasFilterMode]="false" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
            </header>
            <p-dataTable #dt scrollable="true" scrollWidth="100%" [value]="customqueries" [rows]="20" [paginator]="true" [(selection)]="selected">
                <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                <p-column field="FusionAttributeType" header="Type"></p-column>
                <p-column [style]="{width:'40px'}">
                    <ng-template let-override="rowData" pTemplate type="body">
                        <div class="RowTools">
                            <a style="cursor:pointer;" (click)="selected=override;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                        </div>
                    </ng-template>
                </p-column>                            
                <p-column  [style]="{width:'40px'}">
                    <ng-template let-override="rowData" pTemplate type="body">
                        <div class="RowTools">                                
                            <a style="cursor:pointer;" (click)="selected=override;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                        </div>
                    </ng-template>
                </p-column>                          
            </p-dataTable>            
        </div>
        <d3s-fusion-attribute-type-custom-query-editor *ngIf="showEditor" 
            [fusionId]="fusionId"
            [selection]="selected" 
            [existingOverrides]="customqueries"
            (saveClick)="saveOverride($event)" 
            (closeClick)="closeEditor()">
        </d3s-fusion-attribute-type-custom-query-editor>
        <d3s-delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the selected query override?'"                                         
                    (onCancel)="showDelete=false;">
        </d3s-delete-form> 
    `,
    providers: [FusionService]
})

export class FusionAttributeTypeCustomQueryComponent extends BaseComponent implements OnInit {
    @Input() fusionId: number;
    @Input() fusionTypeId: number;
    @Output() onClose = new EventEmitter();

    showDelete: boolean = false;
    showEditor: boolean = false;
    theDeleteCallback: Function;
        
    customqueries: FusionAttributeTypeCustomQuery[];
    selected: FusionAttributeTypeCustomQuery;

    constructor(private fusionService: FusionService,
            private messagesService: MessagesService
        )
    {
        super();
        this.theDeleteCallback = this.deleteOverride.bind(this);        
    }
        
    ngOnInit() {
        this.load();
    }

    load(): void {
        this.isLoading = true;
        this.fusionService.getFusionAttributeTypeCustomQueries(this.fusionTypeId, this.fusionId)
            .then(data => {
                this.customqueries = data;
                this.isLoading = false;
            });
    }

    private closeEditor(): void {
        this.showEditor = false;
    }

    private saveOverride(event): void {
        event.override.FusionID = this.fusionId;
        this.fusionService.saveFusionAttributeTypeCustomQuery(event.override)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.load();
                this.showEditor = false;
            });
    }

    private deleteOverride(id: number): void {
        this.fusionService.deleteFusionAttributeTypeCustomQuery(id)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                if (result.type != 'error') {
                    this.customqueries = this.customqueries.filter(x => x.ID != id);
                }
            });
    }
}