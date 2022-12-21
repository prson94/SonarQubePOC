import { Component, OnDestroy, OnInit } from '@angular/core';
import { AdminBaseComponent } from '../admin-base.component';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { Title } from '@angular/platform-browser';
import { ExportTemplateService } from '../../../services/export-template.service';
import { ExportTemplate } from '../../../models/export-template.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-export-templates-component',
    templateUrl: './admin-export-templates.component.html',
    providers: [ExportTemplateService],       
})

export class AdminExportTemplatesComponent extends AdminBaseComponent implements OnDestroy, OnInit {
    public selected: ExportTemplate;

    exportTemplates: ExportTemplate[];
    showDelete: boolean = false;
    showEditor: boolean = false;
    uploadedFiles: any[] = [];

    theDeleteCallback: Function;
    searchText = $localize`Search...`;
    deleteModalMsg = $localize`Are you sure you want to delete the selected item?`;

    constructor(
        secondaryNavService: SecondaryNavService,
        headerBreadcrumbService: HeaderBreadcrumbService,        
        titleService: Title,
        private exportTemplateService: ExportTemplateService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
        ) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.areaName = StringConstants.Section_ExportTemplates;
        this.setCommonItems();
                
        this.setCommonSecondaryNavTabs({ hasAudit: false });
        this.theDeleteCallback = this.deleteExportTemplate.bind(this);
    }
    
    ngOnDestroy() {
        this.clearSidebar();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.exportTemplateService.getExportTemplates().subscribe((result) => {
            this.exportTemplates = result;
            if (this.selected == null && this.exportTemplates != null && this.exportTemplates.length > 0)
                {this.selected = this.exportTemplates[0];}
            else if (this.selected != null && this.exportTemplates != null && this.exportTemplates.length > 0) {
                const item = this.exportTemplates.filter((x) => x.Uid === this.selected.Uid);
                if (item != null && item.length !== 0)
                    {this.selected = item[0];}
            }

            this.getSelectedTemplateID();
            this.isLoading = false;
        });
    }

    public deleteExportTemplate(uid: string) {
            this.exportTemplateService.deleteExportTemplates(uid).subscribe((result) => {            
            this.showDelete = false;
            this.selected = null;
            this.load();
        });
    }   

    public saveExportTemplate(event) {
        this.exportTemplateService.saveExportTemplate(event.item).subscribe((result) => {            
                this.showEditor = false;
                this.load();                
            });
    }

    public saveFields(event) {
        this.selected.IncludeFieldTypes = event.IncludeFieldTypes;
        this.exportTemplateService.saveExportTemplate(this.selected).subscribe((result) => {           
            for (let i = 0; i < this.exportTemplates.length; i++) {
                if (this.exportTemplates[i].Uid === this.selected.Uid) {
                    this.exportTemplates[i].IncludeFieldTypes = this.selected.IncludeFieldTypes;
                    this.getSelectedTemplateID();
                }
            }
        });
    }

    public closeEditor() {
        this.showEditor = false;
    }

    public openEditor(e: any, item: any) {        
        this.showEditor = false;
        this.selected = item;        
        this.selectNode(e, true);
        this.showDelete = false;
    }

    public selectNode(e: any, showEditor: boolean = false) {        
        if (e == null)
            {return;}
        if (!this.selected.ID) {
            this.getSelectedTemplateID(showEditor);
        } else {
            this.showEditor = showEditor;
        }
        
    }

    public getSelectedTemplateID(showEditor: boolean = false) {
        if (this.selected) {
            this.exportTemplateService.getExportTemplateId(this.selected.Uid).subscribe((item) => { this.selected.ID = item; this.showEditor = showEditor; });
        }
    }

    clearTemplate() {
        this.isLoading = true;
        this.exportTemplateService.saveTemplateFile(this.selected).subscribe((result) => {
            this.selected.HasTemplateFile = false;
            this.isLoading = false;
        });
    }
}