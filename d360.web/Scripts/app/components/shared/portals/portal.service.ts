
import { Injectable, TemplateRef } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class PortalService {
    private registeredPortals = new Set<string>();
    private registeredContent$ = new BehaviorSubject<Map<string, TemplateRef<any>>>(new Map());

    registerPortal(name: string | undefined) {
        if (!name) {
            return;
        }

        if (!environment.production && this.registeredPortals.has(name)) {
            throw new Error(`Attempt to register already registered portal ${name}. `
                + `Most probably, there are several instances of components with portals`);
        }

        this.registeredPortals.add(name);
    }

    unregisterPortal(name: string | undefined) {
        if (!name) {
            return;
        }

        if (!environment.production && !this.registeredPortals.has(name)) {
            throw new Error(`Attempt to unregister non-registered portal ${name}. `
                + `Most probably, there are several instances of components with portals`);
        }

        if (!environment.production && this.registeredContent$.value.has(name)) {
            console.warn(`Unregistered portal ${name}. But there is content left for this portal.`
                + `If content will be desroyed after, please ignore this warning.`);
        }

        this.registeredPortals.delete(name);
    }

    registerContent(name: string | undefined, template: TemplateRef<any> | undefined) {
        if (!name && !template) {
            return;
        }

        if (!environment.production && !name && template) {
            throw new Error(`Attempt to register content without name`);
        }

        if (!environment.production && name && !template) {
            throw new Error(`Attempt to register content ${name} without providing template`);
        }

        let registeredContent = this.registeredContent$.value;
        if (!environment.production && registeredContent.has(name)) {
            throw new Error(`Attempt to register already registered content ${name}. `
                + `Most probably, there are several instances of components with content providers`);
        }

        if (!environment.production && !this.registeredPortals.has(name)) {
            console.warn(`Registered content for portal ${name}. Portal is not found. `
                + `If portal will be rendered after, please ignore this warning.`);
        }

        registeredContent = new Map(registeredContent);
        registeredContent.set(name, template);
        this.registeredContent$.next(registeredContent);
    }

    unregisterContent(name: string | undefined, previousTemplate: TemplateRef<any> | undefined) {
        if (!name) {
            return;
        }

        let registeredContent = this.registeredContent$.value;
        if (!environment.production && !registeredContent.has(name)) {
            throw new Error(`Attempt to unregister non-registered content ${name}. `
                + `Most probably, there are several instances of components with content providers`);
        }

        if (!environment.production && registeredContent.get(name) != previousTemplate) {
            throw new Error(
                `Attempt to unregister content ${name} failed, ` +
                + `because registered template doesn't matches previousTemplate. `
                + `Most probably, there are several instances of components with content providers`);
        }

        registeredContent = new Map(registeredContent);
        registeredContent.delete(name);
        this.registeredContent$.next(registeredContent);
    }

    getPortalContent$(name: string | undefined) {
        return this.registeredContent$.pipe(map(x => x.get(name)));
    }
}
