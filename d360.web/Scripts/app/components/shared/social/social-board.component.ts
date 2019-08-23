import { Input, Component, EventEmitter, Output, OnInit, HostBinding } from '@angular/core';
import { BaseComponent } from '../base.component';
import { SocialService } from '../../../services/social.service';
import { SocialComment, SocialEditCommentData, SocialCommentType } from '../../../models/social.model';
import { CurrentCompanySettings } from '../../../static/company-settings'
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-social-board',
    template: ` 
                <div class="row">
                    <div class="col s12">
                        <header>{{socialMessage}}</header>  
                        <d3s-social-input (commented)="addComment($event);" *ngIf="allowComments()"></d3s-social-input>                        
                        <d3s-loading [isLoading]="isLoading" showTransparentLoader="true"></d3s-loading>
                        <div *ngFor="let comment of comments">
                            <d3s-social-comment [comment]="comment" (delete)="deleteComment($event);" (reply)="replyToComment($event);" (edit)="editComment($event);"></d3s-social-comment>                            
                        </div>                
                        <div style="margin-top:10px;">
                            <button pButton type="button" [disabled]="!hasMore" (click)="loadComments();" label="Load more comments..."></button>
                            <button *ngIf="hasCloseButton" pButton type="button" (click)="close.emit();" label="Close" style="width: 150px;"></button>                    
                        </div>
                    </div>
                </div>
                `,
    providers: [SocialService],       
})

export class SocialBoardComponent extends BaseComponent implements OnInit {
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() hasCloseButton: boolean = false;
    @Input() hasNewInput: boolean = true;
    @Input() daysToLookBack: number = -1;
    @Input() limitToType: SocialCommentType;

    @Output() countsChanged = new EventEmitter();
    @Output() close = new EventEmitter();
    
    private rowCount: number = 5;
    private pageNumber: number = 0;
    private hasMore: boolean = true;
    private comments: SocialComment[] = [];
    private socialMessage: string;
    
    

    constructor(private socialService: SocialService, protected messagesService: MessagesObservableService) {
        super();
    }

    ngOnInit() {

        if (this.objectID > 0) {
            this.socialMessage = null;
        }
        else {
            if (this.limitToType == SocialCommentType.Social)
                this.socialMessage = `My Comment's ${this.daysMessage()}`;
            else if (this.limitToType == SocialCommentType.Issue)
                this.socialMessage = `My Issue's ${this.daysMessage()}`;
            else if (this.limitToType == SocialCommentType.Task)
                this.socialMessage = `My Task's ${this.daysMessage()}`;
            else
                this.socialMessage = 'My Comments';
        }
                
        this.loadComments();
    }

    private daysMessage(): string {
        return this.daysToLookBack > 0 ? ('for the last ' + this.daysToLookBack + ' days') : '- all';
    }

    loadComments() {
        this.isLoading = true;
        this.socialService.getComments(this.objectID, this.objectType, this.daysToLookBack, (this.pageNumber) * this.rowCount, this.rowCount, this.limitToType)
            .subscribe(res => {
                this.isLoading = false;
                console.log(res);
                this.comments = this.comments.concat(res);
                this.hasMore = (res.length && res.length > 0);
            });
        this.pageNumber++;
    }

    private allowComments(): boolean {
        return this.hasNewInput && !CurrentCompanySettings.disableCommunityPosting;
    }

    private deleteComment(event) {
        let comment = event.comment;

        if (!comment) return;

        this.isLoading = true;

        let editData = new SocialEditCommentData(comment, comment.Tags);
        editData.ObjectID = this.objectID;
        editData.ObjectType = this.objectType;
        editData.Comment.IsDeleted = true;
        
        this.socialService.editComment(editData).
            subscribe(res => {
                if (res.IsDeleted) {                    
                    let index = this.comments.findIndex(x => x.ID == res.ID);
                    
                    if (index >= 0) {
                        this.comments.splice(index,1);
                    }    
                    this.messagesService.showInfoMessage('Success', 'Item deleted successfully');
                }
                this.countsChanged.emit({}); // counts changed fire event
                this.isLoading = false;
            });
    }

    private addComment(event) {
        let commentContent = event.comment;

        if (!commentContent) return;

        this.isLoading = true;
        let comment = new SocialComment();

        comment.Body = commentContent;
        comment.CommentTypeID = SocialCommentType.Social;
        
        let addData = new SocialEditCommentData(comment);
        addData.ObjectID = this.objectID;
        addData.ObjectType = this.objectType;        
        addData.Tags = event.tags? event.tags : [];

        this.socialService.addComment(addData).
            subscribe(res => {                
                if (res) {
                    this.comments.unshift(res);                    
                }
                this.messagesService.showInfoMessage('Success', 'Item added successfully');
                this.countsChanged.emit({}); // counts have changed fire event
                this.isLoading = false;
            });
    }

    private editComment(event) {
        let comment = event.comment;

        if (!comment) return;

        this.isLoading = true;

        let editData = new SocialEditCommentData(comment, comment.Tags);
        editData.ObjectID = this.objectID;
        editData.ObjectType = this.objectType;
        
        this.socialService.editComment(editData).
            subscribe(res => {       
                this.messagesService.showInfoMessage('Success', 'Item edited successfully');
                this.isLoading = false;
            });
    }

    private replyToComment(event) {
        if (!event) {
            console.log("DEV ERROR - EVENT OBJECT IS NULL!");
            return;
        }
        let replyText = event.reply;
        let commentId = event.commentId;
        
        if (!replyText || !commentId) return;

        this.isLoading = true;

        let comment = new SocialComment();

        comment.Body = replyText;
        comment.CommentTypeID = SocialCommentType.Social;
        comment.ParentID = commentId;

        let addData = new SocialEditCommentData(comment);
        addData.ObjectID = this.objectID;
        addData.ObjectType = this.objectType;
        addData.Tags = [];

        this.socialService.addComment(addData).
            subscribe(res => {
                if (res) {
                    let index = this.comments.findIndex(x => x.ID == res.ParentID);

                    if (index >= 0) {
                        if (!this.comments[index].Comments)
                            this.comments[index].Comments = [];
                        this.comments[index].Comments.push(res);
                    }                           
                }

                this.isLoading = false;
            });
    }
    
};