///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SocialService } from '../../services/index';
import { SocialComment, SocialEditCommentData } from '../../models/social.model';

@Component({
    selector: 'd3s-social-board',
    template: ` <p-dataScroller [value]="comments" [rows]="rowCount">
                        <template let-comment>
                            <d3s-social-comment [comment]="comment" (delete)="deleteComment($event);"></d3s-social-comment>                            
                        </template>
                </p-dataScroller>                
                <button pButton type="button" [disabled]="!hasMore" (click)="loadComments();" label="Load more comments..." style="width: '150px';"></button>
                `,
    providers: [SocialService],    
})

export class SocialBoardComponent extends BaseComponent implements OnInit {
    @Input() objectID: number;
    @Input() objectType: string;
    @Input() objectName: string;
    

    private rowCount: number = 5;
    private pageNumber: number = 0;
    private hasMore: boolean = true;
    private comments: SocialComment[] = []

    constructor(private socialService: SocialService) {
        super();
    }

    ngOnInit() {
        this.loadComments();
    }

    loadComments() {
        this.isLoading = true;
        this.socialService.getComments(this.objectID, this.objectType, -1, (this.pageNumber) * this.rowCount, this.rowCount)
            .then(res => {
                this.isLoading = false;
                this.comments = this.comments.concat(res);
                this.hasMore = (res.length && res.length > 0);
            });
        this.pageNumber++;
    }

    private deleteComment(event) {
        let comment = event.comment;

        if (!comment) return;

        let editData = new SocialEditCommentData(comment, comment.Tags);
        editData.ObjectID = this.objectID;
        editData.ObjectType = this.objectType;
        editData.Comment.IsDeleted = true;

        this.socialService.editComment(editData).
            then(res => {
                if (res.IsDeleted) {
                    let index = this.comments.findIndex(x => x.ID == res.ID);

                    if (index >= 0) {
                        this.comments = this.comments.splice(index, 1);
                    }
                }
            });
    }
    
};