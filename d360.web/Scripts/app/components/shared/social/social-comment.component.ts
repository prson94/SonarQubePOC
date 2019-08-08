import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { BaseComponent } from '../base.component';
import { SocialService } from '../../../services/social.service';
import { SocialComment, SocialVoteType, SocialCommentType } from '../../../models/social.model';
import { Router, NavigationEnd } from '@angular/router';
import { CurrentCompanySettings } from '../../../static/company-settings'

@Component({
    selector: 'd3s-social-comment',    
    template: ` 
                <div class="row comment" (mouseenter)="showTools=true" (mouseleave)="showTools=false" [ngStyle]="{'background':(showTools ? '#EFEFEF': '')}">                                
                    <div class="col s1 right-align">
                        <img class="user" height="35" [src]="'/resources/image/' + comment.CreatingResourceID + '?size=35'" width="35">                        
                    </div>
                    <div class="col s11">
                        <div class="row" *ngIf="!showEdit">
                            <div class="col s12 toolbox">                                
                                <span class="commentType"><i class="fa" [ngClass]="{'fa-comment blue-text': isSocial() ,'fa-question-circle purple-text': isChallenge(), 'fa-exclamation-triangle orange-text': isIssue()}" aria-hidden="true" ></i></span>&nbsp;<span class="user">                                    
                                    <d3s-preview-tooltip objectType="Resource" [objectId]="comment.CreatingResourceID">{{comment.ResourceName}}</d3s-preview-tooltip>
                                </span>&nbsp;<span class="postDate">{{comment.DateCreated | date:'medium'}}</span> 
                                <div *ngIf="showTools" class="comment-tools">
                                    <a *ngIf="canReply()" class="comment-tool-item-mid" (click)="showReply=true;"><i class="fa fa-reply" aria-hidden="true" ></i></a>
                                    <a *ngIf="comment.IsDeletable" class="comment-tool-item-mid" (click)="deleteCommentClick();"><i class="fa fa-trash-o" aria-hidden="true" ></i></a>                                    
                                    <a *ngIf="comment.IsEditable" class="comment-tool-item-mid" (click)="showEdit = true;editText = comment.Body"><i class="fa fa-pencil-square-o" aria-hidden="true" ></i></a>                                    
                                    <a class="comment-tool-item-mid" (click)="doVote(socialVoteType.UpVote);"><d3s-tooltip [objectType]="'Comment/Votes'" [objectId]="comment.ID" [tooltipType]="'up'" [icon]="'thumbs-o-up'" [iconColor]="'#646464'"></d3s-tooltip> {{upVotes}}</a>
                                    <a class="comment-tool-item-mid" (click)="doVote(socialVoteType.DownVote);"><d3s-tooltip [objectType]="'Comment/Votes'" [objectId]="comment.ID" [tooltipType]="'down'" [icon]="'thumbs-o-down'" [iconColor]="'#646464'"></d3s-tooltip> {{downVotes}}</a>
                                </div>                      
                            </div>
                            <div class="col s12" [innerHtml]="comment.Body"></div>                            
                            <div class="col s12">
                                <i class="fa fa-tag" aria-hidden="true"></i> Comment Tags: <d3s-preview-tooltip *ngFor="let tag of comment.Tags" class="comment-tag" (click)="changeUrl(tag.Url)" [objectType]="tag.Object" [objectId]="tag.ObjectID" [iconColor]="tag.IconForeColor" [foreColor]="tag.IconBackColor">{{tag.TextPath}}</d3s-preview-tooltip>
                            </div>
                        </div>                        
                        <div class="row" *ngIf="showEdit">
                            <div class="col s11 offset-s1" style="padding-top:15px">   
                                <p-editor name="Edit" [style]="{'height':'50px'}" [(ngModel)]="editText" ></p-editor>                 
                            </div>
                            <div class="col s11 offset-s1" style="padding-top:15px;padding-botton:15px;">   
                                <button pButton type="button" (click)="handleEditClick();" label="Edit"></button>
                                <button pButton type="button" (click)="showEdit = false;" label="Cancel"></button>
                            </div>
                        </div>
                    </div>                                    
                </div> 
                <div class="row add-reply" *ngIf="showReply">
                    <div class="col s11 offset-s1" style="padding-top:15px">   
                        <p-editor placeholder="Post Reply..." name="Reply" [style]="{'height':'50px'}" [(ngModel)]="replyText" ></p-editor>                 
                    </div>
                    <div class="col s11 offset-s1" style="padding-top:15px;padding-botton:15px;">   
                        <button pButton type="button" (click)="handleReplyClick();" label="Post"></button>
                        <button pButton type="button" (click)="showReply = false;replyText='';" label="Cancel"></button>
                    </div>
                </div>   
                <div class="row reply" *ngFor="let response of comment?.Comments">
                    <div class="col s2 right-align"><img class="user" height="35" [src]="'/resources/image/' + response.CreatingResourceID + '?size=35'" width="35"></div>
                    <div class="col s10">
                        <div><span class="user"><d3s-preview-tooltip [objectType]="'Resource'" [objectId]="comment.CreatingResourceID">{{response.ResourceName}}</d3s-preview-tooltip></span>&nbsp;<span class="postDate">{{response.DateCreated | date:'medium'}}</span>                        
                        <div [innerHtml]="response.Body"></div>                            
                    </div>                                
                </div>                 
                `,    
    styles: [`
                span.user{
                    font-weight:bold;
                }
                span.postDate{
                    color: #AAAAAA;                    
                }
                img.user{
                    border-radius:5px;
                }                                              
                .comment-tag{
                    border-radius: 5px;
                    margin-right: 5px;
                    padding: 3px 10px;
                    cursor:pointer;
                }
                .comment, .reply{
                    padding:5px 0;
                }
                .comment-tool-item :hover, .comment-tool-item-mid :hover{
                    color:rgba(84,164,218,1);
                }
                .comment-tool-item, .comment-tool-item-mid{
                    padding:5px;
                    font-size:1.4em;
                    color:#646464;
                    cursor:pointer;
                }
                .comment-tool-item-mid{
                    border-right:1px solid #AAAAAA;
                }
                .comment-tools{                                  
                    display:inline-block;
                    position:absolute;
                    top: -.50rem;
                    right: .25rem;
                    border: 1px solid #AAAAAA;
                    border-radius: 5px;
                    box-sizing:border-box;
                    overflow:hidden;
                    background:white;                    
                }
                .toolbox{
                    position:relative;
                }
            `]

})

