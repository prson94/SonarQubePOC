import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';

import { DropdownItemToSelectItemPipe } from './dropdown-to-selectitem.pipe';
import { ModelTypePipe } from './model-type.pipe';
import { PermissionFilterPipe } from './permission-filter.pipe';
import { ScoreDisplayPipe } from './score-display.pipe';
import { TechnicalNameToDisplayValuePipe } from './technical-to-display.pipe';
import { TreeSearchPipe } from './tree-search.pipe';
import { ArrayToSelectItemPipe } from './array-to-selectitem.pipe';
import { SafeHtmlPipe } from './safe-html.pipe';

@NgModule({
    imports: [CommonModule],
    declarations: [
        ArrayToSelectItemPipe,
        TreeSearchPipe,
        DropdownItemToSelectItemPipe,
        ModelTypePipe,
        PermissionFilterPipe,
        ScoreDisplayPipe,
        TechnicalNameToDisplayValuePipe,
        SafeHtmlPipe
    ],
    exports: [
        ArrayToSelectItemPipe,
        TreeSearchPipe,
        DropdownItemToSelectItemPipe,
        ModelTypePipe,
        PermissionFilterPipe,
        ScoreDisplayPipe,
        TechnicalNameToDisplayValuePipe,
        SafeHtmlPipe
    ]
})
export class PipesModule { }