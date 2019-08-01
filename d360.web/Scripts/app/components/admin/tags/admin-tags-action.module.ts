import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';

import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { AdminTagsActionComponent } from './admin-tags-action.component';
import { D3SCheckboxModule } from '../../shared/controls/gov-checkbox';

import { SiteModalModule } from '../../shared/modal/gov-modal.module';
import { TagUsageInfoModule } from './tags-usage-info.module';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,

        //d3s                
        CoreModule,

        TilesModule,
        SiteModalModule,
        TagUsageInfoModule,
        D3SCheckboxModule
    ],
    declarations: [
        AdminTagsActionComponent
    ],
    exports: [
        AdminTagsActionComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]

})
export class AdminTagsActionModule { }