import { Input, Component, EventEmitter, Output, OnInit, ViewEncapsulation } from "@angular/core";
import { BaseComponent } from "../base.component";
import { SocialService } from "../../../services/social.service";
import { CommentDetail, CommentType } from "../../../models/social.model";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { AuthenticationService } from "../../../services/authentication.service";
import { ResourcesService } from "../../../services/resources.service";
import { forkJoin, Observable } from "rxjs";
import { CompanySettingsService } from "../../../services/settings.service";
import { CompanySettingEnum } from "../../../models/settings.model";

@Component({
    selector: "d3s-social-board",
    templateUrl: "./social-board.component.html",
    encapsulation: ViewEncapsulation.None,
    styleUrls: ["social-board.less"],
    providers: [SocialService, ResourcesService],
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
    isAddingNew: boolean = false;

    newComment: CommentDetail;

    constructor(private authenticationService: AuthenticationService,
        private socialService: SocialService,
        protected messagesService: MessagesObservableService,
        private resourcesService: ResourcesService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
        this.newComment = new CommentDetail();
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
                    this.updateResourceData();
                });
        }
        else {
            this.socialService.getCommentForFollowers(this.followerUid, this.daysToLookBack, this.pageNumber, this.rowCount, this.limitToType)
                .subscribe(res => {
                    this.isLoading = false;
                    this.comments = this.comments.concat(res.comments);
                    this.hasMore = (res.comments.length && res.comments.length > 0);
                    this.updateResourceData();
                });
        }

        this.pageNumber++;
    }

    allowComments(): boolean {
        return this.hasNewInput && !this.settingsService.getSettingById(CompanySettingEnum.DisableCommunityPosting).BooleanSetting.Value;
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

    closeEditor() {
        this.isAddingNew = false;
        this.newComment = new CommentDetail();
    }

    onSave(event: any) {
        this.isAddingNew = false;
        this.newComment = new CommentDetail();
        var comment = event.comment as CommentDetail;
        if (event.event === "add") {
            this.addComment(comment);
        }
        if (event.event === "edit") {
            let idx: number = this.comments.findIndex((x) => x.Uid === comment.Uid);
            this.comments[idx] = comment;
        }
        this.updateResourceData();
    }

    private addComment(comment: CommentDetail) {
        if (!comment.ParentID) {
            this.comments = [comment].concat(this.comments);
        }
        else {
            var parent = this.comments.find((x) => x.ID === comment.ParentID);
            if (!parent.Comments) {
                parent.Comments = [];
            }
            parent.Comments.push(comment);
        }
    }

    private cachedResourceData: any = {};
    updateResourceData() {
        var obsArr: Observable<any>[] = [];
        this.getUniqueResourcesFromComments().forEach((res) => {
            if (!this.cachedResourceData[res]) {
                obsArr.push(this.resourcesService.getResource(res));
            }
        });

        if (obsArr.length > 0) {
            forkJoin(obsArr).subscribe((results) => {
                results.forEach((result) => {
                    var data = result.items[0];
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
        var allResources = [];
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
        })
        return allResources;
    }
}