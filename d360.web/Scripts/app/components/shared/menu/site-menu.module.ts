import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';


import { SiteMenuComponent } from './site-menu.component';
import { SiteMenuMegaItemComponent } from './site-menu-mega-item.component';
import { SiteMenuCategoryComponent } from './site-menu-category.component';


import {    
    TooltipModule,    
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

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
    ]
})
export class SiteMenuModule { }