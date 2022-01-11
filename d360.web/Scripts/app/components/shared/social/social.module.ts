import { NgModule }       from "@angular/core";
import { CommonModule }       from "@angular/common";
import { FormsModule }    from "@angular/forms";
import { HTTP_INTERCEPTORS, HttpClientModule } from "@angular/common/http";

import { RouterModule } from "@angular/router";

import { CoreModule } from "../core.module";

import { SocialBoardComponent } from "./social-board.component";
import { SocialCommentComponent} from "./social-comment.component";
import { SocialTagInputComponent } from "./social-tag-input.component";

import { ButtonModule } from "primeng/button";
import { EditorModule } from "primeng/editor";
import { AutoCompleteModule } from "primeng/autocomplete";
import { ResourcesService } from "../../../services/resources.service";
import { CommentFormComponent } from "./comment-form.component";
import { DirectivesModule } from "../../../directives/directives.module";


@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        //primeng        
        AutoCompleteModule,
        EditorModule,
        ButtonModule,
        
        //d3s
        CoreModule,
        DirectivesModule

    ],
    declarations: [
        SocialBoardComponent,
        SocialCommentComponent,
        SocialTagInputComponent, 
        CommentFormComponent
    ],
    exports: [
        SocialBoardComponent,                        
    ],
    providers: [
        
        ResourcesService
    ]
})
export class SocialModule { }