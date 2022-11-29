import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MessageBoxComponent } from './message-box.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

    ],
    declarations: [
        MessageBoxComponent,
    ],
    exports: [
        MessageBoxComponent,
    ],
    providers: [
        
    ]
})
export class IgMessageBoxModule { }