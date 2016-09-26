import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, GroupService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { GroupEditorModel } from '../../models/group.model';

@Component({
    selector: 'd3s-group-item',
    providers: [GroupService],
    template: ` 
                <div class="row">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                        <div class="row" *ngIf="!isLoading">                        
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <d3s-group-responsibility [group]="model?.group"></d3s-group-responsibility>
                                </div>
                            </div>
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <d3s-group-members [groupId]="groupId" [groupName]="model?.group?.Name"></d3s-group-members>
                                </div>
                            </div>                            
                        </div>                            
                        
                    </div>
                </div>
                `
})

export class GroupItemComponent extends BaseComponent implements OnInit {

    private sub: any;
    private model: GroupEditorModel;
    private groupId: number;

    constructor(private route: ActivatedRoute,
        private router: Router,
        private groupService: GroupService,
        protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.groupId = +params['groupId']; // (+) converts string 'id' to a number
            this.headerBreadcrumbService.setCurrentObjectInfo('Group', this.groupId);
            this.logAction('open', 'Group', this.groupId);
            this.isLoading = true;

            this.groupService.getGroup(this.groupId)
                .then(group => {
                    this.model = group;
                    this.headerBreadcrumbService.clearBreadcrumbs();

                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Groups', 'a/group'));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.model.group.Name));

                    this.setBrowserTitle(this.titleService, this.model.group.Name);

                    this.isLoading = false;
                });
            });
        }

    ngOnDestroy() {
        this.sub.unsubscribe();
        this.clearSidebar();
    }


    private load() {

    }

};