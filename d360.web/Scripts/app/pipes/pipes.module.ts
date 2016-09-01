import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { ClassificationTypePipe } from './classification-display.pipe';
import { DropdownItemToSelectItemPipe } from './dropdown-to-selectitem.pipe';
import { FilterPipeName } from './filter-name.pipe';
import { ModelTypePipe } from './model-type.pipe';
import { RelationshipSearchPipe } from './relationship-search.pipe';
import { TechnicalNameToDisplayValuePipe } from './technical-to-display.pipe';

@NgModule({
    imports: [CommonModule],
    declarations: [
        ClassificationTypePipe,
        DropdownItemToSelectItemPipe,
        FilterPipeName,
        ModelTypePipe,
        RelationshipSearchPipe,
        TechnicalNameToDisplayValuePipe,
    ],
    exports: [
        ClassificationTypePipe,
        DropdownItemToSelectItemPipe,
        FilterPipeName,
        ModelTypePipe,
        RelationshipSearchPipe,
        TechnicalNameToDisplayValuePipe,
    ]
})
export class PipesModule { }