import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import { CoreModule } from '../core.module';

import { SocialBoardComponent } from './social-board.component';
import { SocialCommentComponent} from './social-comment.component';
import { SocialInputComponent } from './social-input.component';
import { SocialTagInputComponent } from './social-tag-input.component';

import {    
    AutoCompleteModule,    
    EditorModule,   
    ButtonModule,             
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //primeng        
        AutoCompleteModule,
        EditorModule,
        ButtonModule,
        
        //d3s
        CoreModule,

    ],
    declarations: [
        SocialBoardComponent,
        SocialCommentComponent,
        SocialInputComponent,       
        SocialTagInputComponent, 
    ],
    exports: [
        SocialBoardComponent,                        
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class SocialModule { }