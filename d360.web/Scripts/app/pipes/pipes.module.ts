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
import { MetricConditionDisabledFilterPipe } from './metric-condition-disabled-filter.pipe';
import { SelectItemTextToLabelPipe } from './selectitem-text-to-label.pipe';
import { SimpleSearch } from './simple-search-pipe';
import { AssetpathSeparatorPipe  } from './assetpath-separator.pipe';
import { GenericFilter } from './generic-filter.pipe';
import { EnumToSelectitemPipe } from './enum-to-selectitem.pipe';

@NgModule({
    imports: [CommonModule],
    declarations: [
        ArrayToSelectItemPipe,
        TreeSearchPipe,
        DropdownItemToSelectItemPipe,
        MetricConditionDisabledFilterPipe,
        ModelTypePipe,
        PermissionFilterPipe,
        ResponsibilityTypeRelationAllocationOptionFilterPipe,
        ScoreDisplayPipe,
        TechnicalNameToDisplayValuePipe,
        SafeHtmlPipe,
        UtcDatePipe,
        SelectItemTextToLabelPipe,
        SimpleSearch,
        AssetpathSeparatorPipe,
        GenericFilter,
        EnumToSelectitemPipe
    ],
    exports: [
        ArrayToSelectItemPipe,
        TreeSearchPipe,
        DropdownItemToSelectItemPipe,
        MetricConditionDisabledFilterPipe,
        ModelTypePipe,
        PermissionFilterPipe,
        ResponsibilityTypeRelationAllocationOptionFilterPipe,
        ScoreDisplayPipe,
        TechnicalNameToDisplayValuePipe,
        SafeHtmlPipe,
        UtcDatePipe,
        SelectItemTextToLabelPipe,
        SimpleSearch,
        AssetpathSeparatorPipe,
        GenericFilter,
        EnumToSelectitemPipe
    ]
})
export class PipesModule { }