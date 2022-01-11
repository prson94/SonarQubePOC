import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { AssetRoutingModule } from './asset.routes';
import { AssetComponent } from './asset.component';
import { GovernRequestInterceptor } from '../../http-interceptors/govern-request.interceptor';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        AssetRoutingModule
    ],
    declarations: [        
        AssetComponent
    ],
    providers: [
        
    ]
})

export class AssetModule { }
