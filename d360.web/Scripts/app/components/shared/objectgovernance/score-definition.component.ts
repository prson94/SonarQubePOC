import { Component, Input } from '@angular/core';
import { PointBreakdown } from '../../../models/score.model';

@Component({
    selector: 'score-definition',
    templateUrl: `score-definition.component.html`
})
export class ScoreDefinitionComponent {
    @Input() selected: PointBreakdown;

}
