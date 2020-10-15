import { Component, Input } from '@angular/core';
import { SearchPathComponent } from '../../../models/search-result.model';

@Component({
    selector: 'asset-path-widget',
    template: `<div class="asset-path" [innerHtml]="formatPath()"></div>`
})
export class AssetPathWidgetComponent {

    @Input() path: SearchPathComponent[];
    @Input() withType: boolean = false;

    formatPath() {
        let keyseparator: string = '<span class="assetkeyseparator">/</span>';
        let pathseparator: string = '<i class="fa fa-angle-right assetpathseparator"></i>';
        return this.path.map(p => p.Key.join(keyseparator) + (this.withType ? ' <span class="assettype">(' + p.AssetType + ')</span>' : '')).join(pathseparator);
    }

}