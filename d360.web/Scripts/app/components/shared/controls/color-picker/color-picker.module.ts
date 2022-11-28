import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ColorPickerComponent } from './color-picker.component';
import { DropdownModule } from 'primeng/dropdown';
import { ColorDisplayComponent } from './color-display.component';
import { DirectivesModule } from '../../../../directives/directives.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule, 
        DropdownModule,
        DirectivesModule

    ],
    declarations: [
        ColorPickerComponent,
        ColorDisplayComponent,       
    ],
    exports: [
        ColorPickerComponent,
        ColorDisplayComponent,
    ],
    providers: [
        
    ]
})
export class IgColorPickerModule { }