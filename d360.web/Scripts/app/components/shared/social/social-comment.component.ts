import { Input, Component, EventEmitter, Output, OnInit } from "@angular/core";
import { BaseComponent } from "../base.component";
import { SocialService } from "../../../services/social.service";
import { CommentApiPostModel, CommentDetail, CommentType, Emoji } from "../../../models/social.model";
import { Router } from "@angular/router";
import { CurrentCompanySettings } from "../../../static/company-settings"
import { ResourcesService } from "../../../services/resources.service";

declare var CurrentResourceID;

@Component({
    selector: "d3s-social-comment",
    templateUrl: "./social-comment.component.html",
})

export class SocialCommentComponent extends BaseComponent implements OnInit {
    @Input() comment: CommentDetail;
    @Input() isAdmin: boolean;
    @Input() assetUid: string = "";

    @Output() delete = new EventEmitter();
    @Output() edit = new EventEmitter();



    upVotes: number = 0;
    downVotes: number = 0;

    showTools: boolean = false;
    showReply: boolean = false;
    showEdit: boolean = false;

    replyData: CommentApiPostModel;

    isDeletable: boolean = false;
    isEditable: boolean = false;
    resourceUid: string = "";

    constructor(private socialService: SocialService, private router: Router) {
        super();
        this.replyData = new CommentApiPostModel();
    }

    ngOnInit(): void {
        this.isDeletable = this.isAdmin || (this.comment.CreatedBy == CurrentResourceID);
        this.isEditable = this.comment.CreatedBy == CurrentResourceID;

        this.calculateVotes();
    }

    doVote(emojiString: string) {
        let emoji: Emoji = Emoji[emojiString];

        if (this.isLoading === true) {
            return;
        }

        this.isLoading = true;

        this.socialService.addVote(this.comment.Uid, emoji)
            .subscribe((res) => {
                if (res) {
                    this.socialService.getCommentVotes(this.comment.Uid)
                        .subscribe((v) => {
                            this.comment.Emojis.forEach((e) => e.Count = 0);

                            v.forEach((i) => {
                                let emojis = this.comment.Emojis.find((e) => e.Emoji === Emoji[i.emoji]);
                                if (emojis) {
                                    emojis.Count++;
                                } else {
                                    this.comment.Emojis.push({ Emoji: Emoji[emoji], Count: 1 });
                                }
                            });
                            this.calculateVotes();
                            this.isLoading = false;
                        });
                }
            });
    }

    private calculateVotes() {
        this.downVotes = this.comment.Emojis.filter((e) => e.Emoji === Emoji[Emoji.ThumbsDown]).reduce((prev, curr) => prev + curr.Count, 0);
        this.upVotes = this.comment.Emojis.filter((e) => e.Emoji === Emoji[Emoji.ThumbsUp]).reduce((prev, curr) => prev + curr.Count, 0);
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

    isSocial(): boolean {
        return this.comment.CommentType == CommentType.Social;
    }

    isIssue(): boolean {
        return this.comment.CommentType == CommentType.Issue;
    }

    canReply(): boolean {
        return !CurrentCompanySettings.disableCommunityPosting;
    }

    private getTagName(tag: any) {
        if (tag.Path) {
            return tag.Path;
        }
        return tag.TextPath;
    }

    private getCommentUrl(comment: CommentDetail) {
        if (!comment.CreatedByUid) {
            return "";
        }

        return `/api/v2/membership/users/${comment.CreatedByUid}/image?size=35`;
    }
}