export class SocialCommentComponent extends BaseComponent implements OnInit {
    @Input() comment: SocialComment;

    @Output() delete = new EventEmitter();
    @Output() reply = new EventEmitter();
    @Output() edit = new EventEmitter();

    private upVotes: number = 0;
    private downVotes: number = 0;

    private showTools: boolean = false;
    private showReply: boolean = false;
    private showEdit: boolean = false;
    
    private replyText: string = "";    
    private editText: string = "";

    private socialVoteType = SocialVoteType; // for template to use enum
       

    constructor(private socialService: SocialService, private router: Router) {
        super();
    }

    ngOnInit() {
        if (this.comment && this.comment.Votes) {
            this.calculateVotes();               
        }
    }   
    
    private calculateVotes() {
        this.upVotes = this.comment.Votes.filter(res => res.Vote == SocialVoteType.UpVote).length;
        this.downVotes = this.comment.Votes.filter(res => res.Vote == SocialVoteType.DownVote).length;
    }

    private doVote(vote: SocialVoteType) {
        this.socialService.vote(this.comment.ID, vote).subscribe(
            res => {
                if (res) {                    
                    this.comment.Votes = res;                    
                    this.calculateVotes();    
                }
            });
    }

    private deleteCommentClick() {
        this.delete.emit({ comment: this.comment });
    }

    private changeUrl(route) {
        this.router.navigate([route]); 
    }

    private commentTypeIcon() {
        switch (this.comment.CommentTypeID) {
            case SocialCommentType.Challenge:
                return "Challenge";
            case SocialCommentType.Issue:
                return "Issue";
            case SocialCommentType.Social:
                return "";
            case SocialCommentType.Task:
                return "Task";            
        }

        return "Other";
    }

    private handleReplyClick() {        
        this.reply.emit({ reply: this.replyText, commentId: this.comment.ID });
        this.showReply = false;
    }

    private handleEditClick() {
        this.comment.Body = this.editText;
        this.edit.emit({ comment: this.comment });
        this.showEdit = false;
    }

    private isChallenge(): boolean {
        return this.comment.CommentTypeID == SocialCommentType.Challenge;
    }

    private isSocial(): boolean {        
        return this.comment.CommentTypeID == SocialCommentType.Social;
    }

    private isIssue(): boolean {
        return this.comment.CommentTypeID == SocialCommentType.Issue;
    }

    private canReply(): boolean {
        return !CurrentCompanySettings.disableCommunityPosting;
    }
};