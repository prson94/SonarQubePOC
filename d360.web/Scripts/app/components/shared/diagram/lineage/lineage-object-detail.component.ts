import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { ToolTipService } from '../../../../services/tooltip.service';


@Component({
    selector: 'd3s-lineage-object-detail',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div class="tooltip-panel" [hidden]="isLoading">
            <h3 style="positon: relative"><a [routerLink]="data?.Url">{{data?.DisplayName}}</a> <small *ngIf="data && data.TypeName" style="background-color: #fff; float:right;font-size:65%;">{{data.TypeName}}</small></h3>
            <div>&nbsp;</div>
            <p *ngIf="data?.Description" [innerHtml]="data?.Description"></p>
            <div *ngFor="let field of data?.FieldValues"><span *ngIf="field.Value"><b>{{field.Name}}</b>: <span [innerHtml]="field.Value"></span></span></div>                        
        </div>
    `,
    providers: [ToolTipService]
})

export class LineageObjectDetailComponent implements OnInit, OnChanges {
    @Input() objectType: string;
    @Input() objectId: number;

    data: any = null;
    isLoading = false;

    constructor(private tooltipService: ToolTipService) { }

    ngOnChanges() {
        this.load();
    }

    ngOnInit() { }

    load() {
        this.isLoading = true;
        this.tooltipService.getTooltipInfo(this.objectType, this.objectId)
            .subscribe(data => {
                this.data = data;
                this.isLoading = false;
            });
    }
}