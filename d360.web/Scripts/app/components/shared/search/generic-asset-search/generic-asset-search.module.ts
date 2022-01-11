import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { RouterModule } from '@angular/router';

import { AutoCompleteModule } from 'primeng/autocomplete';
import { TreeModule } from 'primeng/tree';
import { OverlayPanelModule } from 'primeng/overlaypanel';
import { DialogModule } from 'primeng/dialog';
import { SharedModule } from 'primeng/api';

import { AssetSearchComponent } from './generic-asset-search.component';
import { PaginatorModule } from 'primeng/paginator';
import { PredicateSelectorComponent } from './predicate-selector.component';
import { PipesModule } from '../../../../pipes/pipes.module';
import { GovernRequestInterceptor } from '../../../../http-interceptors/govern-request.interceptor';
import { SegmentsTooltipComponent } from './segments-tooltip.component';

import { DirectivesModule } from '../../directives/directives.module'

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        //d3s
        PipesModule,

        //primeng        
        AutoCompleteModule,
        OverlayPanelModule,
        SharedModule,
        TreeModule,
        DialogModule,
        PaginatorModule,
        DirectivesModule
    ],
    declarations: [
        AssetSearchComponent,
        PredicateSelectorComponent,
        SegmentsTooltipComponent
    ],
    exports: [
        AssetSearchComponent,
        PredicateSelectorComponent
    ],
    providers: [
        
    ]
})
export class AssetSearchModule { }

