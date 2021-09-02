import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { ShortcutModule } from '../../shared/shortcuts/shortcut.module';
import { HelpMenuModule } from '../../shared/helpmenu/helpmenu.module';
import { IconPickerModule } from '../../shared/controls/icon-picker/icon-picker.component';
import { PipesModule } from '../../../pipes/pipes.module';
import { DayOfWeekInputModule } from "../../shared/small-widgets/dayofweek-input/dayofweek-input.component";

import { AdminSettingsComponent } from './admin-settings.component';
import { AdminSiteMenuComponent } from './admin-site-menu.component';
import { AdminIpRestrictionComponent } from './admin-ip-restriction.component';
import { AdminImageUploadComponent } from './admin-image-upload.component';
import { AdminSiteMenuPermissionsComponent } from './admin-site-menu-permissions.component';
import { AdminHomeComponent } from './admin-home.component';

import { AdminSettingsRoutingModule } from './admin-settings.routes';
import { D3SSharedModule } from '../../shared/shared.module';

import { SharedModule } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { ColorPickerModule } from 'primeng/colorpicker';
import { TableModule } from 'primeng/table';


@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpClientModule,

        AdminSettingsRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        InputTextModule,
        SharedModule,
        HelpMenuModule,
        ColorPickerModule,
        TableModule,

        //d3s        
        CoreModule,        
        SharedGridPagingInfoModule,        
        TilesModule,
        ShortcutModule,
        IconPickerModule,
        DayOfWeekInputModule,
        D3SSharedModule,
        PipesModule
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