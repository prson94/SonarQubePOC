import {Input, Component, EventEmitter, Output, OnInit, OnDestroy} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';
import {BaseComponent} from '../shared/base.component';
import {Title} from '@angular/platform-browser';
import {SurveysService} from '../../services/surveys.service';
import {ModelsService} from '../../services/models.service';
import {HeaderBreadcrumbService} from '../../services/header-breadcrumb.service';
import {PermissionsService} from '../../services/permissions.service';
import {RightSidebarService} from '../../services/right-sidebar.service';
import {Breadcrumb} from '../../models/breadcrumb.model';
import {Model, ModelHierarchy} from '../../models/model.model';
import {TreeNode} from 'primeng/primeng';
import {MessageBarItem} from '../../models/message-bar-item.model';
import {SurveyType} from '../../models/survey.model';
import {SiteUrlHelpers} from '../../static/site-url-helpers';
import {StringConstants} from '../../static/string-constants';
import {Permission} from '../../models/responsibility-type.model';
import { RightSidebarItem } from '../../models/rightsidebar.model';

declare var CompanySettings;

@Component({
    selector: 'd3s-model-item',
    providers: [ModelsService, SurveysService, PermissionsService],
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading"
             class="row">
            <div class="col s12">
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-object-definition-tile [nymTypes]="model?.NymTypes"
                                                        [objectPermissions]="permissions"
                                                        [objectType]="'Taxonomy'"
                                                        [objectID]="selected?.ID"
                                                        [hasAttributes]="model?.AllowAttributes"
                                                        (onEditComplete)="editComplete($event)"></d3s-object-definition-tile>
                        </div>
                    </div>
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
    modelHierarchy: ModelHierarchy[] = [];
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
                rightSidebarService: RightSidebarService,
                protected modelsService: ModelsService,
                protected titleService: Title,
                protected surveysService: SurveysService,
                protected headerBreadcrumbService: HeaderBreadcrumbService,
                protected permissionsService: PermissionsService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;
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

            if (hierarchyId != 0)
                this.headerBreadcrumbService.setCurrentObjectInfo('Taxonomy', hierarchyId);
            else
                this.headerBreadcrumbService.setCurrentObjectInfo('TaxonomyType', newModelId);
            this.setObjectInfo('Taxonomy', hierarchyId);
            if (this.modelId != newModelId) {
                this.modelId = newModelId;
                this.isLoading = true;
                this.load(hierarchyId);

                this.isLoading = false;
            } else {
                // pop last breadcrumb
                this.headerBreadcrumbService.popLastBreadcrumb();
                this.selectModelHierarchy(hierarchyId).then(n => {
                    this.clearSidebar();
                });
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
            this.headerBreadcrumbService.getFolderTitle("#Models").then((res) => {
                this.crumbs = []; 
                this.headerBreadcrumbService.getFolderIcon(this.currentAreaName ? this.currentAreaName : res).then(icon => {
                    this.lineageShowUsageOnly = true;   
                    this.setCommonRightSideBar(true, this.hasPermission(Permission.ReadResponsibilities), (this.selected != null ? this.selected.HasDashboards : false), true, true, this.hasPermission(Permission.ReadRelationships), true, true);
                    this.rightSidebarService.setCurrentArea(this.selected.DisplayValue, icon, 'Definition');
                    this.rightSidebarService.setCurrentObject('TaxonomyType', this.model.ID, 'Taxonomy', this.selected.ID, false, null, this.selected.Uid);
                    this.rightSidebarService.showItem(new RightSidebarItem('Scoring', 'Scoring', ['fa-sitemap'], `/sidebar/score/Taxonomy/${this.selected.Uid}`, null, 6));
                    this.rightSidebarService.showItem(new RightSidebarItem('Comments', 'Comments', ['fa-comments'], `/sidebar/comments/Taxonomy/${this.selected.ID}/${this.selected.DisplayValue.replace("/", "%2F")}`, null, 31));
                    this.rightSidebarService.showItem(new RightSidebarItem('Actions', 'Actions', null, `/sidebar/actions/Taxonomy/${this.selected.ID}/${this.model.Name.replace("/", "%2F")}`, null, 26));
                });
                this.rightSidebarService.showHeader(true);
            
                this.headerBreadcrumbService.clearBreadcrumbs();
                let areaBreadcrumb = new Breadcrumb(
                    this.currentAreaName ? this.currentAreaName : res, `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${SiteUrlHelpers.SITE_URL_MODEL_CLASSIFICATION}`
                );
                this.headerBreadcrumbService.showBreadcrumb(areaBreadcrumb);
                this.headerBreadcrumbService.showBreadcrumb(
                    new Breadcrumb(this.model.Name,
                        SiteUrlHelpers.getObjectUrl('TAXONOMYTYPE', this.model.ID),
                        undefined,
                        'TAXONOMYTYPE',
                        this.model.ID,
                        undefined,
                        undefined,
                        true,
                        false
                    ));

                if (this.selected && this.selected.ID > 0) {
                    this.checkParent(this.selected);
                    this.headerBreadcrumbService.showBreadcrumb(
                        new Breadcrumb(this.selected.DisplayValue,
                            undefined,
                            true,
                            'Taxonomy',
                            this.selected.ID,
                            this.buildTreeNodeArray(this.modelHierarchy, this.selected.ParentID,false),
                            this.findSelectedTreeNode(this.selected.ID),
                            false));
                }
            });
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
            let selArray = this.modelHierarchy.filter(x => x.ID == selectedHierarchyId);
            if (selArray.length > 0) this.selected = selArray[0];
            else {
                //console.log("ERROR INVALID SELECTED HIERARCHY ID SPECIFIED.", selectedHierarchyId);
                this.selected = (this.modelHierarchy.length && this.modelHierarchy.length > 0) ? this.modelHierarchy[0] : null;
            }
        } else {
            this.selected = (this.modelHierarchy.length && this.modelHierarchy.length > 0) ? this.modelHierarchy[0] : null;
        }

        this.assetID = this.selected.AssetID;

        this.loadPermissions(this.permissionsService, StringConstants.ObjectTaxonomy, this.selected.ID);
        this.buildBreadcrumb();
        return Promise.resolve(null);
    }
    private checkParent(modelItem: ModelHierarchy) {
        if (modelItem.ParentID > 0 && this.modelHierarchy) {
            let parentAr = this.modelHierarchy.filter(x => x.ID == modelItem.ParentID);
            let parent: ModelHierarchy;
            if (parentAr.length > 0) {
                parent = parentAr[0];
                let crumb = new Breadcrumb(parent.DisplayValue,
                    SiteUrlHelpers.getObjectUrl('TAXONOMY', parent.ID, this.modelId),
                    true,
                    'Taxonomy',
                    parent.ID,
                    this.buildTreeNodeArray(this.modelHierarchy, parent.ParentID,false),
                    this.findSelectedTreeNode(parent.ID), false)
                this.crumbs.unshift(crumb);
                this.checkParent(parent);
            } 
        } else {
            this.crumbs.forEach(x => this.headerBreadcrumbService.showBreadcrumb(x));
        }
    }
    private loadModelHierarchy(modelId: number, selectedHierarchyId: number): void {
        this.modelsService.getModelHierarchy(modelId).subscribe(result => {
                this.modelHierarchy = result;

                this.treeNodeArray = this.buildTreeNodeArray(this.modelHierarchy);

                this.selectModelHierarchy(selectedHierarchyId);
                this.messages = []; //clear any messages for this model
                this.loadItemSurvey(this.modelId);

                this.setBrowserTitle(this.titleService, this.model.Name);
                this.clearSidebar();
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
                children: (includeChildren ? this.buildTreeNodeArray(models, root.ID):null) //recursively find its children
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
