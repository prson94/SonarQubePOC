import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { SurveysService } from '../../services/surveys.service';
import { ModelsService } from '../../services/models.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PermissionsService } from '../../services/permissions.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Model, ModelHierarchy } from '../../models/model.model';
import { TreeNode } from 'primeng/api';
import { MessageBarItem } from '../../models/message-bar-item.model';
import { SurveyType } from '../../models/survey.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';
import { Permission } from '../../models/responsibility-type.model';
import { SecondaryNavItem, SecondaryNavCurrentObject } from '../../models/secondaryNav.model';

declare var CompanySettings;

@Component({
    selector: 'd3s-model-item',
    providers: [ModelsService, SurveysService, PermissionsService],
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading"
             class="row">
            <div class="col s12">
                <div class="tile tile-detail">
                    <d3s-object-definition-tile [nymTypes]="model?.NymTypes"
                                                [objectPermissions]="permissions"
                                                [objectType]="'Taxonomy'"
                                                [useV2Api]="true"
                                                [objectID]="selected?.ID"
                                                [hasAttributes]="model?.AllowAttributes"
                                                (onEditComplete)="editComplete($event)"></d3s-object-definition-tile>
                </div>
            </div>
        </div>
    `
})

export class ModelItemComponent extends BaseComponent implements OnInit, OnDestroy {
    sub: any;
    private currentAreaNameSubscription: any;
    private currentAreaName: string;
    treeSub: any;
    model: Model;
    selected: ModelHierarchy;
    modelId: number;
    treeNodeArray: TreeNode[] = [];
    crumbs: Breadcrumb[] = [];
    private messages: MessageBarItem[] = [];
    private surveyType: SurveyType;
    private showSurvey: boolean = false;
    private showSocialScoreBar: boolean = true;

    constructor(private route: ActivatedRoute,
        private router: Router,
        secondaryNavService: SecondaryNavService,
        protected modelsService: ModelsService,
        protected titleService: Title,
        protected surveysService: SurveysService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected permissionsService: PermissionsService
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = headerBreadcrumbService;
    }

    ngOnInit() {
        this.treeSub = this.headerBreadcrumbService.breadcrumbTreeSource$.subscribe(
            id => {
                this.showHierarchy(id);
            });

        this.sub = this.route.params.subscribe(params => {
            let newModelId = +params['modelId'];
            let hierarchyId = +params['id'];// if hierarchyId is passed via alternative route to workaround bug with router escaping ; = and other chars.

            this.currentAreaNameSubscription =
                this.headerBreadcrumbService
                    .getAreaName('TaxonomyType', newModelId)
                    .subscribe(result => { this.currentAreaName = result; if (this.model) this.buildBreadcrumb(); });

            if (!hierarchyId)
                hierarchyId = params['hierarchyId'] ? +params['hierarchyId'] : 0;

            if (this.modelId != newModelId) {
                this.modelId = newModelId;
                this.isLoading = true;
                this.load(hierarchyId);

                this.isLoading = false;
            } 

        });

        this.showSocialScoreBar = (CompanySettings.ShowSocialScoreBar != 'false');
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();
        this.treeSub.unsubscribe();
        this.currentAreaNameSubscription.unsubscribe();
    }

    private buildBreadcrumb() {
        if (this.selected) {
            this.buildSecondaryNavigation(this.selected.Uid);
        }
    }

    private load(hierarchyId: number): void {
        this.modelsService.getModel(this.modelId).subscribe(
            result => {
                this.model = result;
                this.loadModelHierarchy(this.modelId, hierarchyId);
                this.buildBreadcrumb();
            }
        );
    }

    private selectModelHierarchy(selectedHierarchyId: number): Promise<void> {
        if (selectedHierarchyId > 0) {
            let selArray = this.preloadedTreeData.filter(x => x.ID == selectedHierarchyId);
            if (selArray.length > 0) this.selected = selArray[0];
            else {
                //console.log("ERROR INVALID SELECTED HIERARCHY ID SPECIFIED.", selectedHierarchyId);
                this.selected = (this.preloadedTreeData.length && this.preloadedTreeData.length > 0) ? this.preloadedTreeData[0] : null;
            }
        } else {
            this.selected = (this.preloadedTreeData.length && this.preloadedTreeData.length > 0) ? this.preloadedTreeData[0] : null;
        }

        this.assetID = this.selected.AssetID;

        this.loadPermissions(this.permissionsService, StringConstants.ObjectTaxonomy, this.selected.ID);
        this.buildBreadcrumb();
        return Promise.resolve(null);
    }

    private loadModelHierarchy(modelId: number, selectedHierarchyId: number): void {
        this.modelsService.getModelHierarchy(modelId).subscribe(result => {
            this.preloadedTreeData = result;

            this.treeNodeArray = this.buildTreeNodeArray(this.preloadedTreeData);

            this.selectModelHierarchy(selectedHierarchyId);
            this.messages = []; //clear any messages for this model
            this.loadItemSurvey(this.modelId);

            this.setBrowserTitle(this.titleService, this.model.Name);
        }
        );
    }

    private findSelectedTreeNode(id: number): TreeNode {
        let nodes: TreeNode[] = [];

        // add root nodes
        for (let rNode of this.treeNodeArray) {
            nodes.push(rNode);
        }

        //do a breadth first search for the given treenode
        if (nodes.length == 0) return;

        let node = nodes[0];

        while (node) {
            if (node.data.id && node.data.id == id) return node;

            //push children
            if (node.children) {
                for (let cNode of node.children) {
                    nodes.push(cNode);
                }
            }

            //remove this node
            nodes.splice(0, 1);

            if (nodes.length == 0) return null;
            node = nodes[0];
        }
    }

    private buildTreeNodeArray(models: ModelHierarchy[], Parent?: number, includeChildren?: boolean): TreeNode[] {
        //find the root items then 
        includeChildren = includeChildren == undefined ? true : false;
        let rootNodes = models.filter(x => (Parent != undefined ? x.ParentID == Parent : !x.ParentID));

        if (rootNodes.length == 0) return null;

        let res: TreeNode[] = [];

        for (let root of rootNodes) {
            res.push({
                label: root.DisplayValue,
                expanded: true,
                data: {
                    id: root.ID, hasRelations: root.HasChildren, AssetID: root.AssetID
                },
                children: (includeChildren ? this.buildTreeNodeArray(models, root.ID) : null) //recursively find its children
            });
        }

        return res;
    }

    private showHierarchy(id: number) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('TAXONOMY', id, this.modelId));
        this.buildBreadcrumb();
    }

    private loadItemSurvey(modelId: number) {
        if (!this.selected) {
            console.log("ERROR NO MODEL HEIRARCHY ITEM SELECTED TO LOAD SURVEY INFO FOR.");

            return;
        }
        this.surveysService.getObjectSurvey(modelId, 'TaxonomyType', this.selected.ID, 'Taxonomy')
            .subscribe(result => {
                this.surveyType = undefined;
                if (result) {
                    this.surveyType = result;
                    this.messages.push({
                        content: `<u>Click here</u> to take the survey: <em>${result.Name}</em>.`,
                        showClose: true,
                        data: 'Survey'
                    });
                }

            });
    }

    private completeSurvey() {
        this.showSurvey = false;
        var index = this.messages.findIndex(x => x.data == 'Survey');

        if (index >= 0 && index < this.messages.length) {
            this.messages.splice(index, 1);
        }
    }

    private editComplete(e: any) {
        this.load(e.ID);
    }
}
