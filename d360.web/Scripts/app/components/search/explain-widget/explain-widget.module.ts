import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { ExplainWidgetComponent } from './explain-widget.component';

@NgModule({
    imports: [
        CommonModule
    ],
    declarations: [
        ExplainWidgetComponent
    ],
    exports: [
        ExplainWidgetComponent
    ]
})
export class ExplainWidgetModule { }
