export class SiteMenuItem {
    Name: string;
    Url: string;
    Items: SiteMenuItem[];
    IsLink: boolean;
    IsHomePage: boolean = false;
    count: number;
}

export class SiteMenu {
    MenuID: string;
    NavigationItems: SiteMenuItem[];
    ShouldDisplay: boolean = true;
    SortOrder: number;
    isActiveItem: boolean = false;

    ngUrl: string;
    
    Icon: string;
    Title: string;
}

export class SiteMenuModel {
    MenuItems: SiteMenu[];
    IsAdmin: boolean = false;
}

export class SiteNav {
    ID: number;
    ParentID: number;
    Name: string;
    Route: string;
    SortOrder: number;
    Object: string;
    ObjectID: number;

    IsCustom: boolean = false;

    Icon: string;
    Title: string;

    Permissions: SiteNavPermission[] = [];
    public static zindex: number = 1000;
}

export class SiteNavPermission {
    SiteNavID: number;
    Object: string;
    ObjectID: number;

    Name: string;
}

