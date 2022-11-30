import { NgModule } from '@angular/core';
import { CommonModule } from "@angular/common";
import { TabsComponent } from './tabs.component';
import { PipesModule } from '../../../pipes/pipes.module';
import { InfoTooltipModule } from '../tooltip/info-tooltip.component';

@NgModule({
    imports: [
        CommonModule,
        PipesModule,
        InfoTooltipModule
    ],
    declarations: [
        TabsComponent
    ],
    exports: [
        TabsComponent
    ],
    providers: []
})
export class TabsModule { }
