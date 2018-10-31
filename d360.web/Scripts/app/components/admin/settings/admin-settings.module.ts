import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

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
    DataTableModule,
    ColorPickerModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,

        AdminSettingsRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        InputTextModule,
        SharedModule,
        DataTableModule,
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
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminSettingsModule { }