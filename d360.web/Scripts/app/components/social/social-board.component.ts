///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output, OnInit, HostBinding } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SocialService } from '../../services/index';
import { SocialComment, SocialEditCommentData, SocialCommentType } from '../../models/social.model';

@Component({
    selector: 'd3s-social-board',
    template: ` 
                <d3s-social-input (commented)="addComment($event);" [comment]="commentText"></d3s-social-input>
                <div *ngFor="let comment of comments">
                    <d3s-social-comment [comment]="comment" (delete)="deleteComment($event);"></d3s-social-comment>                            
                </div>                
                <button pButton type="button" [disabled]="!hasMore" (click)="loadComments();" label="Load more comments..." style="width: '150px';"></button>
                `,
    providers: [SocialService],        
})

export class SocialBoardComponent extends BaseComponent implements OnInit {
    @Input() objectID: number;
    @Input() objectType: string;
    @Input() objectName: string;
    
    private rowCount: number = 5;
    private pageNumber: number = 0;
    private hasMore: boolean = true;
    private comments: SocialComment[] = [];
    private commentText: string = "";

    constructor(private socialService: SocialService) {
        super();
    }

    ngOnInit() {
        this.loadComments();
    }

    loadComments() {
        this.isLoading = true;
        this.socialService.getComments(this.objectID, this.objectType, -1, (this.pageNumber) * this.rowCount, this.rowCount)
            .then(res => {
                this.isLoading = false;
                this.comments = this.comments.concat(res);
                this.hasMore = (res.length && res.length > 0);
            });
        this.pageNumber++;
    }

    private deleteComment(event) {
        let comment = event.comment;

        if (!comment) return;

        let editData = new SocialEditCommentData(comment, comment.Tags);
        editData.ObjectID = this.objectID;
        editData.ObjectType = this.objectType;
        editData.Comment.IsDeleted = true;
        
        this.socialService.editComment(editData).
            then(res => {
                if (res.IsDeleted) {                    
                    let index = this.comments.findIndex(x => x.ID == res.ID);
                    
                    if (index >= 0) {
                        this.comments.splice(index,1);
                    }                    
                }
            });
    }

    private addComment(event) {
        let commentContent = event.comment;

        if (!commentContent) return;

        let comment = new SocialComment();

        comment.Body = commentContent;
        comment.CommentTypeID = SocialCommentType.Social;
        
        let addData = new SocialEditCommentData(comment);
        addData.ObjectID = this.objectID;
        addData.ObjectType = this.objectType;        
        addData.Tags = [];

        this.socialService.addComment(addData).
            then(res => {                
                if (res) {
                    this.comments.unshift(res);

                    this.commentText = "";
                }
            });
    }
    
};