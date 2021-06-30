import { Input, Component } from '@angular/core';
import { BaseComponent } from '../base.component';

@Component({
    selector: 'side-panel',
    templateUrl: './side-panel.component.html',
})

export class SidePanelComponent extends BaseComponent{
    @Input() dataProfile: any;

    private showDataProfile: boolean;
    private showSidePanel: boolean;    
    private activePanelName: string;

    ngOnInit() {
        this.showDataProfilePanel();
    }

    private showDataProfilePanel() {
        this.activePanelName = "Profiling";
        this.showDataProfile = true;    
        this.showSidePanel = true;
    }

    private hideSidePanel() {
        this.showDataProfile = false;
        this.showSidePanel = false;
    }
}