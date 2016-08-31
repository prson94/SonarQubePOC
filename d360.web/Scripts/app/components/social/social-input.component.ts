///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output, HostBinding, ViewChildren, QueryList, ElementRef } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SocialService } from '../../services/index';
import { SocialComment, SocialEditCommentData } from '../../models/social.model';

@Component({
    selector: 'd3s-social-input',
    template: ` 
                <div class="row" *ngIf="!isEditing">
                    <input type="text" placeholder="Add a comment" class="fakeInput" (click)="isEditing = true;">
                </div>
                <div class="row comment-input" *ngIf="isEditing">
                    <div class="col s12">
                        <p-editor #editor placeholder="Add a comment" name="Description" [style]="{'height':'50px'}" [(ngModel)]="comment" ></p-editor>
                    </div>                               
                </div>               
                <div class="row" *ngIf="isEditing" style="padding-top:15px;padding-botton:15px;">
                    <div class="col s10">
                        Tags:
                    </div>         
                    <div class="col s2">
                        <button class="right" pButton type="button" (click)="handleCommentClick();" label="Comment" style="width: '150px';"></button>
                        <button class="right" pButton type="button" (click)="isEditing=false;" label="Cancel" style="width: '150px';"></button>
                    </div>
                </div> 
                `,
    styles: [`  
            .fakeInput{
                width:100%;
                padding:10px;
                border: 1px solid #CCCCCC;
                border-radius: 5px;
                margin: 5px;
            }          
        `],
})

export class SocialInputComponent extends BaseComponent {    
    @Output() commented = new EventEmitter();
         
    @ViewChildren('editor') viewChildren: QueryList<ElementRef>;
    private isEditing: boolean = false;
    private comment: string = '';
     
    ngAfterViewInit() {
        this.viewChildren.changes.subscribe(x => this.setFocus(x) );
    }

    private handleCommentClick() {
        this.commented.emit({
            comment: this.comment
        });
        this.isEditing = false;
    }
    

    private setFocus(items) {
        if (items.length > 0) {                                    
            items._results[0].quill.focus();
            
        }
    }
};