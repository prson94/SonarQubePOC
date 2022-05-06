import { Component, Input } from '@angular/core';
import { CompanySettingsService } from '../../services/settings.service';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-admin-allocation',
    providers: [],
    template: `
                <header>Allocations</header>                
                    <p-table #dt [value]="allocations" class="nym-table" selectionMode="single" [metaKeySelection]="true" [dataKey]="'Name'">
                        <ng-template pTemplate="header">
                            <tr>
                                <th style="width: 25px; padding-left: 2px; padding-right: 2px; text-align: center"></th>
                                <th [pSortableColumn]="'Name'">
                                    <ng-container i18n>Name</ng-container>
                                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                </th>
                            </tr> 
                            <tr [hidden]="showSimpleFilter">
                                <th></th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item let-expanded="expanded">
                            <tr [pSelectableRow]="item">
                                <td [pRowToggler]="item">
                                    <i [ngClass]="expanded ? 'fa fa-chevron-circle-down' : 'fa fa-chevron-circle-right'" style="pointer:cursor;"></i>
                                </td>
                                <td>{{item.Name}}</td>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="rowexpansion" let-item>
                            <tr>
                                <td colspan="2">
                                    <d3s-admin-nym-allocations [objectType]="objectType" [objectID]="objectID"></d3s-admin-nym-allocations>
                                </td>
                            </tr>
                        </ng-template>
                    </p-table>
                `
})

export class AdminAllocationComponent extends BaseComponent {
    @Input() objectID: number;
    @Input() objectType: string;

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    public rows = [0];

    public allocations: any[] = [{ Name: 'Grammatic Type Allocation' }];
}