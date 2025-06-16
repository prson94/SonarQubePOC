import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'anonymous-root',
    templateUrl: './anonymous.html',
    imports: [RouterOutlet]
})
export class AnonymousRoot {
  //constructor() {}
  title = 'monolith.client';
}
