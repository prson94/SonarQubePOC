import { Component, Input } from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-admin-allocation',
    providers: [],
    template: `
               <header>Allocations</header>                
                <p-dataTable #dt [value]="allocations" selectionMode="single" expandableRows="true" [expandedRows]="rows">                        
                    <ng-template let-item pTemplate="rowexpansion">
                        <d3s-admin-nym-allocations [objectType]="objectType" [objectID]="objectID"></d3s-admin-nym-allocations>
                    </ng-template>
                    <p-column expander="true" [style]="{ 'width':'25px', 'padding-left': '2px', 'padding-right': '2px', 'text-align' : 'center' }"></p-column>
                    <p-column field="Name" header="Name" sortable="true"></p-column>                    
                </p-dataTable>
                `
})

export class AdminAllocationComponent extends BaseComponent {
    @Input() objectID: number;
    @Input() objectType: string;

    public rows = [0];

    public allocations: any[] = [{ Name: 'Grammatic Type Allocation' }];
}