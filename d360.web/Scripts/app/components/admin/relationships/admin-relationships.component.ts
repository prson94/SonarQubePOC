import { Component, OnDestroy} from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { RelationshipType } from '../../../models/relationship.model';
import { Title } from '@angular/platform-browser';
import { SecondaryNavItem } from '../../../models/secondaryNav.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-relationships-component',
    template: `<div class="row">
                    <div class="col l12 m12 s12">                    
                        <div class="tile tile-detail">
                            <d3s-admin-relationships-list (selectedChange)="selectedItemChange($event)" [(selected)]="selected"></d3s-admin-relationships-list>
                        </div>
                    </div>                    
                </div>  
                `
})

export class AdminRelationshipsComponent extends AdminBaseComponent implements OnDestroy {   
    selected: RelationshipType;
    
    constructor(
        secondaryNavService: SecondaryNavService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);        
    }

    selectedItemChange(event) {
        this.selected = event;

        this.buildSecondaryNavigationForObject(this.selected.Id, 'IntersectType');

        if (this.auditSidebar) {
            this.auditSidebar.url = `/sidebar/audit/${this.selected.Uid}`;
        }
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

}