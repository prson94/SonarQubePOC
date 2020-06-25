import { Component, Input, Output, EventEmitter, NgModule, } from '@angular/core';
import { BaseComponent } from './base.component';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IconService } from '../../services/icon.service';
import { DropdownModule } from 'primeng/dropdown';

@Component({
    selector: 'd3s-icon-picker',
    template: `
<ng-container *ngIf="!isLoading">
    <p-dropdown styleClass="icon-picker"
                panelStyleClass="icon-panel"
                [options]="categories" 
                [group]="true" 
                [ngModel]="ngModel"
                (ngModelChange)="ngModelChange.emit($event)"
                [filter]="true"
                [showClear]="true"
                appendTo="body"
                scrollHeight="320px"
                placeholder="Optional"
                filterPlaceholder="Type to filter">
        <ng-template let-group pTemplate="group">
            <span>{{group.label}}</span>
        </ng-template>
        <ng-template let-item pTemplate="selectedItem">
            <div class="iconfield-item-selected">
                <span class="iconfield-swatch">
                    <i [class]="'fa ' + item.value"></i>
                </span>
                <span class="iconfield-item-label">
                    {{item.label}}
                </span>
            </div>
        </ng-template>
        <ng-template let-item pTemplate="item">
            <div class="iconfield-item">
                <span class="iconfield-swatch">
                    <i [class]="'fa ' + item.value"></i>
                </span>
                <span class="iconfield-item-label">
                    {{item.label}}
                </span>
            </div>
        </ng-template>
    </p-dropdown> 
</ng-container>
       
    `,
    styles: [
        `
   .iconfield-swatch {
        display: inline-block;
        position: relative;
        left: -3px;
        width: 24px;
        height: 24px;
        line-height: 24px;
        text-align: center;
        background: #f1f2f3;
        color: #202020;
        border-radius: 2px;
        margin-right: 5px;
        font-size: 1em;
    }

    .iconfield-item-label {
        position: relative;
        top: 1px;
        -webkit-box-flex: 1;
        -ms-flex-positive: 1;
        flex-grow: 1;
        overflow: hidden;
        text-overflow: ellipsis;
        color: #202020;
    }

    .iconfield-item, .iconfield-item-selected {
        display: -webkit-inline-box;
        display: -ms-inline-flexbox;
        display: inline-flex;
        height: 32px;
        margin: 0;
        padding: 0;
        -webkit-box-align: center;
        -ms-flex-align: center;
        align-items: center;
        max-width: 100%;
    }

    .iconfield-item-selected {
         position: relative; 
         top: -5px; 
        left: -2px;
    }
`
    ]
})

export class IconPickerComponent extends BaseComponent {
    @Input() ngModel: string;
    @Output() ngModelChange = new EventEmitter();

    private categories: any = [];

    constructor(private iconService: IconService) {
        super();
        this.isLoading = true;
    }

    ngOnInit() {

        this.iconService.getIconProperties().subscribe(result => {
            result.forEach(i => {
                let index = this.categories.findIndex(x => x.label == i.categories[0]);

                if (index == -1) {
                    this.categories.push({
                        label: i.categories[0],
                        value: i.categories[0],
                        items: [{ label: i.name, value: 'fa-' + i.id }]
                    });
                } else {
                    this.categories[index].items.push({ label: i.name, value: 'fa-' + i.id });
                }
            });

            this.categories.forEach(c => c.items.sort((a, b) => this.sortByName(a, b)));
            this.categories.sort((a, b) => this.sortByName(a, b));

            this.isLoading = false;
        });
    }

    private sortByName(a, b) {
        return (a.label < b.label) ? -1 : (a.label > b.label) ? 1 : 0;
    }
}

@NgModule({
    declarations: [
        IconPickerComponent
    ],
    exports: [
        IconPickerComponent
    ]
    , imports: [
        CommonModule,
        FormsModule,
        DropdownModule,
    ],
    providers: [IconService]
})
export class IconPickerModule { }