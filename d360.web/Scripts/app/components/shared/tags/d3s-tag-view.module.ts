import { AutoCompleteModule } from 'primeng/autocomplete';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgModule } from '@angular/core';
import { PipesModule } from '../../../pipes/pipes.module';
import { SharedModule } from 'primeng/api';
import { TooltipModule } from 'primeng/tooltip'; 
import { TagView } from './d3s-tag-view';
import { DirectivesModule } from '../../../directives/directives.module';
 
@NgModule({
    declarations: [
        TagView
    ],
    exports: [
        TagView
    ],
    imports: [
        AutoCompleteModule,
        CommonModule,
        FormsModule,
        PipesModule,
        SharedModule,
        TooltipModule,
        DirectivesModule
    ]
})
export class TagViewModule { }
