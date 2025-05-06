import { Component, EventEmitter, Input, Output } from "@angular/core";
import { cloneDeep } from "lodash-es";
import * as DOMPurify from "isomorphic-dompurify";
import { BaseComponent } from "../../components/shared/base.component";
import { CommentDetail, CommentApiPutModel, CommentApiPostModel } from "../../models/social.model";
import { AuthenticationService } from "../../services/authentication.service";
import { MessagesObservableService } from "../../services/messages-observable.service";
import { CompanySettingsService } from "../../services/settings.service";
import { SocialService } from "../../services/social.service";
import { Tag } from "../../models/tag.model";
import { SocialTagInput } from "./social-tag-input";
import { EditorModule } from "primeng/editor";
import { ButtonModule } from "../../directives/ig-button-directive";
import { DirectivesModule } from "../../directives/directives.module";
import { CoreModule } from "../../components/shared/core.module";
import { FormsModule } from "@angular/forms";

@Component({
    selector: "comment-form",
	templateUrl: "comment-form.html",
	standalone: true,
	imports: [DirectivesModule, ButtonModule, CoreModule, EditorModule, FormsModule, SocialTagInput]
})

export class CommentForm extends BaseComponent {
    @Input() comment: CommentDetail;
    @Input() parentUid: string = "";
    @Input() assetUid: string = "";
    @Input() isVisible: boolean = false;

    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    labelUpdate = $localize`Update`;
    labelPost = $localize`Post`;

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
			this.comment.Body = DOMPurify.sanitize(this.comment.Body);
            this.originalComment = cloneDeep(this.comment);
        }
    }

    addTag(event) {
        if (!this.comment.Tags) {
            this.comment.Tags = [];
        }
        this.comment.Tags.push(event.tag);
    }

    removeTag(tag: Tag) {
        const index = this.comment.Tags.findIndex((x) => x.AssetUid === tag.AssetUid);

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
            const putModel = new CommentApiPutModel();

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
			const postModel = new CommentApiPostModel();
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