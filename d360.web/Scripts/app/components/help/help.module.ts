import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';

import { HelpRoutingModule } from './help.routes';

import { HelpComponent } from './help.component';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //routing 
        HelpRoutingModule,

        //d3s        
        CoreModule,        
    ],
    declarations: [
        HelpComponent,
    ]
})
export class HelpModule { }