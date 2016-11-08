import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';

import { DropdownItemToSelectItemPipe } from './dropdown-to-selectitem.pipe';
import { ModelTypePipe } from './model-type.pipe';
import { ScoreDisplayPipe } from './score-display.pipe';
import { TechnicalNameToDisplayValuePipe } from './technical-to-display.pipe';
import { TreeSearchPipe } from './tree-search.pipe';

@NgModule({
    imports: [CommonModule],
    declarations: [
        TreeSearchPipe,
        DropdownItemToSelectItemPipe,
        ModelTypePipe,
        ScoreDisplayPipe,
        TechnicalNameToDisplayValuePipe,
    ],
    exports: [
        TreeSearchPipe,
        DropdownItemToSelectItemPipe,
        ModelTypePipe,
        ScoreDisplayPipe,
        TechnicalNameToDisplayValuePipe,
    ]
})
export class PipesModule { }