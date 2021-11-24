import { Directive, Input, TemplateRef, ViewContainerRef } from '@angular/core';

interface NgLetContext<T> {
    ngLet: T;
}


/**
 * This allows to introduce new variables in angular templates
 * 
 * Example usage:
 * 
 *      Consider that we have next code:
 * 
 *          {{observable$ | async}}
 * 
 *      With this directive you can write next code:
 * 
 *          <ng-container *ngLet="observable$ | async as data">
 *              {{data}}
 *          </ng-container>
 * 
 * Use cases:
 * 
 * 1. Don't repeat yourself, reduce amount of subscriptions etc
 * 2. When you async-pipe in event handler
 */
@Directive({
    selector: '[ngLet]'
})
export class NgLetDirective<T> {
    private context: NgLetContext<T> = { ngLet: null };

    constructor(viewContainer: ViewContainerRef, templateRef: TemplateRef<NgLetContext<T>>) {
        viewContainer.createEmbeddedView(templateRef, this.context);
    }

    @Input()
    set ngLet(value: T) {
        this.context.ngLet = value;
    }
}
