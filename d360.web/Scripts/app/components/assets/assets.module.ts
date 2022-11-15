import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AssetsRoutingModule } from './assets.routes';
import { AssetsComponent } from './assets.component';


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
