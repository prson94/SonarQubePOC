///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';


@Component({
    selector: 'd3s-assignments-tile',
    template: `
                <div class="tile tile-detail">
                   <header>Your Assignments
                    <d3s-tile-actions [hasAdd]="false"></d3s-tile-actions>                            
                   </header>
                    <div *ngIf="isLoading" style="width:100%; text-align:center;">
                        <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                    </div>
                    <p-dataTable *ngIf="!isLoading" [value]="assignments" selectionMode="single" [(selection)]="selected" >                    
                        <p-column field="Name" header="Name" [sortable]="true"></p-column>           
                        <p-column field="Name" header="Count" [sortable]="true"></p-column>                                                                
                    </p-dataTable>                      
                </div>
                `
})

export class AssignmentsTile extends BaseComponent implements OnInit {
    private assignments: any[] = [];
    private selected: any;

    ngOnInit() {

    }
}


