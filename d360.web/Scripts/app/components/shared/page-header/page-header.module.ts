import { NgModule } from '@angular/core';
import { CommonModule } from "@angular/common";
import { PageHeaderComponent } from './page-header.component';

@NgModule({
    imports: [
        CommonModule
    ],
    declarations: [
        PageHeaderComponent
    ],
    exports: [
        PageHeaderComponent
    ],
    providers: []
})
export class PageHeaderModule { }