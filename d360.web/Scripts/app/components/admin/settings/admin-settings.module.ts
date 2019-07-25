import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { ShortcutModule } from '../../shared/shortcuts/shortcut.module';
import { IconPickerModule } from '../../shared/icon-picker.component';

import { AdminSettingsComponent } from './admin-settings.component';
import { AdminSiteMenuComponent } from './admin-site-menu.component';
import { AdminIpRestrictionComponent } from './admin-ip-restriction.component';
import { AdminImageUploadComponent } from './admin-image-upload.component';
import { AdminSiteMenuPermissionsComponent } from './admin-site-menu-permissions.component';
import { AdminHomeComponent } from './admin-home.component';

import { AdminSettingsRoutingModule } from './admin-settings.routes';
import { D3SSharedModule } from '../../shared/shared.module';

import {
    ButtonModule,
    DropdownModule,
    InputTextModule,
    SharedModule,
    ColorPickerModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,

        AdminSettingsRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        InputTextModule,
        SharedModule,
        ColorPickerModule,
        TableModule,

        //d3s        
        CoreModule,        
        SharedGridPagingInfoModule,        
        TilesModule,
        ShortcutModule,
        IconPickerModule,
        D3SSharedModule,
    ],
    declarations: [
//        IconPickerComponent,
        AdminSettingsComponent,
        AdminSiteMenuComponent,
        AdminIpRestrictionComponent,
        AdminImageUploadComponent,
        AdminSiteMenuPermissionsComponent,
        AdminHomeComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true},
    ]
})
export class AdminSettingsModule { }