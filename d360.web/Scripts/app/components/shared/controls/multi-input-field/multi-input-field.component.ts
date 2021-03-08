import { EventEmitter, Component, NgModule, ViewEncapsulation, ChangeDetectionStrategy, OnInit, Input, ChangeDetectorRef, ViewChild, ElementRef, AfterViewChecked, AfterContentInit, OnDestroy, HostListener, OnChanges, SimpleChanges, Output, DoCheck } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { KeyMapHelpers } from '../../../../static/keyboard-key-helper';
import { IgBadgeModule } from '../badge/badge.module';

@Component({
    selector: 'ig-multi-input-field',
    templateUrl: 'multi-input-field.component.html',
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ['./multi-input-field.component.less']
})
export class MultiInputField {
    constructor(public cdRef: ChangeDetectorRef) {}
}


@NgModule({
    imports: [
        CommonModule,
        TooltipModule,
        FormsModule,
        IgBadgeModule
    ],
    declarations: [MultiInputField],
    exports: [MultiInputField]
})

export class MultiInputFieldModule { }