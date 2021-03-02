import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommentApiPostModel, CommentApiPutModel, CommentDetail, CommentDetails } from '../../../models/social.model';
import { Tag } from '../../../models/tag.model';
import { AuthenticationService } from '../../../services/authentication.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { SocialService } from '../../../services/social.service';
import { BaseComponent } from '../base.component';

@Component({
    selector: 'd3s-comment-form',
    templateUrl: 'comment-form.component.html'
})

export class CommentFormComponent extends BaseComponent {
    @Input() comment: CommentDetail;
    @Input() parentUid: string = '';
    @Input() assetUid: string = '';
    @Input() isVisible: boolean = false;

    @Output() onCancel: EventEmitter<any> = new EventEmitter();

    constructor(private authenticationService: AuthenticationService,
        private socialService: SocialService,
        protected messagesService: MessagesObservableService) {
        super();
        this.comment = new CommentDetail();
        this.comment.Tags = [];
    }

    addTag(event) {
        if (!this.comment.Tags)
            this.comment.Tags = [];
        this.comment.Tags.push(event.tag);
    }

    removeTag(tag: Tag) {
        let index = this.comment.Tags.findIndex(x => x.AssetUid == tag.AssetUid);

        if (index >= 0 && index < this.comment.Tags.length) {
            this.comment.Tags.splice(index, 1);
        }
    }

    cancelClick() {
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
                subscribe(res => {
                    this.messagesService.showInfoMessage("Success", "Item edited successfully");
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
                subscribe(res => {
                    this.messagesService.showInfoMessage("Success", "Item added successfully");
                    this.isLoading = false;
                });
        }



    }

    private changeUrl(route) {
        console.log("chaning route");
        return;
        //this.router.navigate([route]);
    }

    private getTagName(tag: any) {
        if (tag.Path) {
            return tag.Path;
        }
        return tag.TextPath;
    }
}