import { Input, Component, EventEmitter, Output, OnInit, HostBinding } from "@angular/core";
import { BaseComponent } from "../base.component";
import { SocialService } from "../../../services/social.service";
import { CommentApiPostModel, CommentApiPutModel, CommentDetail, CommentType } from "../../../models/social.model";
import { CurrentCompanySettings } from "../../../static/company-settings"
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { AuthenticationService } from "../../../services/authentication.service";

@Component({
    selector: "d3s-social-board",
    templateUrl: "./social-board.component.html", 
    providers: [SocialService],       
})

export class SocialBoardComponent extends BaseComponent implements OnInit {
    @Input() followerUid: string;
    @Input() assetUid: string;
    @Input() hasCloseButton: boolean = false;
    @Input() hasNewInput: boolean = true;
    @Input() daysToLookBack: number = -1;
    @Input() limitToType: CommentType;

    @Output() countsChanged = new EventEmitter();
    @Output() close = new EventEmitter();
    
    rowCount: number = 15;
    pageNumber: number = 1;
    hasMore: boolean = true;
    comments: CommentDetail[] = [];
    socialMessage: string;

    isAdmin: boolean = false;

    constructor(private authenticationService: AuthenticationService, private socialService: SocialService, protected messagesService: MessagesObservableService) {
        super();
    }

    ngOnInit() {

        if (this.objectID > 0) {
            this.socialMessage = null;
        }
        else {
            if (this.limitToType == CommentType.Social) {
                this.socialMessage = `My comments ${this.daysMessage()}`;
            }
            else if (this.limitToType == CommentType.Issue) {
                this.socialMessage = `My issues ${this.daysMessage()}`;
            }
            else {
                this.socialMessage = "My comments";
            }
        }

        this.authenticationService.checkCurrentUserAdmin().subscribe((res) => {
            this.isAdmin = res;
            this.loadComments();
        });
    }

    private daysMessage(): string {
        return this.daysToLookBack > 0 ? ("for the last " + this.daysToLookBack + " days") : "- all";
    }

    loadComments() {
        this.isLoading = true;
        if (this.assetUid) {
            this.socialService.getComments(this.assetUid, this.daysToLookBack, this.pageNumber, this.rowCount, this.limitToType)
                .subscribe(res => {
                    this.isLoading = false;
                    this.comments = this.comments.concat(res.comments);
                    this.hasMore = (res.count > this.comments.length);
                    this.pageNumber++;
                });
        }
        else {
            this.socialService.getCommentForFollowers(this.followerUid, this.daysToLookBack, this.pageNumber, this.rowCount, this.limitToType)
                .subscribe(res => {
                    this.isLoading = false;
                    this.comments = this.comments.concat(res.comments);
                    this.hasMore = (res.comments.length && res.comments.length > 0);
                });
        }

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
                    let index = this.comments.findIndex((x) => x.ID == comment.ID);

                    if (index >= 0 && !(comment.Comments && comment.Comments.length > 0)) {
                        this.comments.splice(index,1);
                    }
                    this.messagesService.showInfoMessage("Success", "Item deleted successfully");
                }
                this.countsChanged.emit({}); // counts changed fire event
                this.isLoading = false;
            });
    }

    addComment(event) {
        let commentContent = event.comment;

        if (!commentContent) {
            return;
        }

        this.isLoading = true;
        let comment = new CommentApiPostModel();

        comment.Body = commentContent;
        comment.AssetUid = this.assetUid;
        comment.Body = commentContent;
        let taggedAssetUids: string[] = [];

        if (event.tags) {
            event.tags.forEach((t) => {
                taggedAssetUids.push(t.AssetUid);
            });
        }

        comment.Tags = taggedAssetUids;

        this.socialService.addComment(comment).
            subscribe(res => {                
                if (res) {
                    this.comments.unshift(res);                    
                }
                this.messagesService.showInfoMessage("Success", "Item added successfully");
                this.countsChanged.emit({}); // counts have changed fire event
                this.isLoading = false;
            });
    }

    editComment(event) {
        if (!event.comment) {
            return;
        }

        this.isLoading = true;

        let comment = new CommentApiPutModel();
        comment.Body = event.comment.Body;
        comment.Tags = event.tags;
        comment.Uid = event.comment.Uid;
        
        this.socialService.editComment(comment).
            subscribe(res => {       
                this.messagesService.showInfoMessage("Success", "Item edited successfully");
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
        
        if (!replyText || !parentUid) {
            return;
        }

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