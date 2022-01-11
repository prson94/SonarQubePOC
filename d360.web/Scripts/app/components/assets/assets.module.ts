import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { AssetsRoutingModule } from './assets.routes';
import { AssetsComponent } from './assets.component';
import { GovernRequestInterceptor } from '../../http-interceptors/govern-request.interceptor';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        AssetsRoutingModule
    ],
    declarations: [        
        AssetsComponent
    ],
    providers: [
        
    ]
})

export class AssetsModule { }
