
import { Input, Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService} from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';

@Component({
    selector: 'd3s-resource-list',    
    template: ` 
                <div class="row">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                        <div class="tile tile-detail" >    
                            <d3s-dynamic-grid [title]="'Users'" objectType="ResourceType" [objectID]="objectID" 
                              [rowID]="'ResourceID'"
                              [itemName]="'Resource'"
                              [createUri]="'form/dynamicedit/create/resource/'" 
                              [editUri]="'form/dynamicedit/edit/resource/'" 
                              [dataUri]="resourceUri()" 
                              [deleteUri]="'form/DeleteResourceByID?id='"
                                (editItemClick)="openResource($event)"></d3s-dynamic-grid>                                                                       
                        </div>                        
                    </div>
                </div>
                `
})

export class ResourceListComponent extends BaseComponent{    
    private objectID: number = 1;

    constructor(private route: ActivatedRoute,
        private router: Router,
        protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();
    }    

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Resources');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Resources'));
    }

    private resourceUri(): string {
        return `/api/resources/${this.objectID}?$orderby=LastName,FirstName`;
    }

    private openResource(event) {        
        this.router.navigateByUrl(`/a/resource/${event.ResourceID}`);
    }
};