import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-tooltip',
    templateUrl: './gallery.tooltip.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        `
    ], changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryTooltipComponent {
    properties: Array<any>;
    sampleUsage: string = `<button pTooltip="Export to Excel" tooltipPosition="top" tooltipStyleClass="ig-tooltip" igButton icon="fa-download"></button>`;

}
