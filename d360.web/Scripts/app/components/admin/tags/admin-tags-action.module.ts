import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { AdminTagsActionComponent } from './admin-tags-action.component';

import { SiteModalModule } from '../../shared/modal/gov-modal.module';
import { TagUsageInfoModule } from './tags-usage-info.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        
        //d3s                
        CoreModule,

        TilesModule,
        SiteModalModule,
        TagUsageInfoModule        
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