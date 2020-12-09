import { Component, Input } from '@angular/core';
import { PointBreakdown } from '../../../models/score.model';
import { BaseComponent } from '../base.component';

@Component({
    selector: 'score-calculation',
    templateUrl: `score-calculation.component.html`
})
export class ScoreCalculationComponent{
    @Input() selected: PointBreakdown;



}
