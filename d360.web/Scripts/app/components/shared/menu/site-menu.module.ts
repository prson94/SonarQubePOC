import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

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
        HttpModule,
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
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class SiteMenuModule { }