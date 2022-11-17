export type Tab =  {
    title: string;
    count?: number;
    url: string;
    tag?: any;
    warningMessage?: string;
    subTabsUrl?: string[] ;
    isVisible?: () => boolean;
}
