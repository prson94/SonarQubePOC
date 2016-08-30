///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SocialService } from '../../services/index';
import { SocialComment, SocialVoteType } from '../../models/social.model';

@Component({
    selector: 'd3s-social-comment',    
    template: ` 
                <div class="row comment" (mouseenter)="showTools=true" (mouseleave)="showTools=false">                                
                    <div class="col s1 right-align">
                        <img class="user" height="35" [src]="'/resources/image/' + comment.CreatingResourceID + '?size=35'" width="35">                        
                    </div>
                    <div class="col s11">
                        <div class="row">
                            <div class="col s12 toolbox">
                                <span class="user">{{comment.ResourceName}}</span> <span class="postDate">{{comment.DateCreated | date:'medium'}}</span>
                                <div *ngIf="showTools" class="comment-tools">
                                    <a class="comment-tool-item-mid" (click)="reply.emit();"><i class="fa fa-reply" aria-hidden="true" ></i></a>
                                    <a class="comment-tool-item-mid" (click)="delete.emit();"><i class="fa fa-trash-o" aria-hidden="true" ></i></a>                                    
                                    <a class="comment-tool-item-mid" (click)="doVote(socialVoteType.UpVote);"><d3s-tooltip [objectType]="'Comment/Votes'" [objectId]="comment.ID" [tooltipType]="'up'" [icon]="'thumbs-o-up'" [iconColor]="'#646464'"></d3s-tooltip> {{upVotes}}</a>
                                    <a class="comment-tool-item-mid" (click)="doVote(socialVoteType.DownVote);"><d3s-tooltip [objectType]="'Comment/Votes'" [objectId]="comment.ID" [tooltipType]="'down'" [icon]="'thumbs-o-down'" [iconColor]="'#646464'"></d3s-tooltip> {{downVotes}}</a>
                                </div>                      
                            </div>
                            <div class="col s12" [innerHtml]="comment.Body"></div>                            
                            <div class="col s12">
                                <i class="fa fa-tag" aria-hidden="true"></i> Tags: <span *ngFor="let tag of comment.Tags" [ngStyle]="{'background':tag.IconBackColor, 'color':tag.IconForeColor}" class="comment-tag" (click)="visitTag(tag.Url)">{{tag.TextPath}} </span>
                            </div>
                        </div>                        
                    </div>                                    
                </div>    
                <div class="row reply" *ngFor="let response of comment?.Comments">
                    <div class="col s2 right-align"><img class="user" height="35" [src]="'/resources/image/' + response.CreatingResourceID + '?size=35'" width="35"></div>
                    <div class="col s10">
                        <div><span class="user">{{response.ResourceName}}</span> <span class="postDate">{{response.DateCreated | date:'medium'}}</span></div>
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
                :host :hover {
                    background: #EFEFEF;                    
                 }
                .comment-tag{
                    border-radius: 5px;
                    margin-right: 5px;
                    padding: 0 5px;
                    cursor:pointer;
                }
                .comment, .reply{
                    padding-bottom:10px;
                }
                .comment-tool-item, .comment-tool-item-mid{
                    padding:5px;
                    font-size:1.25em;
                    color:#646464;
                }
                .comment-tool-item-mid{
                    border-right:1px solid #646464;
                }
                .comment-tools{                                  
                    display:inline-block;
                    position:absolute;
                    top: 0;
                    right: .25rem;
                    border: 1px solid #646464;
                    border-radius: 5px;
                    box-sizing:border-box;
                    overflow:hidden;
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

    private upVotes: number = 0;
    private downVotes: number = 0;

    private showTools: boolean = false;

    public socialVoteType = SocialVoteType; // for template to use enum
    public upVote: SocialVoteType = SocialVoteType.UpVote;
    public downVote: SocialVoteType = SocialVoteType.DownVote;
    
    constructor(private socialService: SocialService) {
        super();
    }

    ngOnInit() {
        if (this.comment && this.comment.Votes) {
            this.calculateVotes();            
        }
    }   
    
    private visitTag(url: string): void {
        window.location.href = url;
    }    

    private calculateVotes() {
        this.upVotes = this.comment.Votes.filter(res => res.Vote == SocialVoteType.UpVote).length;
        this.downVotes = this.comment.Votes.filter(res => res.Vote == SocialVoteType.DownVote).length;
    }

    private findVoteByVoter(resourceId: number) {
        let indx = 0;
        for (let vote of this.comment.Votes) {
            if (vote.ResourceID == resourceId) return indx;
            indx++;
        }
        return -1;
    }

    private doVote(vote: SocialVoteType) {
        this.socialService.vote(this.comment.ID, vote).then(
            res => {
                if (res) {
                    this.comment.Votes = res;

                    this.calculateVotes();
                }
            });
    }

};