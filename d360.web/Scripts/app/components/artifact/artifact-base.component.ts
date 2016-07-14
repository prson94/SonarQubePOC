import { Breadcrumb } from '../../models/breadcrumb.model';
import { MessagesService, HeaderBreadcrumbService, PageHeader  } from '../../services/index';

export class ArtifactBaseComponent {    
    public areaLink: string = undefined;
    public areaDescription: string = "base";
    public area: string = "Glossary";

    protected isLoading = false;

    constructor(protected headerBreadcrumbService: HeaderBreadcrumbService, protected pageHeader: PageHeader) {
        pageHeader.description = "";
    }        
}