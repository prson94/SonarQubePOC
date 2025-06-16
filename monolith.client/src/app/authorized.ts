import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'authorized-root',
    templateUrl: './authorized.html',
    imports: [RouterOutlet]
})
export class AuthorizedRoot {
  //constructor() {}
  title = 'monolith.client';
}
