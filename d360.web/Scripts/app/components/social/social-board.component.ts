///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SocialService } from '../../services/index';
import { SocialComment } from '../../models/social.model';

@Component({
    selector: 'd3s-social-board',
    template: ` 
                <p-dataScroller [value]="comments" [rows]="rowCount">
                        <template let-comment>
                            <div class="row">                                
                                <div class="col s1 right-align"><img class="user" height="35" [src]="'/resources/image/' + comment.CreatingResourceID + '?size=35'" width="35"></div>
                                <div class="col s11">
                                    <div><span class="user">{{comment.ResourceName}}</span> <span class="postDate">{{comment.DateCreated | date:'medium'}}</span></div>
                                    <div [innerHtml]="comment.Body"></div>                            
                                </div>                            
                            </div>    
                            <div class="row" *ngFor="let response of comment?.Comments">
                                <div class="col s2 right-align"><img class="user" height="35" [src]="'/resources/image/' + response.CreatingResourceID + '?size=35'" width="35"></div>
                                <div class="col s10">
                                    <div><span class="user">{{response.ResourceName}}</span> <span class="postDate">{{response.DateCreated | date:'medium'}}</span></div>
                                    <div [innerHtml]="response.Body"></div>                            
                                </div>                                
                            </div>                                                                                    
                        </template>
                </p-dataScroller>                
                <button pButton type="button" [disabled]="!hasMore" (click)="loadComments();" label="Load more comments..." style="width: '150px';"></button>
                `,
    providers: [SocialService],
    styles: [`
                span.user{
                    font-weight:bold;
                }
                span.postDate{
                    color: #CCCCCC;                    
                }
                img.user{
                    border-radius:5px;
                }
            `]
    
})

export class SocialBoardComponent extends BaseComponent implements OnInit {
    @Input() objectID: number;
    @Input() objectType: string;
    @Input() objectName: string;
    @Input() daysToLookBack: number = 7;

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
        this.socialService.getComments(this.objectID, this.objectType, this.daysToLookBack, this.pageNumber++, this.rowCount)
            .then(res => {
                this.isLoading = false;
                this.comments = this.comments.concat(res);
                this.hasMore = (res.length && res.length > 0);
            });
    }
    
};