
import { Component, OnInit, EventEmitter, Output, Input, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import * as _ from 'lodash';
import { SelectItem } from 'primeng/api';
import { AssetService } from '../../../../services/asset.service';

@Component({
    selector: 'ig-color-picker',
    template: `
                <div class="d3s-color-picker">
                    <p-dropdown [appendTo]="'body'" [options]="colors" [ngModel]="selectedColor" (onChange)="itemChanged($event)" placeholder="{{placeholder}}" scrollHeight="320px" showClear="true" filter="true" filterPlaceholder="Search colors" [disabled]="disabled">
                        <ng-template let-item pTemplate="selectedItem">
                            <div class="ig-colorfield-item-selected">
                                <span class="ig-colorfield-swatch" [style.background-color]="item?.title"></span>
                                <span class="ig-colorfield-item-label">{{item?.label}}</span>
                            </div>
                        </ng-template>
                        <ng-template let-color pTemplate="item">
                            <div class="ig-colorfield-item">
                                <span class="ig-colorfield-swatch" [style.background-color]="color.title"></span>
                                <span class="ig-colorfield-item-label">{{color.label}}</span>
                            </div>
                        </ng-template>
                    </p-dropdown>
                </div>
			  `,
    providers: [AssetService]
})

export class ColorPickerComponent implements OnInit {

    @Input() colors: SelectItem[] = [];
    @Input() placeholder: string = 'Optional';
    @Input() selectedColor: string;
    @Input() loadDefaultColors: boolean = false;
    @Input() disabled: boolean = false;
    @Output() selectedColorChange = new EventEmitter();

    constructor(private router: Router, private ref: ChangeDetectorRef, private assetService: AssetService) {
    }
    ngOnInit() {
        this.load();
    }

    private itemChanged(item: any) {
        this.selectedColorChange.emit(item.value);
    }

    load(): any {
        if (this.loadDefaultColors) {
            this.assetService.getAllColors().subscribe(res => {
                if (res)
                    this.colors = res;
                if (this.selectedColor && res.length > 0) {
                    let isCustom = this.colors.filter(x => { return x.label == this.selectedColor }).length == -1;
                    if (isCustom)
                        this.selectedColor = null;
                } else {
                    this.selectedColor = null;
                }
                this.ref.markForCheck();
            });
        }
    }
};
