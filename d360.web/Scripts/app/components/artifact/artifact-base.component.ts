import { Breadcrumb } from '../../models/breadcrumb.model';
import { MessagesService, HeaderBreadcrumbService, PageHeader, RightSidebarService  } from '../../services/index';
import { Title } from '@angular/platform-browser';
import { BaseComponent } from '../shared/base.component';
import { Subscription }   from 'rxjs/Subscription';
import { RightSidebarItem } from '../../models/rightsidebar.model';

export class ArtifactBaseComponent extends BaseComponent {    
    public areaLink: string = undefined;
    public areaDescription: string = "base";
    public area: string = "Glossary";

    protected isLoading = false;

    //sidebar
    sidebarSubscription: Subscription;

    isAuditVisible: boolean = false;

    constructor(protected headerBreadcrumbService: HeaderBreadcrumbService, protected pageHeader: PageHeader, protected rightSidebarService?: RightSidebarService) {
        super();
        pageHeader.description = "";
    }        

    setBrowserTitle(tileService: Title, area: string) {
        tileService.setTitle(`D3S - ${area}`);
    }

    setCommonRightSideBar(hasAudit?: boolean) {
        if (this.rightSidebarService) {
            this.rightSidebarService.showItem(new RightSidebarItem('Audit', 'audit'));


            this.sidebarSubscription = this.rightSidebarService.rightSidebarClicked$.subscribe(
                item => {                    
                    if (item.tag == 'audit')
                        this.isAuditVisible = !this.isAuditVisible;
                    else
                        this.showHideBreadcrumbItem(item);
                });
        }
    }

    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        console.log('show/hide ' + activatedItem);
    }

    clearSidebar() {
        if (this.rightSidebarService) {
            this.rightSidebarService.clearItems();
            this.sidebarSubscription.unsubscribe();
        }
    }
}