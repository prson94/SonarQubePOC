
import { Component, OnInit, Input } from '@angular/core';
import { Router } from '@angular/router';
import * as _ from 'lodash';
import { DetailField } from '../../../../models/object-detail.model';
import { LinkClickInterceptor } from '../../../../services/href-click-service';

@Component({
    selector: 'd3s-color-display',
    template: `
                <div *ngIf="colorsObject && colorsObject.length > 0">
                    <span *ngFor="let item of colorsObject;">
                        <span [class]="'ig-colorfield-item ' + styleClass" style="display: inline-flex !important;">
                            <span class="ig-colorfield-swatch" [ngClass]="{'empty': (item.color == 'transparent' || item.color == null)}" [ngStyle]="{'background-color': item.color}"></span>
                            <span *ngIf="!url" class="ig-colorfield-item-label" [style.white-space]="whiteSpace">
                                {{item.name}}
                            </span>
                            <a context-link *ngIf="url" class="ig-colorfield-item-label" [style.white-space]="whiteSpace" (click)="navigate(url, $event)">
                                {{item.name}}
                            </a>
                        </span>
                        <br/>
                    </span>
                </div>
			  `
})

export class ColorDisplayComponent implements OnInit {

    @Input() colorsJSON: string;
    @Input() url: string;
    @Input() styleClass: string = 'grid';
    @Input() interceptLinkClick: boolean = false;
    @Input() field: DetailField;
    @Input() valueIndex: number = 0;

    public colorsObject: any;
    public whiteSpace: string = '';

    constructor(private router: Router,
        private linkClickInterceptor: LinkClickInterceptor) {
    }
    ngOnInit() {
        if (this.colorsJSON) {
            if (typeof this.colorsJSON === 'string') {
                if (this.isStringJson(this.colorsJSON)) {
                    this.colorsObject = JSON.parse(this.colorsJSON);
                }
                else {
                    this.colorsObject = [{ name: this.colorsJSON, color: 'transparent' }];
                }
            }
            else {
                this.colorsObject = this.colorsJSON;
            }
        }
        if (this.field?.FieldName && this.field.FieldName === 'GovernanceRole') {
            this.whiteSpace = 'normal';
        }
    }
    private isStringJson(str) {
        try {
            JSON.parse(str);
        } catch (e) {
            return false;
        }
        return true;
    }

    getColorFromName(status: string) {
        status = status.toLowerCase().trim();
        switch (status) {
            case 'draft':
                return '#d1dce4';
            case 'certified':
                return '#4ecc89';
            case 'under review':
                return '#e2792a';
            default:
                //custom status, we need to generate a color
                let hash = 0;
                for (let i = 0; i < status.length; i++) {
                    hash = status.charCodeAt(i) + ((hash << 5) - hash);
                    hash = hash & hash;
                }
                return `hsl(${(hash * 2) % 360}, 70%, 70%)`;
        }
    }

    navigate(url: string, e: any, data = null) {
        if (this.interceptLinkClick) {
            this.linkClickInterceptor.sendEvent(e, this.field, url, this.valueIndex);
            return;
        }
        this.router.navigateByUrl(url);
        e.preventDefault();
    }
}
