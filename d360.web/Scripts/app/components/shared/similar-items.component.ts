import { Component, Input, OnChanges, OnInit, OnDestroy, SimpleChanges, NgModule, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { UriBasedService } from '../../services/uri-based.service';
import { CommonModule } from '@angular/common';

import { HTTP_INTERCEPTORS } from '@angular/common/http';      


import { CoreModule } from './core.module';
import { RouterModule } from '@angular/router';
import { Subject, Subscription } from 'rxjs';

@Component({
    selector: 'd3s-similar-items',
    template: `     
    <div *ngIf="items.length > 0">
        <div style="color: #FFB230" i18n>The following items with similar names already exist:</div>
        <span *ngFor="let s of items; let i = index;">
            <d3s-preview-tooltip [objectType]="s.object" [objectId]="s.objectid"><a [routerLink]="s.Url">{{s.Name}}</a></d3s-preview-tooltip>            
            <span *ngIf="i < (items.length - 1)">,</span>&nbsp; 
        </span>
    </div>                  
                `,
    providers: [UriBasedService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SimilarItemsComponent implements OnChanges, OnInit, OnDestroy {
    @Input() uri: string = '';
    @Input() query: string = '';

    public items = [];
    queryStream$ = new Subject<string>();
    searchSub: Subscription;

    constructor(private ref: ChangeDetectorRef, private uriBasedService: UriBasedService) { }

    ngOnInit() {
        this.searchSub = this.uriBasedService.search(this.uri, this.queryStream$)
            .subscribe(res => {
                if (this.query == '')
                    this.items = [];
                else
                    this.items = res;
                this.ref.markForCheck();
            });
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['uri'] == null && changes['query'] == null) {
            this.items = [];
            this.ref.markForCheck();
            return;
        }

        if ((changes['uri'] != null && changes['uri'].currentValue != changes['uri'].previousValue) ||
            (changes['query'] != null && changes['query'].currentValue != changes['query'].previousValue)) {
            this.queryStream$.next(changes['query'].currentValue);
        }
    }

    ngOnDestroy() {
        if (this.searchSub) {
            this.searchSub.unsubscribe();
        }
    }
}

@NgModule({
    declarations: [
        SimilarItemsComponent
    ],
    exports: [
        SimilarItemsComponent
    ]
    , imports: [
        CommonModule,
        CoreModule,
        RouterModule,
    ],
    providers: [

    ]
})
export class SimilarItemsModule { }
