
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import * as _ from 'lodash';
import { SelectItem } from 'primeng/api';
import { AssetService } from '../../../../services/asset.service';

@Component({
    selector: 'd3s-color-picker',
    template: `
                <div class="d3s-color-picker">
                    <p-dropdown [options]="colors" placeholder="Optional" scrollHeight="320px" showClear="true" filter="true" filterPlaceholder="Search colors">
                        <ng-template let-item pTemplate="selectedItem">
                            <div class="ig-colorfield-item-selected"><span class="ig-colorfield-swatch" [style.background-color]=item.value></span>
                                <span class="ig-colorfield-item-label">{{item.label}}</span></div>
                        </ng-template>
                        <ng-template let-color pTemplate="item">
                            <div class="ig-colorfield-item"><span class="ig-colorfield-swatch" [style.background-color]=color.value></span>
                                <span class="ig-colorfield-item-label">{{color.label}}</span></div>
                        </ng-template>
                    </p-dropdown>
                </div>
			  `,
    providers: [AssetService]
})

export class ColorPickerComponent implements OnInit {

    private colors: SelectItem[] = [];
    private selectedColor: SelectItem;
    constructor(private router: Router, private assetService: AssetService) {
    }

    ngOnInit(): void {
        //get all colors
        this.colors = [];
        this.load();

    }
    load(): any {
        this.assetService.getAllColors().subscribe(res => {
            if(res)
                this.colors = res;
        });
    }

};
