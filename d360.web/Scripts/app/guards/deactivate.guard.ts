
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BrowserComponent } from '../components/sidebar/visualization/browser.component';

export interface CanDeactivateComponent {
    canDeactivate: () => Observable<boolean> | boolean;
}
@Injectable()
export class DeactivateGuard  {
    canDeactivate(component) {
        try {
            if (component && (component as BrowserComponent)) {
                var state = (component as BrowserComponent).isSaved;
                if (state != null && state === false) {
                    return window.confirm($localize`Changes that you made may not be saved.`);
                }
            }

        }
        catch (ex) {
            return true;
        }
        return true;
    }
}