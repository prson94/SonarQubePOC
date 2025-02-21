import { Component, EventEmitter, Input, OnInit, Output } from "@angular/core";
import { Router } from "@angular/router";
import { Subscription } from 'rxjs';
import { BaseComponent } from "../../components/shared/base.component";
import { CommentDetail, CommentApiPostModel, Emoji, CommentType } from "../../models/social.model";
import { AssetService } from "../../services/asset.service";
import { CompanySettingsService } from "../../services/settings.service";
import { SocialService } from "../../services/social.service";
import { CommentForm } from "./comment-form";
import { CoreModule } from "../../components/shared/core.module";
import { UserAvatarModule } from "../../components/shared/small-widgets/user-avatar/user-avatar.module";
import { DatePipe } from "@angular/common";
import { PipesModule } from "../../pipes/pipes.module";

@Component({
    selector: "comment-item",
	templateUrl: "./comment-item.html",
	standalone: true,
	imports: [CommentForm, CoreModule, DatePipe, PipesModule, UserAvatarModule]
})

export class CommentItem extends BaseComponent implements OnInit {
    @Input() comment: CommentDetail;
    @Input() isAdmin: boolean;
	@Input() assetUid: string = "";
	@Input() commentsEnabled: boolean;
	@Input() isReply: boolean = false;

    @Output() delete = new EventEmitter();
    @Output() edit = new EventEmitter();

    upVotes: number = 0;
    downVotes: number = 0;

    showReply: boolean = false;
    showEdit: boolean = false;

    replyData: CommentApiPostModel;

    isDeletable: boolean = false;
    isEditable: boolean = false;
    resourceUid: string = "";
    checkpermission: Subscription;

    constructor(
        private assetService: AssetService,
        private socialService: SocialService,
        protected settingsService: CompanySettingsService,
        private router: Router) {
        super(settingsService);
        this.replyData = new CommentApiPostModel();
    }

    ngOnInit(): void {
		this.isDeletable = this.isAdmin || (this.comment.CreatedBy === this.settingsService.CurrentResourceID);
		this.isEditable = this.comment.CreatedBy === this.settingsService.CurrentResourceID;
		this.calculateVotes();
    }

    doVote(emojiString: string) {
        const emoji: Emoji = Emoji[emojiString];

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
                                const emojis = this.comment.Emojis.find((e) => e.Emoji === i.emoji);
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

    ngOnDestroy() {
        if (this.checkpermission) {
            this.checkpermission.unsubscribe();
        }
    }

     private changeUrlwithPermission(route, uid) {
          const routeValue: string = route.toString().toLocaleLowerCase();
          if (uid !== null && !this.isAdmin && routeValue.substring(0,6) === "asset/") {
            if (this.checkpermission) {
                this.checkpermission.unsubscribe();
            }
               this.checkpermission = this.assetService.getAsset(uid)
                .subscribe((res) => {
                    if (res) {
                        this.router.navigate([route]);
                    }
                });
        }
        else {
            this.router.navigate([route]);
        }
    }

    isModified() {
        return (this.comment.CreatedOn !== this.comment.UpdatedOn);
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

    private getTagName(tag: any) {
        if (tag.Path) {
            return tag.Path;
        }
        return tag.TextPath;
    }
}