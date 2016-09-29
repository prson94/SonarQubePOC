
import { Component, Input, Output, EventEmitter, HostBinding } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { TagService } from '../../services/index';
import { Tag } from '../../models/tag.model';

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
}