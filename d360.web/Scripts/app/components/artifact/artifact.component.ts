///<reference path="../../es6-shim.d.ts"/>
import { Component } from '@angular/core';

import 'rxjs/Rx';

@Component({
    selector: 'd3s-artifact',
    template: `
                <div id="main">
                    <router-outlet></router-outlet>
                </div>
             ` ,   
})

export class ArtifactComponent {

}
