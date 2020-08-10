import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { TooltipComponent } from './tooltip.component';
import { PreviewTooltipComponent } from './preview-tooltip.component';
import { LookupTooltipComponent } from './lookup-tooltip.component';
import { LoadingComponent } from './loading.component';
import { D3STreeTableToggler } from './treetable-toggler.component';
import { D3SColumnFilterModule } from './turbotable-column-filter.component';
import { D3SSortIconModule } from './turbotable-sorticon.component';
import { DirectivesModule } from '../../directives/directives.module';
import { NgxJsonViewModule } from 'ng-json-view';
import { PipesModule } from '../../pipes/pipes.module';
import { IgColorPickerModule } from './small-widgets/color-picker/color-picker.module';

@NgModule({
    declarations: [
        TooltipComponent,
        PreviewTooltipComponent,
        LoadingComponent,
        LookupTooltipComponent,
        D3STreeTableToggler
    ],
    exports: [
        TooltipComponent,
        PreviewTooltipComponent,
        LoadingComponent,
        LookupTooltipComponent,
        D3STreeTableToggler,
        D3SSortIconModule,
        D3SColumnFilterModule,
        DirectivesModule
    ]
    , imports: [
        CommonModule,
        RouterModule,
        DirectivesModule,
        PipesModule,
        //JSON Viewer module
        NgxJsonViewModule,
        IgColorPickerModule,
    ]

})

export class CoreModule { }