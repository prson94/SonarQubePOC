import { Input, Component, EventEmitter, Output, OnChanges, SimpleChange } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/fusion.service';
import { MessagesService } from '../../services/messages.service';
import { FusionConfigurationDetails, FusionQueryAttributeType  } from '../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-query-list',
    template: ` 
                <div class="col s12">
                <div class="tile tile-detail">
                    <header *ngIf="!isLoading && !showDelete && !showEditor">Fusion Queries For {{fusion?.Name}}    
                            <d3s-tile-actions [hasAdd]="true" (addClick)="showAddQuery()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                    </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading && !showDelete && !showEditor">                        
                        <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
                        <p-dataTable #dt [globalFilter]="gb" scrollable="true" scrollWidth="100%" [value]="queries" selectionMode="single" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="defaultPagingOptions" [(selection)]="selected" (onRowDblclick)="selected=$event.data" >
                            <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>                            
                            <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="Uri" header="Uri" [sortable]="false" [filter]="!showSimpleFilter">
                                <template let-query="rowData" pTemplate type="body">
                                    <a target="_blank" href="/services/fusion/{{query.FusionID}}/{{query.ID}}/data?metadata=true">/services/fusion/{{query.FusionID}}/{{query.ID}}/data?metadata=true</a>                                        
                                </template>
                            </p-column>
                            <p-column [style]="{width:'40px'}">
                                <template let-query="rowData" pTemplate type="body">
                                    <div class="RowTools">
                                        <a style="cursor:pointer;" (click)="selected=query;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                    </div>
                                </template>
                            </p-column>                            
                            <p-column  [style]="{width:'40px'}">
                                <template let-query="rowData" pTemplate type="body">
                                    <div class="RowTools">                                
                                        <a style="cursor:pointer;" (click)="selected=query;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                    </div>
                                </template>
                            </p-column>                            
                        </p-dataTable>      
                    </span>
                    <d3s-delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the query [' + [selected?.Name] + ']?'"                                         
                                (onCancel)="showDelete=false;"
                    ></d3s-delete-form>  
                    <d3s-fusion-query-attribute-editor *ngIf="showEditor" [query]="selected" (saveClick)="doSave($event);" (closeClick)="showEditor=false;"></d3s-fusion-query-attribute-editor> 
                </div>
                </div>
                <div class="col s12" *ngIf="!isLoading && !showDelete && !showEditor && selected">
                    <div class="tile tile-detail">                                              
                        <d3s-field-definition-tile  [objectType]="'FusionQueryAttributeType'" [objectID]="selected.ID" ></d3s-field-definition-tile>
                    </div>
                </div>
          `,
    providers: [FusionService],
})

export class FusionQueryListComponent extends BaseComponent implements OnChanges {
    @Input() fusion: FusionConfigurationDetails;
    @Output() treeRequiresUpdate = new EventEmitter();

    private queries: FusionQueryAttributeType[] = [];
    private selected: FusionQueryAttributeType;

    private showDelete: boolean = false;
    private showEditor: boolean = false;

    public theDeleteCallback: Function;

    constructor(private fusionService: FusionService, private messagesService: MessagesService) {
        super();
        this.theDeleteCallback = this.deleteQuery.bind(this);
    }
        
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['fusion'] && this.fusion) this.load();
    }

    private load() {
        this.isLoading = true;
        this.fusionService.getFusionQueryAttributeTypes(this.fusion.FusionTypeID, this.fusion.ID).
            then(result => {
                this.queries = result;
                this.selected = this.queries.length > 0 ? this.queries[0] : null;
                this.isLoading = false;
            });
    }

    private showAddQuery() {
        this.selected = null;
        this.showEditor = true;
    }

    private doSave(data) {
        data.query.FusionID = this.fusion.ID;
        this.fusionService.saveQueryAttributeType(data.query)        
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    if (data.query.ID == undefined) {
                        data.query.ID = Number(result.id);
                        this.queries[this.queries.length] = data.query;
                        this.treeRequiresUpdate.emit();
                    }
                    else {
                        let index = this.queries.findIndex(x => x.ID == data.query.ID);
                        if (index >= 0 && index < this.queries.length)
                            this.queries[index] = data.query;
                    }
                    this.selected = data.query;
                }
                this.showEditor = false;
            });
    }

    private deleteQuery(id: number) {
        this.fusionService.deleteFusionQuery(id).
            then(result => {
                this.showMessageForResult(this.messagesService, result);
                //remove the template with this id from the grid
                if (result.type != 'error') {
                    this.queries = this.queries.filter(x => x.ID != id);
                    this.selected = this.queries.length > 0 ? this.queries[0] : null;
                }
                this.showDelete = false;
                this.treeRequiresUpdate.emit();
            });
    }
}