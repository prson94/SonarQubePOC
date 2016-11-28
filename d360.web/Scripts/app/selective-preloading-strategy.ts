import { PreloadingStrategy, Route } from '@angular/router';
import 'rxjs/add/observable/of';
import { Observable } from 'rxjs/Observable';


export class SelectivePreloadingStrategy implements PreloadingStrategy {
    preload(route: Route, load: Function): Observable<any> {
        if (route.data && route.data['preload']) {
            console.log('Preloaded: ' + route.path);
            return load();
        }
        else {
            return Observable.of(null);
        }
    }
}