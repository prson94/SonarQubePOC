import { Component, Input } from '@angular/core';
import { PointBreakdown } from '../../../../models/score.model';
import { BaseComponent } from '../../base.component';

@Component({
    selector: 'score-calculation',
    templateUrl: `score-calculation.component.html`
})
export class ScoreCalculationComponent extends BaseComponent {
    @Input() selected: PointBreakdown;
    @Input() measures: PointBreakdown[];
    @Input() formattedCheck: string = '';

    private getSum(): number {
        var res = 0;
        this.measures.forEach(x => res += x.Weight);
        return res;
    }
}
