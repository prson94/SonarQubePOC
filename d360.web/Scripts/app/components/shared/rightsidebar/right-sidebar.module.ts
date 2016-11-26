import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import { RightSidebarItemComponent } from './right-sidebar-item.component';
import { RightSidebarComponent } from './right-sidebar.component';


@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,         
    ],
    declarations: [
        RightSidebarItemComponent,
        RightSidebarComponent
    ],
    exports: [        
        RightSidebarComponent
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class RightsidebarModule { }