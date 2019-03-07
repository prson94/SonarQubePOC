import {Component} from '@angular/core';
import {HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import {AttributeTypeService} from '../../../services/attribute-type.service';
import {RightSidebarService} from '../../../services/right-sidebar.service';
import {MessagesService} from '../../../services/messages.service';
import {AdminBaseComponent} from '../admin-base.component';
import {AttributeType} from '../../../models/attribute-type.model';
import {TreeNode} from 'primeng/primeng';
import {Title} from '@angular/platform-browser';
import {type} from 'os';

@Component({
    selector: 'd3s-admin-attributes-component',
    providers: [AttributeTypeService],
    templateUrl: './admin-attributes.component.html'
})

export class AdminAttributesComponent extends AdminBaseComponent {
    attributes: TreeNode[] = [];
    selected: TreeNode;

    showDelete: boolean = false;
    showEditor: boolean = false;
    theDeleteCallback: Function;
    parentID: number = 0;

    constructor(
        rightSidebarService: RightSidebarService,
        private attributeTypeService: AttributeTypeService,
        protected messagesService: MessagesService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title
    ) {
        super(headerBreadcrumbService, titleService, rightSidebarService);

        this.areaName = "Attribute Groups";
        this.setCommonItems();
        this.setCommonRightSideBar(true);

        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/AttributeType/${this.selected.data.ID}`
            });
        }

        this.theDeleteCallback = this.deleteAttributeType.bind(this);
    }

    ngOnInit() {
        this.getAttributes();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    getAttributes() {
        this.isLoading = true;

        this
            .attributeTypeService
            .getAttributes()
            .subscribe(result => {
                this.attributes = this.formTree(result);
                this.selected = this.attributes.length > 0 ? this.attributes[0] : null;
                this.isLoading = false;
            });
    }

    private formTree(data): TreeNode[] {
        let tree = new Array<TreeNode>();

        data.filter(d => d.ParentID == null).forEach(d => {
            tree.push({data: d, children: []});
        });

        tree.forEach(t => {
            this.formTreeR(t, data);
        });

        return tree;
    }

    private formTreeR(node: TreeNode, data) {
        data.filter(d => d.ParentID == node.data.ID).forEach(d => {
            let child: TreeNode = {data: d, children: []};
            node.children.push(child);
            this.formTreeR(child, data);
        });
    }


    deleteAttributeType(id: number) {
        this
            .attributeTypeService
            .deleteAttributeType(id)
            .subscribe(res => {
                    this.showMessageForResult(this.messagesService, res);
                    this.showDelete = false;
                    this.selected = this.attributes.length > 0 ? this.attributes[0] : null;
                    this.getAttributes();
                }
            );
    }

    saveAttributeType(event) {
        this.isLoading = true;

        this
            .attributeTypeService
            .saveAttributeType(event.attribute)
            .subscribe(result => {
                if (result.type == "error") {
                    this.isLoading = false;

                    this.messagesService.showError(result.title, result.message);
                } else {
                    this.getAttributes();

                    this.isLoading = false;
                    this.showEditor = false;
                }
            });
    }

    closeEditor() {
        this.showEditor = false;

        if (this.selected == null) {
            this.selected = this.attributes.length > 0 ? this.attributes[0] : null;
        }
    }

    add(parentID: number) {
        this.showEditor = true;
        this.selected = null;
        this.parentID = parentID;
    }
}
