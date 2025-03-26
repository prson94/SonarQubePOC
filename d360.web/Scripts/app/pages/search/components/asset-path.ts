import { Component, Input } from '@angular/core';
import { SearchPathComponent } from '../../../models/search-result.model';
import { escape, isNil } from "lodash-es";

@Component({
    selector: 'asset-path',
	template: `<ng-container>
					@for (section of path; track path; let isLast = $last) {
						<span class="assetname" [innerHtml]="formatKey(section)"></span>
						@if (showType(section)) { <span class="assettype"> ({{section.AssetType}})</span> }
						@if (!isLast) { <i class="fa fa-angle-right assetpathseparator"></i> }
					}
            </ng-container>`,
	standalone: true
})
export class AssetPath {

    @Input() path: SearchPathComponent[];
    @Input() withType: boolean = false;

    formatKey(section: SearchPathComponent): string {
        const keyseparator: string = '<span class="assetkeyseparator">/</span>';
        return section.Key.map((v) => escape(v)).join(keyseparator);
    }

    showType(section: SearchPathComponent): boolean {
        return this.withType && !isNil(section.AssetType) && section.AssetType !== "";
    }
}