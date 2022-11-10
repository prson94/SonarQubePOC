import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IgBadgeModule } from '../../../shared/controls/badge/badge.module';

import { SimpleBadgeComponent } from './simple-badge.component';

@NgModule({
    imports: [
        CommonModule,
        IgBadgeModule
    ],
    declarations: [
        SimpleBadgeComponent
    ],
    exports: [
        SimpleBadgeComponent
    ],
    providers: [
        
    ]
})
export class SimpleBadgeModule { }