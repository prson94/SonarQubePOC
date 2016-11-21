import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';

import { SocialBoardComponent } from './social-board.component';
import { SocialCommentComponent} from './social-comment.component';
import { SocialInputComponent } from './social-input.component';
import { SocialTagInputComponent } from './social-tag-input.component';

import {
    GrowlModule,
    InputTextModule,
    InputMaskModule,
    DataTableModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,
    CalendarModule,
    MenuModule,
    MenubarModule,
    AccordionModule,
    SelectButtonModule,
    AutoCompleteModule,
    MultiSelectModule,
    SpinnerModule,
    EditorModule,        
    PaginatorModule,
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //primeng
        GrowlModule,
        InputTextModule,
        InputMaskModule,
        DataTableModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,
        CalendarModule,
        MenuModule,
        MenubarModule,
        AccordionModule,
        SelectButtonModule,
        AutoCompleteModule,
        MultiSelectModule,
        SpinnerModule,
        EditorModule,                
        PaginatorModule,
        SharedModule,

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
        SocialCommentComponent,
        SocialInputComponent,
        SocialTagInputComponent,
    ]
})
export class SocialModule { }