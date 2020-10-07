import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AssetPathWidgetComponent } from './asset-path-widget.component';

@NgModule({
    imports: [
        CommonModule
    ],
    declarations: [
        AssetPathWidgetComponent
    ],
    exports: [
        AssetPathWidgetComponent
    ]
})
export class AssetPathWidgetModule { }
