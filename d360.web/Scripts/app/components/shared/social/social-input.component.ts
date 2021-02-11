import { Component, EventEmitter, Output, ViewChildren, QueryList, ElementRef } from '@angular/core';
import { BaseComponent } from '../base.component';
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
                        <d3s-preview-tooltip *ngFor="let tag of tags" class="comment-tag" (click)="changeUrl(tag.Url)" [uid]="tag.AssetUid" [iconColor]="tag.IconForeColor" [foreColor]="tag.IconBackColor">{{tag.TextPath}} <i class="fa fa-times" (click)="removeTag(tag)"></i></d3s-preview-tooltip>
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
    isEditing: boolean = false;
    comment: string = '';
    tags: Tag[] = [];
     
    ngAfterViewInit() {
        this.viewChildren.changes.subscribe(x => this.setFocus(x) );
    }

    handleCommentClick() {
        this.commented.emit({
            comment: this.comment,
            tags: this.tags
        });
        this.isEditing = false;
    }
    

    setFocus(items) {
        if (items.length > 0) {
            items._results[0].quill.focus();
        }
    }

    addTag(event) {
        this.tags.push(event.tag);
    }

    removeTag(tag: Tag) {
        let index = this.tags.findIndex(x => x.Object == tag.Object && x.ObjectID == tag.ObjectID);

        if (index >= 0 && index < this.tags.length) {
            this.tags.splice(index, 1);
        }
    }

    showEditor() {
        this.tags = [];
        this.comment = "";
        this.isEditing = true;
    }
}