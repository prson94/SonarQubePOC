import { Breadcrumb } from '../../models/breadcrumb.model';
import { MessagesService, HeaderBreadcrumbService, PageHeader  } from '../../services/index';
import { Title } from '@angular/platform-browser';
import { BaseComponent } from '../shared/base.component';

export class ArtifactBaseComponent extends BaseComponent {    
    public areaLink: string = undefined;
    public areaDescription: string = "base";
    public area: string = "Glossary";

    protected isLoading = false;

    constructor(protected headerBreadcrumbService: HeaderBreadcrumbService, protected pageHeader: PageHeader) {
        super();
        pageHeader.description = "";
    }        

    setBrowserTitle(tileService: Title, area: string) {
        tileService.setTitle(`D3S - ${area}`);
    }
}