import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { IgBadgeModule } from '../badge/badge.module';
import { DropdownModule } from 'primeng/dropdown';
import { DirectivesModule } from '../../../../directives/directives.module';
import { FieldConditionGrid } from './field-condition-grid.component';
import { IgDateModule } from '../date/date';

@NgModule({
    imports: [
        CommonModule,
        TooltipModule,
        FormsModule,
        ReactiveFormsModule,
        IgBadgeModule,
        IgDateModule,

        DropdownModule,
        DirectivesModule
    ],
    declarations: [FieldConditionGrid],
    exports: [FieldConditionGrid]
})
export class FieldConditionGridModule { }
