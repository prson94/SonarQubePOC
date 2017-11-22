import { Component, Input, OnChanges, SimpleChanges, NgModule } from '@angular/core';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { UriBasedService } from '../../services/uri-based.service';
import { CommonModule } from '@angular/common';
import { XHRBackend } from '@angular/http';
import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';
import { CoreModule } from './core.module';
import { RouterModule } from '@angular/router';

@Component({
    selector: 'd3s-similar-items',
    template: `     
    <div *ngIf="items.length > 0">
        <div style="color: #FFB230">The following items with similar names already exist:</div>
        <span *ngFor="let s of items; let i = index;">
            <d3s-preview-tooltip objectType="Artifact" [objectId]="s.objectid"><a [routerLink]="s.Url">{{s.Name}}</a></d3s-preview-tooltip>            
            <span *ngIf="i < (items.length - 1)">,</span>&nbsp; 
        </span>
    </div>                  
                `,
    providers: [UriBasedService],
})
export class SimilarItemsComponent implements OnChanges {
    @Input() uri: string = '';
    @Input() query: string = '';

    private items = [];

    constructor(private uriBasedService: UriBasedService) { }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['uri'] == null && changes['query'] == null) {
            this.items = [];
            return;
        }

        if ((changes['uri'] != null && changes['uri'].currentValue != changes['uri'].previousValue) ||
            (changes['query'] != null && changes['query'].currentValue != changes['query'].previousValue)) {
            this.getSimilarItems();
        }
    }

    getSimilarItems() {
        this.items = [];

        if (this.uri == null || this.uri == '' || this.query == null || this.query.length < 2)
            return;

        this.uriBasedService.getItems(this.uri + this.query)
            .then(r => {
                r.forEach(i => {
                    i.Url = '/' + SiteUrlHelpers.getObjectUrl('Artifact', i.objectid, i.objecttypeid);
                });
                this.items = r;
            });
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
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class SimilarItemsModule { }
