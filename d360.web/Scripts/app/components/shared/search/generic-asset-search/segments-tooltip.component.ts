import { Component, ChangeDetectionStrategy, OnInit, Input, ChangeDetectorRef } from '@angular/core';
import { ToolTipService } from '../../../../services/tooltip.service';
import { AssetTypeClass } from '../../../../models/asset.model';


@Component({
    selector: 'd3s-segments-tooltip',
    template: `<div class="gas-tooltip">
                    <div class="segments">
                                <span class="span-break segment" *ngFor="let segment of item.Segments;">
                                    <span class="value">{{segment.Value}}</span>
                                    <i *ngIf="item.Segments.indexOf(segment) != item.Segments.length-1" class="arrow-right fa fa-chevron-right"></i>
                                </span>
                            </div>
                <span>{{assetTypeText}}</span>
             </div>`,
    providers: [ToolTipService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class SegmentsTooltipComponent implements OnInit {

    @Input() item: any;

    assetTypeText: string = '...loading...';

    constructor(
        private tooltipService: ToolTipService,
        private ref: ChangeDetectorRef
    ) {

    }

    ngOnInit() {
        this.tooltipService.getTooltipInfoByUid(this.item.AssetTypeUid)
            .subscribe(info => {
                switch (info.Class) {                    
                    case AssetTypeClass.BusinessAsset:
                        this.assetTypeText = "Business Asset";
                        break;
                    case AssetTypeClass.Model:
                        this.assetTypeText = "Model";
                        break;
                    case AssetTypeClass.Organization:
                        this.assetTypeText = "Organization";
                        break;
                    case AssetTypeClass.Policy:
                        this.assetTypeText = "Policy";
                        break;
                    case AssetTypeClass.Reference:
                        this.assetTypeText = "Reference";
                        break;
                    case AssetTypeClass.ReferenceItemType:
                        this.assetTypeText = "Reference Item Type";
                        break;
                    case AssetTypeClass.Rule:
                        this.assetTypeText = "Rule";
                        break;
                    case AssetTypeClass.TechnicalAsset:
                        this.assetTypeText = "Technical Asset";
                        break;
                    default:
                        this.assetTypeText = "Unknown Type";
                        break;
                }

                this.assetTypeText += ": " + info.DisplayName;
                this.ref.markForCheck();
            });
    }


}


