import { Input, Component, EventEmitter, Output, HostBinding, ViewChildren, QueryList, ElementRef } from '@angular/core';
import { BaseComponent } from '../base.component';
import { SocialService } from '../../../services/social.service';
import { SocialComment, SocialEditCommentData } from '../../../models/social.model';
import { Tag } from '../../../models/tag.model';

@Component({
    selector: 'd3s-social-input',
    template: ` 
                <div class="row" *ngIf="!isEditing">
                    <input type="text" placeholder="Add a comment" class="fakeInput" (click)="showEditor();">
                </div>
                <div class="row comment-input" *ngIf="isEditing">
                    <div class="col s12">
                        <p-editor #editor placeholder="Add a comment" name="Description" [style]="{'height':'50px'}" [(ngModel)]="comment" ></p-editor>
                    </div>                               
                </div>               
                <div class="row" *ngIf="isEditing" style="padding-top:15px;padding-botton:15px;">
                    <div class="col s12" style="padding-bottom:15px;" *ngIf="tags.length > 0">
                        <d3s-tooltip *ngFor="let tag of tags" class="comment-tag" (click)="changeUrl(tag.Url)" [objectType]="tag.Object" [objectId]="tag.ObjectID" [tooltipType]="'preview'" [iconColor]="tag.IconForeColor" [foreColor]="tag.IconBackColor">{{tag.TextPath}} <i class="fa fa-times" (click)="removeTag(tag)"></i></d3s-tooltip>
                    </div>
                    <div class="col s10">
                        <d3s-social-tag-input (selectTag)="addTag($event)"></d3s-social-tag-input>                                               
                    </div>         
                    <div class="col s2">
                        <button class="right" pButton type="button" (click)="isEditing=false;" label="Cancel"></button>
                        <button class="right" pButton type="button" (click)="handleCommentClick();" label="Post"></button>                        
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
            .comment-tag{
                border-radius: 5px;
                margin-right: 5px;
                padding: 3px 10px;
                cursor:pointer;
            }      
        `],
})

export class SocialInputComponent extends BaseComponent {    
    @Output() commented = new EventEmitter();
         
    @ViewChildren('editor') viewChildren: QueryList<ElementRef>;
    private isEditing: boolean = false;
    private comment: string = '';
    private tags: Tag[] = [];
     
    ngAfterViewInit() {
        this.viewChildren.changes.subscribe(x => this.setFocus(x) );
    }

    private handleCommentClick() {
        this.commented.emit({
            comment: this.comment,
            tags: this.tags
        });
        this.isEditing = false;
    }
    

    private setFocus(items) {
        if (items.length > 0) {                                    
            items._results[0].quill.focus();            
        }
    }

    private addTag(event) {
        this.tags.push(event.tag);
    }

    private removeTag(tag: Tag) {
        let index = this.tags.findIndex(x => x.Object == tag.Object && x.ObjectID == tag.ObjectID);

        if (index >= 0 && index < this.tags.length) {
            this.tags.splice(index, 1);
        }
    }

    private showEditor() {
        this.tags = [];
        this.comment = "";
        this.isEditing = true;
    }
};