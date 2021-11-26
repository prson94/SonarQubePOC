import { ChangeDetectionStrategy, Component, Input, SimpleChanges } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { PortalService } from './portal.service';

/**
 * Portals are a technology used to render content physically into other places.
 * They are extremely useful in cases when logical decomposition & physical decomposition doesn't match.
 * 
 * How to use portals:
 * 
 * 1. In some component, for example App-component, write portal placeholder.
 *    See example:
 * 
 *        <ig-portal-renderer name="valve"></ig-portal-renderer>
 * 
 * 2. In some other component, for example DeepChild-component, write portal content provider.
 *    1. You need to wrap your content into ng-template
 *    2. And pass template to ig-portal-content-provider
 *    3. See example:
 * 
 *           <ig-portal-content-provider name="valve" [template]="apertureTemplate">
 *               <ng-template #apertureTemplate>
 *                   <b>Aperture Science</b>
 *               </ng-template>
 *           </ig-portal-content-provider>
 * 
 * For clarity, this will happen during rendering:
 * 
 * 1. ig-portal-content-provider will not contain any content. Resulting HTML:
 * 
 *        <ig-portal-content-provider></ig-portal-content-provider>
 * 
 * 2. ig-portal-renderer will be expanded to have content. Resulting HTML:
 * 
 *        <ig-portal-renderer name="valve">
 *            <b>Aperture Science</b>
 *        </ig-portal-renderer>
 */
@Component({
    selector: 'ig-portal-renderer',
    template: `
        <ng-container *ngIf="template$ | async as template" [ngTemplateOutlet]="template">
        </ng-container>
    `,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class PortalRendererComponent {
    @Input() name: string;

    constructor(private portalService: PortalService) {
    }

    private nameInput$ = new BehaviorSubject(undefined);
    template$ = this.nameInput$.pipe(switchMap(name => this.portalService.getPortalContent$(name)));

    ngOnChanges(changes: SimpleChanges) {
        if ('name' in changes) {
            this.portalService.unregisterPortal(changes['name'].previousValue);
            this.portalService.registerPortal(changes['name'].currentValue);
        }

        this.nameInput$.next(this.name);
    }

    ngOnDestroy() {
        this.portalService.unregisterPortal(this.name);
    }
}