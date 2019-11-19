import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { AssetRoutingModule } from './asset.routes';
import { AssetComponent } from './asset.component';
import { GovernRequestInterceptor } from '../../http-interceptors/govern-request.interceptor';

@NgModule({
    imports: [
        CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        AssetRoutingModule
    ],
    declarations: [        
        AssetComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        }
    ]
})

export class AssetModule { }
