///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output, OnInit, HostBinding } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SocialService } from '../../services/index';
import { SocialComment, SocialEditCommentData } from '../../models/social.model';

@Component({
    selector: 'd3s-social-input',
    template: ` 
                <div class="row comment-input">
                    <div class="col s12">
                        <p-editor placeholder="What's happening?" name="Description" [style]="{'height':'50px'}" [(ngModel)]="comment" ></p-editor>
                    </div>                    
                    <div class="col s12" style="padding-top:15px;padding-botton:15px;">
                        <button class="right" pButton type="button" (click)="handleCommentClick();" label="Comment" style="width: '150px';"></button>
                    </div>
                </div>                
                `,
    styles: [`            
        `],
})

export class SocialInputComponent extends BaseComponent implements OnInit {    
    @Output() commented = new EventEmitter();
    
    private comment: string;

    ngOnInit() {

    }

    private handleCommentClick() {
        this.commented.emit({
            comment: this.comment
        });
        this.comment = "";
    }
};