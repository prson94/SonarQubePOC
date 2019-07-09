import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import { SiteMenuComponent } from './site-menu.component';
import { SiteMenuMegaItemComponent } from './site-menu-mega-item.component';
import { SiteMenuCategoryComponent } from './site-menu-category.component';
import { PipesModule } from '../../../pipes/pipes.module';


import {    
    TooltipModule,    
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,
        PipesModule, 

        //prime
        TooltipModule,
    ],
    declarations: [
        SiteMenuComponent,
        SiteMenuMegaItemComponent,
        SiteMenuCategoryComponent,
    ],
    exports: [
        SiteMenuComponent,        
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class SiteMenuModule { }