import { MenuItem } from 'primeng/primeng';

export class ActionBarItem {
    icon: string;
    tooltip: string;
    menuItems: MenuItem[];
    action: Function;
}
