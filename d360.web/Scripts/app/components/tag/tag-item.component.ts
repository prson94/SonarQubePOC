import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { RulesService } from '../../services/rules.service';
import { PermissionsService } from '../../services/permissions.service';
import { SurveysService } from '../../services/surveys.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { RuleDetail, RuleImplementation, RuleType } from '../../models/rule.model';
import { MessageBarItem } from '../../models/message-bar-item.model';
import { SurveyType } from '../../models/survey.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';
import { RightSidebarItem, AssetAction, EditFormData } from '../../models/rightsidebar.model';
import { Permission } from '../../models/responsibility-type.model';
import { Observable, Subscribable, Subscription } from 'rxjs';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { TagService } from '../../services/tag.service';
import { TagType, TagDetail, TagItem } from '../../models/tag.model';
import { retry, window } from 'rxjs/operators';
import { Action } from 'rxjs/internal/scheduler/Action';
import { EditableColumn } from 'primeng/table';

declare var CompanySettings;

@Component({
    selector: 'd3s-rule-item',
    providers: [RulesService, PermissionsService, TagService, GridDefinitionService],
    templateUrl: 'tag-item.component.html'
})

export class TagItemComponent extends BaseComponent implements OnInit, OnDestroy {
    routeParamsSubscription: any;
    tagId: number;
    tag: TagType;
    tagUsage: TagDetail[];
    selected: TagDetail;
    private currentAreaNameSubscription: any;
    private currentAreaName: string;

    private messages: MessageBarItem[] = [];

    private sub: any;
    actions: AssetAction;


    constructor(private route: ActivatedRoute,
        private router: Router,
        protected tagsService: TagService,
        protected titleService: Title,
        protected messagesService: MessagesObservableService,
        private gridDefinitionService: GridDefinitionService,
        private headerActionsService: HeaderActionsService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected permissionsService: PermissionsService,
        rightSidebarService: RightSidebarService) {
        super();
        this.rightSidebarService = rightSidebarService;

    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.tagId = +params['tagId']; // (+) converts string 'id' to a number
            this.headerBreadcrumbService.setCurrentObjectInfo('Tag', this.tagId);
            this.logAction('open', 'Tag', this.tagId);
            this.isLoading = true;
            this.messages = [];


            this.loadPermissions(this.permissionsService, "Tag", this.tagId)
                .then(p => {
                    this.load();
                });

            this.currentAreaName = "Tag";
        });
    }

    ngOnDestroy() {

    }

    load() {
        this.isLoading = true;
        this.tagsService.getTagById(this.tagId)
            .subscribe(result => {
                this.tag = result;
                this.setObjectInfo('Tag', this.tagId);
                this.buildBreadcrumb();
                this.setBrowserTitle(this.titleService, this.tag.Value);

                this.setObjectInfo(
                    'Tag',
                    this.tagId,
                    this.tag.Value,
                    null,
                    null,
                    this.tag.uid
                );
                this.setCommonRightSideBar(true);
                this.rightSidebarService.showHeader(true);

                this.setActions();

                if (this.auditSidebar) {
                    this.auditSidebar.hasDynamicUrl = true;
                    this.auditSidebar.dynamicUrlCallback = (() => {
                        return `/sidebar/audit/Tag/${this.tagId}`
                    });
                }

                this.tagsService.getTagDetails(this.tag.uid)
                    .subscribe(data => {
                        this.tagUsage = data.items;
                        this.selected = this.tagUsage[0];
                        this.isLoading = false;

                    });

            },
                err => {
                    this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
                });
    }

    buildBreadcrumb() {
        this.rightSidebarService.setCurrentArea(this.tag.Value, 'fa-tag', 'Tagged Assets');

    }

    getAssetType(item: TagDetail) {

        switch (item.AssetType) {
            case 'ArtifactType': return `Glossary <i class='fa fa-angle-right'></i> ${item.AssetTypeName}`;
            case 'PolicyType': return `Policy <i class='fa fa-angle-right'></i> ${item.AssetTypeName}`;
            case 'TaxonomyType': return `Model <i class='fa fa-angle-right'></i> ${item.AssetTypeName}`;
            case 'RuleType': return `Rule <i class='fa fa-angle-right'></i> ${item.AssetTypeName}`;
            default: return '';
        }

    }

    openTagPage(item: TagItem) {
        if (item.Id != this.tagId) {
            this.router.navigate([`${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${item.Id}`]);
        }
    }

    export() {
        this.tagsService.exportTagsByUid(this.tag.uid);
    }

    setActions() {
        this.actions = new AssetAction();
        this.actions.isVisible = true;
        this.actions.showBack = true;
        this.actions.showDelete = true;
        this.actions.showEdit = true;
        this.actions.editCallback = this.onActionEditClick.bind(this);

        let editAction: EditFormData = new EditFormData();
        editAction.title = 'Edit Tag';
        editAction.closeClick = this.onActionEditCloseClick.bind(this);
        editAction.selected = this.tag.uid;
        editAction.isModalVisible = false;
        editAction.modalTitle = "Edit Tag";
        editAction.objectID = this.tag.uid;
        editAction.objectType = 'Tag';
        editAction.saveClick = this.saveTag.bind(this);
        editAction.showAsModal = true;

        this.actions.edit = editAction;

        this.rightSidebarService.setActionTitleItems(this.actions);
    }

    onActionEditCloseClick() {
        if (this.actions) {
            this.actions.edit.isModalVisible = false;
            this.rightSidebarService.setActionTitleItems(this.actions);
        }
    }

    onActionEditClick() {
        this.actions.edit.isModalVisible = true;
        this.rightSidebarService.setActionTitleItems(this.actions);
    }

    saveTag(event) {

        if (event.additionalOption && event.additionalOption.code) {
            let arr: string[] = [];
            arr.push(event.item.uid);
            this.consolidateTags(event.additionalOption.code, arr);
            return;
        }

        this.tagsService.saveTag(event.item)
            .subscribe(result => {
                let msg: string = '';
                if (event.item.uid == undefined) {
                    msg = `${result.Value} succesfully created`;
                }
                else {
                    msg = `${result.Value} succesfully updated`;
                }
                this.showMessageForResult(this.messagesService, result, msg);

            });
    }

    consolidateTags(parentUid: string, childrenUids: string[]) {
        this.tagsService.consolidateTags(parentUid, childrenUids)
            .subscribe(result => {

                if (result) {

                    this.messagesService.showInfoMessage("Success", "Tag consolidation succesfull");

                }
                this.onActionEditCloseClick();
            }, err => {
                this.showMessageForResult(this.messagesService, err);

            });
    }

};