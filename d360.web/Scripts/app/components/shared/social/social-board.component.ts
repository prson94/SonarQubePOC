import { Input, Component, EventEmitter, Output, OnInit, HostBinding, ViewEncapsulation } from "@angular/core";
import { BaseComponent } from "../base.component";
import { SocialService } from "../../../services/social.service";
import { CommentApiPostModel, CommentApiPutModel, CommentDetail, CommentType } from "../../../models/social.model";
import { CurrentCompanySettings } from "../../../static/company-settings"
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { AuthenticationService } from "../../../services/authentication.service";

@Component({
    selector: "d3s-social-board",
    templateUrl: "./social-board.component.html",
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['social-board.less'],
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
                        this.comments.splice(index, 1);
                    }
                    this.messagesService.showInfoMessage("Success", "Item deleted successfully");
                }
                this.countsChanged.emit({}); // counts changed fire event
                this.isLoading = false;
            });
    }
}