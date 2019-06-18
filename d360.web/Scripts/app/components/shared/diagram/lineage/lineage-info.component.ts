import { Component, Input, OnInit, OnChanges, Output, EventEmitter } from '@angular/core';
import { ToolTipService } from '../../../../services/tooltip.service';
import { LineageNode } from '../../../../models/lineage.model';
import * as go from 'gojs';

@Component({
    selector: 'd3s-lineage-info',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div class="tooltip-panel" [hidden]="isLoading">
            <h3 style="positon: relative"><a [routerLink]="data?.Url">{{data?.DisplayName}}</a> <small *ngIf="data && data.TypeName" style="background-color: #fff; float:right;font-size:65%;">{{data.TypeName}}</small></h3>
            <div>&nbsp;</div>
            <p *ngIf="data?.Description" [innerHtml]="data?.Description"></p>
            <div *ngFor="let field of data?.FieldValues">
                <span *ngIf="field.Value"><b>{{field.Name}}</b>: <span  *ngIf="GetJSON(field.Value) == 'Error'; else showJSON" [innerHtml]="field.Value"></span>
                      <ng-template #showJSON >
                          <span>
                              <ngx-json-view [data]="GetJSON(field.Value)"></ngx-json-view>
                          </span>
                      </ng-template>
                </span>
            </div>                        
        </div>
    `,
    providers: [ToolTipService]
})

export class LineageInfoComponent implements OnInit, OnChanges {
    @Input() node: LineageNode;
    @Input() diagram: go.Diagram;
    @Output() selectionChange = new EventEmitter();
    data: any = null;
    isLoading = false;

    constructor(private tooltipService: ToolTipService) { }

    ngOnChanges() {
        //console.log(this.node);
        this.data = null;
        this.load();
    }

    ngOnInit() { }

    load() {
        if (this.node == null || this.node.objectId == null)
            return;
        //console.log(this.node);
        this.isLoading = true;
        this.tooltipService.getTooltipInfo(this.node.object, this.node.objectId)
            .then(data => {
                this.data = data;
                this.isLoading = false;
            });
    }

    private GetJSON(value: string) {
        try {
            return JSON.parse(value);
        } catch {
            return "Error";
        }
    }
}