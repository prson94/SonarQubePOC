
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import * as _ from 'lodash';
import { SelectItem } from 'primeng/api';
import { AssetService } from '../../../../services/asset.service';

@Component({
    selector: 'd3s-color-picker',
    template: `
                <div class="d3s-color-picker" style="width:240px">
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
            else
                this.colors = [
                    { label: 'Cobalt', value: '#2E6EC2' },
                    { label: 'Azure', value: '#0BB8CE' },
                    { label: 'Denim', value: '#248BE5' },
                    { label: 'Sky', value: '#72BEF8' },

                    { label: 'Burgundy', value: '#990132' },
                    { label: 'Scarlet', value: '#EF2832' },
                    { label: 'Hot Pink', value: '#E6005C' },
                    { label: 'Blush', value: '#E55C57' },
                    { label: 'Lilac', value: '#E7BDEF' },

                    { label: 'Emerald', value: '#01A96C' },
                    { label: 'Grass', value: '#43A047' },
                    { label: 'Lime', value: '#97D70B' },
                    { label: 'Spring', value: '#9AE39D' },

                    { label: 'Rust', value: '#C54309' },
                    { label: 'Orange', value: '#FE6600' },
                    { label: 'Amber', value: '#FFA900' },
                    { label: 'Peach', value: '#FFCC80' },

                    { label: 'Mustard', value: '#D0BC0A' },
                    { label: 'Sunshine', value: '#FFE50B' },
                    { label: 'Lemon', value: '#FDFD45' },
                    { label: 'Sand', value: '#EEE9B0' },

                    { label: 'Indigo', value: '#642BBC' },
                    { label: 'Violet', value: '#B825D0' },
                    { label: 'Mauve', value: '#BF75CC' },
                    { label: 'Cornflower', value: '#C5CCF4' },

                    { label: 'Teal', value: '#02817F' },
                    { label: 'Aqua', value: '#80CBC4' },
                    { label: 'Slate', value: '#657986' },
                    { label: 'Stone', value: '#90A4AE' },

                    { label: 'Chocolate', value: '#4A322B' },
                    { label: 'Wood', value: '#6E4C41' },
                    { label: 'Tan', value: '#BD9255' },
                    { label: 'Coffee', value: '#A18880' }
                ];
        });
    }

};
