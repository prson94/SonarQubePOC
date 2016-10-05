import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService, ReferenceService } from '../../services/index';
import { ReferenceItemType } from '../../models/reference.model';

@Component({
    selector: 'd3s-reference-item-type-list',
    template: ` 
                <div class="tile tile-detail">
                    <header>Reference Item Types
                        <d3s-tile-actions [hasAdd]="true" (addClick)="addReferenceItemTypeList()"></d3s-tile-actions>                            
                    </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                        <input #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">
                        <p-dataTable [globalFilter]="gb" [value]="referenceTypes" selectionMode="single" [selection]="selected" (selectionChange)="selected=$event;selectedChange.emit(selected);" scrollable="true" scrollWidth="100%" [rows]="10" [paginator]="true" [pageLinks]="4" [rowsPerPageOptions]="[5,10,20]" [responsive]="true" [stacked]="stacked">                                                
                            <p-column field="Name" header="Name" [sortable]="true"></p-column>                                
                        </p-dataTable>        
                    </span>
                </div>
              `,
    providers: [ReferenceService],
})

export class ReferenceItemTypeGridComponent extends BaseComponent implements OnInit {
    @Input() selected: ReferenceItemType;
    @Output() selectedChange = new EventEmitter();

    private referenceTypes: ReferenceItemType[];
    
    constructor(private referenceService: ReferenceService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        
    }
    
};