import { NgModule } from '@angular/core';
import { CommonModule } from "@angular/common";
import { PageHeaderComponent } from './page-header.component';
import { PortalsModule } from '../portals/portals.module';

@NgModule({
    imports: [
        CommonModule,
        PortalsModule
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
