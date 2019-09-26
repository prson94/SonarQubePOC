import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';
import { D3SSharedModule } from '../../shared/shared.module';

import { AdminResourcesComponent } from './admin-resources.component';
import { AdminResourcesRoutingModule } from './admin-resources.routes';
import { SharedModule } from 'primeng/shared';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,

        AdminResourcesRoutingModule,

        //prime        
        SharedModule,

        //d3s                
        CoreModule, 
        D3SSharedModule,       
        SharedFieldDefinitionModule,        
        TilesModule,
    ],
    declarations: [
        AdminResourcesComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class AdminResourcesModule { }