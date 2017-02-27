import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import { CoreModule } from '../shared/core.module';

import { MappingRoutingModule } from './mapping.routes';

import { MappingComponent } from './mapping.component';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //routing 
        MappingRoutingModule,

        //d3s        
        CoreModule,
    ],
    declarations: [
        MappingComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class MappingModule { }