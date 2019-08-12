import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { RightSidebarItemComponent } from './right-sidebar-item.component';
import { RightSidebarComponent } from './right-sidebar.component';
import { DynamicPercentageModule } from '../small-widgets/dynamic-percentage/dynamic-percentage-module';
import { SharedDynamicGridEditorModule } from '../dynamicgrideditor/shared-dynamic-grid-editor.module';
import { TagUsageInfoModule } from '../../admin/tags/tags-usage-info.module';
import { SharedDeleteFormModule } from '../delete.form';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        DynamicPercentageModule,
        HttpClientModule,
        SharedDynamicGridEditorModule,
        TagUsageInfoModule,
        SharedDeleteFormModule
    ],
    declarations: [
        RightSidebarItemComponent,
        RightSidebarComponent
    ],
    exports: [        
        RightSidebarComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class RightsidebarModule { }