import { Input, Component, EventEmitter, Output, OnInit } from "@angular/core";
import { BaseComponent } from "../base.component";
import { SocialService } from "../../../services/social.service";
import { CommentDetail, CommentType, Emoji } from "../../../models/social.model";
import { Router } from "@angular/router";
import { CurrentCompanySettings } from "../../../static/company-settings"

declare var CurrentResourceID;

@Component({
    selector: "d3s-social-comment",    
    templateUrl: "./social-comment.component.html",    
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
                .comment-removed {
                    border-radius: 3px;
                    padding: 5px;
                    background: #EFEFEF;
                    border: 1px solid #CCCCCC;
                    font-size: 90%;
                }       
                .comment-modified {
                    margin-left: 5px;
                    font-style: oblique;
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
    @Input() comment: CommentDetail;
    @Input() isAdmin: boolean;

    @Output() delete = new EventEmitter();
    @Output() reply = new EventEmitter();
    @Output() edit = new EventEmitter();

    upVotes: number = 0;
    downVotes: number = 0;

    showTools: boolean = false;
    showReply: boolean = false;
    showEdit: boolean = false;
    
    replyText: string = "";    
    editText: string = "";

    isDeletable: boolean = false;
    isEditable: boolean = false;

    //public emoji: Emoji;

    constructor(private socialService: SocialService, private router: Router) {
        super(); 
    } 

    ngOnInit(): void {
        this.isDeletable = this.isAdmin || (this.comment.CreatedBy == CurrentResourceID);
        this.isEditable = this.comment.CreatedBy == CurrentResourceID;
        this.downVotes = this.comment.Emojis.filter((e) => e.Emoji == Emoji.ThumbsDown).length;
        this.upVotes = this.comment.Emojis.filter((e) => e.Emoji == Emoji.ThumbsUp).length;
    }

    doVote(emojiString: string) {
        let emoji: Emoji = Emoji[emojiString];

        if (emoji == Emoji.ThumbsDown) {
            this.downVotes++;
        }
        else {
            this.upVotes++;
        }

        this.socialService.addVote(this.comment.Uid, emoji).subscribe(
            res => {
                if (res) {
                    this.comment.Emojis.find((e) => e.Emoji == emoji).Count++;
                }
            });
    }

    private deleteCommentClick() {
        this.delete.emit({ comment: this.comment });
    }

    private changeUrl(route) {
        this.router.navigate([route]); 
    }

    isModified() {
        return (this.comment.CreatedOn != this.comment.UpdatedOn);
    }

    private commentTypeIcon() {
        switch (this.comment.CommentType) {
            case CommentType.Issue:
                return "Issue";
            case CommentType.Social:
                return "";
        }

        return "Other";
    }

    handleReplyClick() {
        this.reply.emit({ reply: this.replyText, parentUid: this.comment.Uid });
        this.showReply = false;
    }

    handleEditClick() {
        this.comment.UpdatedOn = new Date(0); //Just to show the modified symbol.
        this.comment.Body = this.editText;
        this.edit.emit({ comment: this.comment });
        this.showEdit = false;
    }

    isSocial(): boolean {        
        return this.comment.CommentType == CommentType.Social;
    }

    isIssue(): boolean {
        return this.comment.CommentType == CommentType.Issue;
    }

    canReply(): boolean {
        return !CurrentCompanySettings.disableCommunityPosting;
    }
}