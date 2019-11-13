import { AutoCompleteModule } from 'primeng/autocomplete';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgModule } from '@angular/core';
import { PipesModule } from '../../../pipes/pipes.module';
import { SharedModule } from 'primeng/shared';
import { TooltipModule } from 'primeng/tooltip'; 
import { TagView } from './d3s-tag-view';
 
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
        DeprecatedI18NPipesModule,
        FormsModule,
        PipesModule,
        SharedModule,
        TooltipModule
    ]
})
export class TagViewModule { }
