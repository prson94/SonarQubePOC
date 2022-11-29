import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { PaginatorComponent } from './paginator-bar-component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
    ],
    declarations: [
        PaginatorComponent
    ],
    exports: [
        PaginatorComponent
    ],
    providers: [
        
    ]
})
export class PaginatorModule { }