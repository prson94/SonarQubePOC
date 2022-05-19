import { Injectable } from "@angular/core";
import { PropertyGroupComponent } from "./property-group.component";

@Injectable({ providedIn: 'root' })
export class PropertyGroupsService {
    private instanceIdToComponent = new Map<string, PropertyGroupComponent>();

    getById(id: string): PropertyGroupComponent {
        return this.instanceIdToComponent.get(id);
    }

    register(component: PropertyGroupComponent) {
        if (this.instanceIdToComponent.has(component.instanceId)) {
            throw new Error(`PropertyGroupComponent with id=${component.instanceId} is already registered`);
        }

        this.instanceIdToComponent.set(component.instanceId, component);
    }

    unregister(component: PropertyGroupComponent) {
        if (!this.instanceIdToComponent.has(component.instanceId)) {
            throw new Error(
                `Failed to unregister PropertyGroupComponent with id=${component.instanceId} 
                because it wasn't registered`);
        }

        this.instanceIdToComponent.delete(component.instanceId);
    }
}