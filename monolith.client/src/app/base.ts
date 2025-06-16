import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'app-root',
    templateUrl: './base.html',
    imports: [RouterOutlet]
})
export class BaseComponent {
  title = 'Govern';
}
