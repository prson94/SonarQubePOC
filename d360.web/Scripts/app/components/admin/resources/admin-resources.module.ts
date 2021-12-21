import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';



import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';
import { D3SSharedModule } from '../../shared/shared.module';

import { AdminResourcesComponent } from './admin-resources.component';
import { AdminResourcesRoutingModule } from './admin-resources.routes';
import { SharedModule } from 'primeng/api';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,


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
    ]
})
export class AdminResourcesModule { }