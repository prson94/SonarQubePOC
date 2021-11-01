import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { BaseComponent } from './base.component';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-grid-selection-info',
    template: ` 
<div style="position: relative">
    <div style="position: absolute; top: -54px; left: 0; font-weight: normal; padding-top: 5px; float:left; display:inline; width: 300px; z-index: 10; text-align: left;">
            <ng-container *ngIf="includeSelectLinks">
                <a style="color: #51a6dc; cursor: pointer;" (click)="onSelectAllClick.emit()">Select All</a> | <a style="color: #51a6dc; cursor: pointer;" (click)="onSelectNoneClick.emit()">Select None</a> &nbsp;&nbsp;&nbsp;
            </ng-container>
            {{selectedItems}} of {{totalItems}} items selected
    </div>
</div>
        `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GridSelectionInfoComponent extends BaseComponent {
    @Input() includeSelectLinks = true;
    @Input() model: any[] = [];
    @Input() selection: any[] = [];
    @Output() onSelectAllClick = new EventEmitter();
    @Output() onSelectNoneClick = new EventEmitter();

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    get totalItems(): number {
        return this.model == null ? 0 : this.model.length;
    }

    get selectedItems(): number {
        return this.selection == null ? 0 : this.selection.length;
    }
   
}

@NgModule({
    imports: [CommonModule,
    ],
    declarations: [
        GridSelectionInfoComponent
    ],
    exports: [
        GridSelectionInfoComponent
    ]
})
export class SharedGridSelectionInfoModule { }