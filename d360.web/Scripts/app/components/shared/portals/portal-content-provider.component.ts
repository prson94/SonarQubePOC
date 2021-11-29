import { ChangeDetectionStrategy, Component, Input, SimpleChanges, TemplateRef } from '@angular/core';
import { PortalService } from './portal.service';

/***
 * Portals are a technology used to render content physically into other places.
 * @see PortalRendererComponent for more detailed description
 */
 @Component({
    selector: 'ig-portal-content-provider',
    template: ``,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class PortalContentProviderComponent {
    @Input() name: string;
    @Input() template: TemplateRef<any>;

    constructor(private portalService: PortalService) {
    }

    ngOnChanges(changes: SimpleChanges) {
        if (('name' in changes) || ('template' in changes)) {
            this.portalService.unregisterContent(changes['name'].previousValue, changes['template'].previousValue);
            this.portalService.registerContent(changes['name'].currentValue, changes['template'].currentValue);
        }
    }

    ngOnDestroy() {
        this.portalService.unregisterContent(this.name, this.template);
    }
}