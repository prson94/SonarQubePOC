import { Component, Input } from '@angular/core';
import { SearchPathComponent } from '../../../models/search-result.model';

@Component({
    selector: 'asset-path-widget',
    template: `<ng-container>
                    <ng-container *ngFor="let section of path; last as isLast">
                        <span class="assetname" [innerHtml]="formatKey(section)"></span>
                        <span *ngIf="withType" class="assettype"> ({{section.AssetType}})</span>
                        <i *ngIf="!isLast" class="fa fa-angle-right assetpathseparator"></i>
                </ng-container>
            </ng-container>`
})
export class AssetPathWidgetComponent {

    @Input() path: SearchPathComponent[];
    @Input() withType: boolean = false;

    formatKey(section: SearchPathComponent): string {
        let keyseparator: string = '<span class="assetkeyseparator">/</span>';
        return section.Key.join(keyseparator)
    }
}