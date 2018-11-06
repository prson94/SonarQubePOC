import { Component, NgZone, OnDestroy } from '@angular/core';
import { AdminBaseComponent } from '../admin-base.component'

@Component({
    selector: 'd3s-admin-export-templates-component',
    template: ` <div class="row">
                    <div class="col l3 m5 s12">
                        <div class="tile tile-detail">
                            list of templates
                        </div>
                    </div>
                    <div class="col l9 m7 s12">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">  
                                    selected template details
                                </div>
                            </div>
                        </div>
                    </div>
                <div>
                `,
    providers: [],       
})

export class AdminExportTemplatesComponent extends AdminBaseComponent implements OnDestroy {
    ngOnDestroy() {
        this.clearSidebar();
    }
}