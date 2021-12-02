import { Component, Input, OnInit, OnChanges, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { ToolTipService } from '../../../services/tooltip.service';

@Component({
    selector: 'd3s-workflow-monitor-action-details',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div class="detail-panel" [hidden]="isLoading">
            <div>
                <span class="FieldName">
                    Action Type:&nbsp;
                </span>
                <span>
                    {{data?.TypeName}}
                </span>
            </div>
            <div>
                <span class="FieldName">
                    UID:&nbsp;
                </span>
                <span>
                    {{data?.UID}}
                </span>
            </div>
            <ng-container *ngFor="let field of data?.FieldValues">
                <div *ngIf="field.Value">
                    <span pTooltip="{{field.Description}}" tooltipPosition="left" tooltipStyleClass="ig-tooltip" class="FieldName">
                        {{field.Name}}:&nbsp;
                    </span>
                    <ng-template [ngIf]="field.Values && field.Values.length > 0" [ngIfElse]="singlevalue">
                        <span>
                            <span *ngFor="let singleitem of field.Values" style="margin-left:2em;text-indent:-1em;display:block;">
                                <span [innerHtml]="singleitem"></span>
                            </span>
                        </span>
                    </ng-template>
                    <ng-template #singlevalue>
                        <span [innerHtml]="field.Value">
                        </span>
                    </ng-template>
                </div>
            </ng-container>                        
        </div>
    `,
    providers: [ToolTipService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class WorkflowMonitorActionDetailsComponent implements OnInit, OnChanges {
    @Input() id: number;
    data: any = null;
    isLoading = false;

    constructor(private tooltipService: ToolTipService, private ref: ChangeDetectorRef) { }

    ngOnChanges() {
        this.data = null;
        this.load();
    }

    ngOnInit() { }

    load() {
        if (this.id == null || this.id < 1)
            return;

        this.isLoading = true;
        this.ref.markForCheck();
        this.tooltipService.getTooltipInfo('Issue', this.id)
            .subscribe(data => {
                this.data = data;
                this.isLoading = false;
                this.ref.markForCheck();
            });



    }
}