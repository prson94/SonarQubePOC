import { Input, Component, EventEmitter, Output, OnInit, HostBinding } from '@angular/core';
import { BaseComponent } from '../base.component';
import { SocialService } from '../../../services/social.service';
import { CommentApiPostModel, CommentApiPutModel, CommentDetail, CommentType } from '../../../models/social.model';
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
    @Input() assetUid: string;
    @Input() hasCloseButton: boolean = false;
    @Input() hasNewInput: boolean = true;
    @Input() daysToLookBack: number = -1;
    @Input() limitToType: CommentType;

    @Output() countsChanged = new EventEmitter();
    @Output() close = new EventEmitter();
    
    rowCount: number = 5;
    pageNumber: number = 0;
    hasMore: boolean = true;
    comments: CommentDetail[] = [];
    socialMessage: string;

    constructor(private socialService: SocialService, protected messagesService: MessagesObservableService) {
        super();
    }

    ngOnInit() {

        if (this.objectID > 0) {
            this.socialMessage = null;
        }
        else {
            if (this.limitToType == CommentType.Social)
                this.socialMessage = `My comments ${this.daysMessage()}`;
            else if (this.limitToType == CommentType.Issue)
                this.socialMessage = `My issues ${this.daysMessage()}`;
            else
                this.socialMessage = 'My comments';
        }
                
        this.loadComments();
    }

    private daysMessage(): string {
        return this.daysToLookBack > 0 ? ('for the last ' + this.daysToLookBack + ' days') : '- all';
    }

    loadComments() {
        this.isLoading = true;
        this.socialService.getComments(this.assetUid, this.daysToLookBack, (this.pageNumber) * this.rowCount, this.rowCount, this.limitToType)
            .subscribe(res => {
                this.isLoading = false;
                this.comments = this.comments.concat(res);
                this.hasMore = (res.length && res.length > 0);
            });
        this.pageNumber++;
    }

    allowComments(): boolean {
        return this.hasNewInput && !CurrentCompanySettings.disableCommunityPosting;
    }

    deleteComment(event) {
        let comment = event.comment as CommentDetail;

        if (!comment) return;

        this.isLoading = true;

        this.socialService.deleteComment(comment.Uid).
            subscribe(res => {
                if (res) {
                    comment.IsDeleted = true;
                    let index = this.comments.findIndex(x => x.ID == comment.ID);
                    
                    if (index >= 0) {
                        this.comments.splice(index,1);
                    }
                    this.messagesService.showInfoMessage('Success', 'Item deleted successfully');
                }
                this.countsChanged.emit({}); // counts changed fire event
                this.isLoading = false;
            });
    }

    addComment(event) {
        let commentContent = event.comment;

        if (!commentContent) return;

        this.isLoading = true;
        let comment = new CommentApiPostModel();

        comment.Body = commentContent;
        comment.AssetUid = this.assetUid;
        comment.Body = commentContent;
        comment.Tags = event.tags? event.tags : [];

        this.socialService.addComment(comment).
            subscribe(res => {                
                if (res) {
                    this.comments.unshift(res);                    
                }
                this.messagesService.showInfoMessage('Success', 'Item added successfully');
                this.countsChanged.emit({}); // counts have changed fire event
                this.isLoading = false;
            });
    }

    editComment(event) {
        if (!event.comment) return;

        this.isLoading = true;

        let comment = new CommentApiPutModel();
        comment.Body = event.comment.Body;
        comment.Tags = event.tags;
        comment.Uid = event.comment.Uid;
        
        this.socialService.editComment(comment).
            subscribe(res => {       
                this.messagesService.showInfoMessage('Success', 'Item edited successfully');
                this.isLoading = false;
            });
    }

    replyToComment(event) {
        if (!event) {
            console.log("DEV ERROR - EVENT OBJECT IS NULL!");
            return;
        }
        let replyText = event.reply;
        let parentUid = event.parentUid;
        
        if (!replyText || !parentUid) return;

        this.isLoading = true;

        let comment = new CommentApiPostModel();

        comment.Body = replyText;
        comment.ParentUid = parentUid;
        comment.AssetUid = this.assetUid;
        comment.Tags = [];

        this.socialService.addComment(comment).
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
    
}