import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { ClassificationTypePipe } from './classification-display.pipe';
import { DropdownItemToSelectItemPipe } from './dropdown-to-selectitem.pipe';
import { FilterPipeName } from './filter-name.pipe';
import { ModelTypePipe } from './model-type.pipe';
import { TechnicalNameToDisplayValuePipe } from './technical-to-display.pipe';
import { ScoreDisplayPipe } from './score-display.pipe';
import { TreeSearchPipe } from './tree-search.pipe';

@NgModule({
    imports: [CommonModule],
    declarations: [
        TreeSearchPipe,
        ClassificationTypePipe,
        DropdownItemToSelectItemPipe,
        FilterPipeName,
        ModelTypePipe,        
        ScoreDisplayPipe,
        TechnicalNameToDisplayValuePipe,        
    ],
    exports: [
        TreeSearchPipe,
        ClassificationTypePipe,
        DropdownItemToSelectItemPipe,
        FilterPipeName,
        ModelTypePipe,        
        ScoreDisplayPipe,
        TechnicalNameToDisplayValuePipe,        
    ]
})
export class PipesModule { }