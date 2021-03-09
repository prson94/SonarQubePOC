import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { DropdownModule } from 'primeng/dropdown';
import { AdvancedFilteringComponent } from './advanced-filtering.component';
import { DirectivesModule } from '../../../directives/directives.module';
import { IgBadgeModule } from '../../shared/controls/badge/badge.module';
import { IgDateModule } from '../../shared/controls/date/date';
import { IgNumberFieldModule } from '../../shared/controls/number-picker/number-input.component';


@NgModule({
    imports: [
        CommonModule,
        TooltipModule,
        FormsModule,
        ReactiveFormsModule,
        IgBadgeModule,
        IgDateModule,
        IgNumberFieldModule,


        DropdownModule,
        DirectivesModule
    ],
    declarations: [AdvancedFilteringComponent],
    exports: [AdvancedFilteringComponent]
})
export class AdvancedFiltersModule { }
