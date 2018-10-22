import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';

import { DropdownItemToSelectItemPipe } from './dropdown-to-selectitem.pipe';
import { ModelTypePipe } from './model-type.pipe';
import { PermissionFilterPipe } from './permission-filter.pipe';
import { ScoreDisplayPipe } from './score-display.pipe';
import { TechnicalNameToDisplayValuePipe } from './technical-to-display.pipe';
import { TreeSearchPipe } from './tree-search.pipe';
import { ArrayToSelectItemPipe } from './array-to-selectitem.pipe';
import { ResponsibilityTypeRelationAllocationOptionFilterPipe } from './responsibilitytypeallocation-filter.pipe';
import { SafeHtmlPipe } from './safe-html.pipe';
import { UtcDatePipe } from './utc-date.pipe';

@NgModule({
    imports: [CommonModule],
    declarations: [
        ArrayToSelectItemPipe,
        TreeSearchPipe,
        DropdownItemToSelectItemPipe,
        ModelTypePipe,
        PermissionFilterPipe,
        ResponsibilityTypeRelationAllocationOptionFilterPipe,
        ScoreDisplayPipe,
        TechnicalNameToDisplayValuePipe,
        SafeHtmlPipe,
        UtcDatePipe
    ],
    exports: [
        ArrayToSelectItemPipe,
        TreeSearchPipe,
        DropdownItemToSelectItemPipe,
        ModelTypePipe,
        PermissionFilterPipe,
        ResponsibilityTypeRelationAllocationOptionFilterPipe,
        ScoreDisplayPipe,
        TechnicalNameToDisplayValuePipe,
        SafeHtmlPipe,
        UtcDatePipe
    ]
})
export class PipesModule { }