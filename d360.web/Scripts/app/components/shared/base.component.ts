import { Title } from '@angular/platform-browser';

export class BaseComponent {    
    protected isLoading = false;

    constructor() {  }

    protected setBrowserTitle(tileService: Title, area: string) {
        tileService.setTitle(`D3S - ${area}`);
    }
}