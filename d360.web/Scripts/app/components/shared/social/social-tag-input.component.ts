import { Component, Input, Output, EventEmitter, HostBinding } from '@angular/core';
import { BaseComponent } from '../base.component';
import { TagService } from '../../../services/tag.service';
import { Tag } from '../../../models/tag.model';
import { D3SObjectHelpers } from '../../../static/d3s-object-helpers';

@Component({
    selector: 'd3s-social-tag-input',
    template: `
           <p-autoComplete size="50"
                            scrollHeight="400px"
                            [(ngModel)]="tag" 
                            [suggestions]="tags" 
                            (completeMethod)="search($event)" 
                            field="TextPath"  
                            placeholder="Tag an item"
                            (onSelect)="selectItem()">   
                            <ng-template let-item pTemplate="item">
                                   <span style="color:#999999;">{{userFriendlyObjectName(item.Object)}} - <span *ngIf="item.ObjectTypeName">{{item.ObjectTypeName}} -</span></span> {{item.TextPath}} <span *ngIf="item.GoverningDomain">({{item.GoverningDomain}})</span>
                            </ng-template>  
                    </p-autoComplete>
        `,
    providers: [TagService],
})

export class SocialTagInputComponent extends BaseComponent {
    @Output() selectTag = new EventEmitter();
        
    private tags : Tag[] = [];
    private tag : Tag;

    constructor(private tagService: TagService) {
        super();
    }

    private search(event) {
        this.tagService.getTags(event.query).then(data => {
            this.tags = data;
        }); 
    }

    private selectItem() {
        this.selectTag.emit({
            tag: this.tag
        });
        this.tag = null;
    }
    private userFriendlyObjectName(objectType: string) {
        return D3SObjectHelpers.getObjectTypeFriendlyName(objectType);
    }
}