///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SocialService } from '../../services/index';
import { SocialComment } from '../../models/social.model';

@Component({
    selector: 'd3s-social-board',
    template: ` 
                    <p-dataScroller [value]="comments" [rows]="10">
                        <template let-comment>
                            <div class="row">
                                <div class="col s2"></div>
                                <div class="col s10">
                                    <div>{{comment.ResourceName}} <span>{{comment.DateCreated | date:'medium'}}</span></div>
                                    <div [innerHtml]="comment.Body"></div>                            
                                </div>
                            </div>                            
                        </template>
                    </p-dataScroller>

                `,
    providers: [SocialService]  
})

export class SocialBoardComponent extends BaseComponent implements OnInit {
    @Input() objectID: number;
    @Input() objectType: string;
    @Input() objectName: string;
    @Input() daysToLookBack: number = 7;

    private comments: SocialComment[] = []

    constructor(private socialService: SocialService) {
        super();
    }

    ngOnInit() {
        this.loadComments();
    }

    loadComments() {
        this.isLoading = true;
        this.socialService.getComments(this.objectID, this.objectType, this.daysToLookBack)
            .then(res => {
                this.isLoading = false;
                this.comments = res;
            });
    }
};