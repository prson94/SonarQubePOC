import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { AutoCompleteModule } from 'primeng/autocomplete';
import { TreeModule } from 'primeng/tree';
import { OverlayPanelModule } from 'primeng/overlaypanel';
import { DialogModule } from 'primeng/dialog';
import { SharedModule } from 'primeng/shared';

import { AssetSearchComponent } from './generic-asset-search.component';
import { PaginatorModule } from 'primeng/paginator';
import { PredicateSelectorComponent } from './predicate-selector.component';
import { PipesModule } from '../../../../pipes/pipes.module';
import { GovernRequestInterceptor } from '../../../../http-interceptors/govern-request.interceptor';
import { SegmentsTooltipComponent } from './segments-tooltip.component';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //d3s
        PipesModule,

        //primeng        
        AutoCompleteModule,
        OverlayPanelModule,
        SharedModule,
        TreeModule,
        DialogModule,
        PaginatorModule
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
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class AssetSearchModule { }

