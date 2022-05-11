import { Component, EventEmitter, Input, Output } from "@angular/core";
import { CommentApiPostModel, CommentApiPutModel, CommentDetail } from "../../../models/social.model";
import { Tag } from "../../../models/tag.model";
import { AuthenticationService } from "../../../services/authentication.service";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { SocialService } from "../../../services/social.service";
import { BaseComponent } from "../base.component";
import * as _ from "lodash";
import { CompanySettingsService } from "../../../services/settings.service";

@Component({
    selector: "d3s-comment-form",
    templateUrl: "comment-form.component.html"
})

export class CommentFormComponent extends BaseComponent {
    @Input() comment: CommentDetail;
    @Input() parentUid: string = "";
    @Input() assetUid: string = "";
    @Input() isVisible: boolean = false;

    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    originalComment: CommentDetail;

    constructor(
        private authenticationService: AuthenticationService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private socialService: SocialService) {
        super(settingsService);
        this.comment = new CommentDetail();
        this.comment.Tags = [];
    }

    ngOnInit() {
        if (this.comment.Body) {
            this.originalComment = _.cloneDeep(this.comment);
        }
    }

    addTag(event) {
        if (!this.comment.Tags) {
            this.comment.Tags = [];
        }
        this.comment.Tags.push(event.tag);
    }

    removeTag(tag: Tag) {
        let index = this.comment.Tags.findIndex((x) => x.AssetUid === tag.AssetUid);

        if (index >= 0 && index < this.comment.Tags.length) {
            this.comment.Tags.splice(index, 1);
        }
    }

    cancelClick() {
        if (this.originalComment) {
            this.onSave.emit({ comment: this.originalComment, event: "edit" });
        }
        this.onCancel.emit();
    }

    saveClick() {
        this.isLoading = true;

        if (this.comment.Uid) {
            var putModel = new CommentApiPutModel();

            putModel.Body = this.comment.Body;
            putModel.Tags = this.comment.Tags.map((x) => x.AssetUid);
            putModel.Uid = this.comment.Uid;

            this.socialService.editComment(putModel).
                subscribe((res) => {
                    if (res) {
                        this.messagesService.showInfoMessage($localize`Success`, $localize`Item edited successfully`);
                        this.comment.UpdatedOn = new Date();
                        this.onSave.emit({ comment: this.comment, event: "edit" });
                    }
                    this.isLoading = false;
                });
        }
        else {
            var postModel = new CommentApiPostModel();
            postModel.AssetUid = this.assetUid;
            postModel.Body = this.comment.Body;
            postModel.Tags = this.comment.Tags.map((x) => x.AssetUid);

            if (this.parentUid) {
                postModel.ParentUid = this.parentUid;
            }

            this.socialService.addComment(postModel).
                subscribe((res) => {
                    if (res) {
                        this.messagesService.showInfoMessage($localize`Success`, $localize`Item added successfully`);
                        this.onSave.emit({ comment: res, event: "add" });
                    }
                    this.isLoading = false;  
                });
        }
    }

    private getTagName(tag: any) {
        if (tag.Path) {
            return tag.Path;
        }
        return tag.TextPath;
    }
}