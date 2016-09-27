import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-raise-issue-button',
    template: `           
        <button type="button"  class="issue-button" (click)="raiseIssue()">Raise Issue</button>
        `,    
    styles: [`
        :host{
            float:right;
        }
    `]
})

export class RaiseIssueButtonComponent extends BaseComponent {
    
    constructor(private router: Router) {
        super();
    }

    private raiseIssue() {
        this.router.navigateByUrl(`/a/workflow/raiseissue`);
    }
    
}