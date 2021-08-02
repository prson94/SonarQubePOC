import { Component, EventEmitter, Input, Output } from '@angular/core';
import { BaseComponent } from '../base.component';

@Component({
    selector: 'match-detection',
    templateUrl: './match-detection.component.html',
    styleUrls: ['match-detection.less']
})

export class MatchDetectionComponent extends BaseComponent {
    @Input() isVisible: boolean = false;
    @Output() onClose = new EventEmitter();

}