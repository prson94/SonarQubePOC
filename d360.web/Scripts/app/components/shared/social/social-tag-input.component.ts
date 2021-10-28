import {debounceTime} from 'rxjs/operators';
import { Component, Output, EventEmitter, OnDestroy } from '@angular/core';
import { BaseComponent } from '../base.component';
import { TagService } from '../../../services/tag.service';
import { Tag } from '../../../models/tag.model';
import { D3SObjectHelpers } from '../../../static/d3s-object-helpers';
import { SubscriptionLike as ISubscription } from 'rxjs';
import { CompanySettingsService } from '../../../services/settings.service';

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
                                   <span style="color:#999999;">{{userFriendlyObjectName(item.Displayobject)}} - <span *ngIf="item.ObjectTypeName">{{item.ObjectTypeName}} -</span></span> {{item.TextPath}}
                            </ng-template>  
                    </p-autoComplete>
        `,
    providers: [TagService],
})

export class SocialTagInputComponent extends BaseComponent  implements OnDestroy{
    @Output() selectTag = new EventEmitter();
        
    tags: Tag[] = [];
    tag: Tag;

    private searchSub: ISubscription;
    constructor(
        protected settingsService: CompanySettingsService,
        private tagService: TagService) {
        super(settingsService);
    }

    ngOnDestroy(): void {
        if (this.searchSub) this.searchSub.unsubscribe();
    }

    search(event) {
        this.searchSub = this.tagService.getTags(event.query).pipe(
            debounceTime(400))
            .subscribe(data => {
            this.tags = data;
        }); 
    }

    selectItem() {
        this.selectTag.emit({
            tag: this.tag
        });
        this.tag = null;
    }

    userFriendlyObjectName(objectType: string) {
        return D3SObjectHelpers.getObjectTypeFriendlyName(objectType);
    }
}