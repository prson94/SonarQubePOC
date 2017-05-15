import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';

import { DropdownItemToSelectItemPipe } from './dropdown-to-selectitem.pipe';
import { ModelTypePipe } from './model-type.pipe';
import { ScoreDisplayPipe } from './score-display.pipe';
import { TechnicalNameToDisplayValuePipe } from './technical-to-display.pipe';
import { TreeSearchPipe } from './tree-search.pipe';
import { ArrayToSelectItemPipe } from './array-to-selectitem.pipe';

@NgModule({
    imports: [CommonModule],
    declarations: [
        ArrayToSelectItemPipe,
        TreeSearchPipe,
        DropdownItemToSelectItemPipe,
        ModelTypePipe,
        ScoreDisplayPipe,
        TechnicalNameToDisplayValuePipe,
    ],
    exports: [
        ArrayToSelectItemPipe,
        TreeSearchPipe,
        DropdownItemToSelectItemPipe,
        ModelTypePipe,
        ScoreDisplayPipe,
        TechnicalNameToDisplayValuePipe,
    ]
})
export class PipesModule { }