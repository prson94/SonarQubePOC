import { Component, OnInit, ViewChild } from '@angular/core';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { LoadDetail } from '../../models/load.model';
import { AdminBaseComponent } from './admin-base.component';
import { FormMode } from '../../models/form.model';
import { LoadService } from '../../services/load.service';
import { Title } from '@angular/platform-browser';
import { ObjectDetailComponent} from '../shared/objectdetails/object-detail.component';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-load',
    providers: [LoadService],
    templateUrl: './admin-load.component.html',
})

export class AdminLoadComponent extends AdminBaseComponent implements OnInit {
    loads: LoadDetail[] = [];
    selectedRow: LoadDetail;
    objectType = 'Load';
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;
    
    constructor(headerBreadcrumbService: HeaderBreadcrumbService, private loadService: LoadService, titleService: Title) {
        super(headerBreadcrumbService, titleService);        
        this.areaName = "Bulk Loading";
        this.setCommonItems();        
    }

    ngOnInit() {
        this.load();
    }

    load() {

        this.isLoading = true;
        this.loadService.getLoads()
            .then(data => {
                this.loads = data;
                this.selectedRow = this.loads.length > 0 ? this.loads[0] : null;
                this.isLoading = false;
            });
    }

    refreshGrid() {
        this.selectedRow = null;      
        this.load();
    }
    
}