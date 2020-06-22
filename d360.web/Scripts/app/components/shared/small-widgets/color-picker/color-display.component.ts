
import { Component, OnInit, Input, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import * as _ from 'lodash';
import { AssetService } from '../../../../services/asset.service';

@Component({
    selector: 'd3s-color-display',
    template: `
                <div *ngIf="colorsObject && colorsObject.length > 0">
                    <span *ngFor="let item of colorsObject">
                        <span class="ig-colorfield-item grid">
                            <span class="ig-colorfield-swatch" [ngClass]="{'empty': (item.color == 'transparent' || item.color == null)}" [ngStyle]="{'background-color': item.color}"></span>
                            <span class="ig-colorfield-item-label">{{item.name}}</span>
                        </span>
                        <br/>
                    </span>
                </div>
			  `
})

export class ColorDisplayComponent implements OnInit {

    @Input() colorsJSON: string;
    private colorsObject: any;

    constructor() {
    }
    ngOnInit() {
        try {
            this.colorsObject = JSON.parse(this.colorsJSON);
        } catch{
            console.log("invalid color JSON string. " + this.colorsJSON);
        }
    }
};
