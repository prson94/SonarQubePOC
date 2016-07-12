export class SiteMenuItem {
    Name: string;
    Url: string;
    Items: SiteMenuItem[];
}

export class SiteMenu {
    MenuID: string;
    NavigationItems: SiteMenuItem[];
    ShouldDisplay: boolean;
}

