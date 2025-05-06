import { Component, EventEmitter, Input, OnInit, Output, ViewEncapsulation } from "@angular/core";
import { forkJoin, Observable } from "rxjs";
import { BaseComponent } from "../../components/shared/base.component";
import { CompanySettingEnum } from "../../models/settings.model";
import { CommentType, CommentDetail } from "../../models/social.model";
import { AuthenticationService } from "../../services/authentication.service";
import { MessagesObservableService } from "../../services/messages-observable.service";
import { ResourcesService } from "../../services/resources.service";
import { CompanySettingsService } from "../../services/settings.service";
import { SocialService } from "../../services/social.service";
import { LoadingComponent } from "./loading";
import { CommentForm } from "./comment-form";
import { CommentItem } from "./comment-item";
import { ButtonModule } from "../../directives/ig-button-directive";

@Component({
    selector: "social-board",
	templateUrl: "./social-board.html",
	standalone: true,
	imports: [ButtonModule, CommentForm, CommentItem, LoadingComponent],
    encapsulation: ViewEncapsulation.None
})

export class SocialBoard extends BaseComponent implements OnInit {
    @Input() followerUid: string;
    @Input() assetUid: string;
    @Input() hasCloseButton: boolean = false;
    @Input() daysToLookBack: number = -1;
    @Input() limitToType: CommentType;

    @Output() countsChanged = new EventEmitter();
    @Output() close = new EventEmitter();

    rowCount: number = 15;
    pageNumber: number = 1;
    hasMore: boolean = true;
    comments: CommentDetail[] = [];

    isAdmin: boolean = false;
    isAddingNew: boolean = false;

    newComment: CommentDetail;

	commentsEnabled: boolean = true;

    constructor(private authenticationService: AuthenticationService,
        private socialService: SocialService,
        protected messagesService: MessagesObservableService,
        private resourcesService: ResourcesService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
        this.newComment = new CommentDetail();
    }

    ngOnInit() {
		this.commentsEnabled = !this.settingsService.getSettingById(CompanySettingEnum.DisableCommunityPosting).BooleanSetting.Value;

        this.authenticationService.checkCurrentUserAdmin().subscribe((res) => {
            this.isAdmin = res;
            this.loadComments();
        });
    }

    loadComments() {
        this.isLoading = true;
        if (this.assetUid) {
            this.socialService.getComments(this.assetUid, this.daysToLookBack, this.pageNumber, this.rowCount, this.limitToType)
                .subscribe((res) => {
                    this.isLoading = false;
                    this.comments = this.comments.concat(res.comments);
                    this.hasMore = (res.count > this.comments.length);
                    this.pageNumber++;
                    this.updateResourceData();
                });
        }
        else {
            this.socialService.getCommentForFollowers(this.followerUid, this.daysToLookBack, this.pageNumber, this.rowCount, this.limitToType)
                .subscribe((res) => {
                    this.isLoading = false;
                    this.comments = this.comments.concat(res.comments);
					this.hasMore = (res.comments.length && res.comments.length > 0);
					this.pageNumber++;
                    this.updateResourceData();
                });
        }
    }

    deleteComment(event) {
        const comment = event.comment as CommentDetail;

        if (!comment) {return;}

        this.isLoading = true;

        this.socialService.deleteComment(comment.Uid).
            subscribe((res) => {
                if (res) {
                    comment.IsDeleted = true;
                    const index = this.comments.findIndex((x) => x.ID === comment.ID);

                    if (index >= 0 && !(comment.Comments && comment.Comments.length > 0)) {
                        this.comments.splice(index, 1);
                    }
                    this.messagesService.showInfoMessage($localize`Success`, $localize`Item deleted successfully`);
                }
                this.countsChanged.emit({}); // counts changed fire event
                this.isLoading = false;
            });
    }

    closeEditor() {
        this.isAddingNew = false;
        this.newComment = new CommentDetail();
    }

    onSave(event: any) {
        this.isAddingNew = false;
        this.newComment = new CommentDetail();
		const comment = event.comment as CommentDetail;
        if (event.event === "add") {
            this.addComment(comment);
        }
        if (event.event === "edit") {
            const idx: number = this.comments.findIndex((x) => x.Uid === comment.Uid);
            this.comments[idx] = comment;
        }
        this.updateResourceData();
    }

    private addComment(comment: CommentDetail) {
        if (!comment.ParentID) {
            this.comments = [comment].concat(this.comments);
        }
        else {
			const parent = this.comments.find((x) => x.ID === comment.ParentID);
            if (!parent.Comments) {
                parent.Comments = [];
            }
            parent.Comments.push(comment);
        }
    }

    private cachedResourceData: any = {};
    updateResourceData() {
		const obsArr: Observable<any>[] = [];
        this.getUniqueResourcesFromComments().forEach((res) => {
            if (!this.cachedResourceData[res]) {
                obsArr.push(this.resourcesService.getResource(res));
            }
        });

        if (obsArr.length > 0) {
            forkJoin(obsArr).subscribe((results) => {
                results.forEach((result) => {
					const data = result.items[0];
                    if (!this.cachedResourceData[data.ResourceID]) {
                        this.cachedResourceData[data.ResourceID] = data.uid;
                    }
                });

                this.updateCommentData();
            });
        }
        else {
            this.updateCommentData();
        }
    }

    updateCommentData() {
        this.comments.forEach((comment) => {
            comment.CreatedByUid = this.cachedResourceData[comment.CreatedBy];

            if (comment.Comments && comment.Comments.length > 0) {
                comment.Comments.forEach((x) => {
                    x.CreatedByUid = this.cachedResourceData[x.CreatedBy];
                });
            }
        });
    }

    getUniqueResourcesFromComments(): number[] {
		const allResources = [];
        this.comments.forEach((comment) => {
            if (!allResources.some((x) => {
                x === comment.CreatedBy;
            })) {
                allResources.push(comment.CreatedBy);
            }
            if (comment.Comments && comment.Comments.length > 0) {
                comment.Comments.forEach((comm) => {
                    if (!allResources.some((x) => x === comm.CreatedBy)) {
                        allResources.push(comm.CreatedBy);
                    }
                });
            }
        });
        return allResources;
    }
}