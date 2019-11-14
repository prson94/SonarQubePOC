import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { AssetsRoutingModule } from './assets.routes';
import { AssetsComponent } from './assets.component';
import { GovernRequestInterceptor } from '../../http-interceptors/govern-request.interceptor';


@NgModule({
    imports: [
        CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        AssetsRoutingModule
    ],
    declarations: [        
        AssetsComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        }
    ]
})

export class AssetsModule { }
