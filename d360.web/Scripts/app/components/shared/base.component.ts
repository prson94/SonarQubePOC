import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { RightSidebarService  } from '../../services/index';
import { Subscription }   from 'rxjs/Subscription';

export class BaseComponent {    
    protected isLoading = false;

    //sidebar
    sidebarSubscription: Subscription;

    isAuditVisible: boolean = false;
    isOwnershipVisible: boolean = false;

    constructor(protected rightSidebarService?: RightSidebarService) {  }

    protected setBrowserTitle(tileService: Title, area: string) {
        tileService.setTitle(`D3S - ${area}`);
    }


    setCommonRightSideBar(hasAudit?: boolean, hasOwnership?: boolean) {
        if (this.rightSidebarService) {
            if (hasAudit || hasAudit === undefined) this.rightSidebarService.showItem(new RightSidebarItem('Audit', 'audit'));
            if (hasOwnership) this.rightSidebarService.showItem(new RightSidebarItem('Ownership', 'ownership'));


            this.sidebarSubscription = this.rightSidebarService.rightSidebarClicked$.subscribe(
                item => {
                    if (item.tag == 'audit')
                        this.isAuditVisible = !this.isAuditVisible;
                    else if (item.tag == 'ownership')
                        this.isOwnershipVisible = !this.isOwnershipVisible;
                    else
                        this.showHideBreadcrumbItem(item);
                });
        }
    }

    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        console.log('show/hide :');
        console.log(activatedItem);
    }

    clearSidebar() {
        if (this.rightSidebarService) {
            this.rightSidebarService.clearItems();
            this.sidebarSubscription.unsubscribe();
        }
    }
}