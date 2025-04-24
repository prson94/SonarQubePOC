import { debounceTime, switchMap, distinctUntilChanged } from 'rxjs/operators';
import { Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { Subject, SubscriptionLike as ISubscription } from 'rxjs';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { BaseComponent } from '../../components/shared/base.component';
import { CompanySettingsService } from '../../services/settings.service';
import { TagService } from '../../services/tag.service';
import { D3SObjectHelpers } from '../../static/d3s-object-helpers';
import { FormsModule } from '@angular/forms';
import { Tag } from '../../models/tag.model';

@Component({
	selector: 'social-tag-input',
	standalone: true,
	imports: [AutoCompleteModule, FormsModule],
    template: `
           <p-autoComplete size="50"
                            scrollHeight="400px"
                            [(ngModel)]="tag" 
                            [suggestions]="tags" 
                            (completeMethod)="search($event)" 
                            field="TextPath"  
                            i18n-placeholder
                            placeholder="Tag an item"
                            (onSelect)="selectItem()">   
                            <ng-template let-item pTemplate="item">
                                   <span style="color:#999999;">{{userFriendlyObjectName(item.Displayobject)}} - <span *ngIf="item.ObjectTypeName">{{item.ObjectTypeName}} -</span></span> {{item.TextPath}}
                            </ng-template>  
                    </p-autoComplete>
        `
})

export class SocialTagInput extends BaseComponent  implements OnInit, OnDestroy{
    @Output() selectTag = new EventEmitter();
        
    tags: Tag[] = [];
    tag: Tag;

	private searchTerm$ = new Subject<string>();

    private searchSub: ISubscription;
    constructor(
        protected settingsService: CompanySettingsService,
        private tagService: TagService) {
        super(settingsService);
	}

	ngOnInit(): void {
		this.createSubscription();
	}

    ngOnDestroy(): void {
        if (this.searchSub) {this.searchSub.unsubscribe();}
	}

	createSubscription() {
		if (this.searchSub) { this.searchSub.unsubscribe(); }

		this.searchSub = this.searchTerm$.pipe(
			debounceTime(400),
			distinctUntilChanged(),
			switchMap((term) => {
				return this.tagService.getTags(term);
			})
		).subscribe((data) => {
			this.tags = data;
		});
	}
	
	search(event) {
		this.searchTerm$.next(event.query);
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