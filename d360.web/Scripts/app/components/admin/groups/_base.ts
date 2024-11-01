import { StringConstants } from "../../../static/string-constants";
import { Tab } from "../../shared/tabs/tabs.models";

export class GroupBasePage {
	defaultPagingOptions: number[] = [25,50,100];
	header: string = "Groups";
	icon: string = "fa-cog";
	isLoading: boolean = false;
	simpleSearchTooltipHTML: string = StringConstants.simpleSearchTooltipHTML;
	tabs: Tab[] = [
		{
			url: `/admin/groups`,
			title: $localize`Groups`,
			isVisible: () => true
		}, {
			url: `/admin/groups/fields`,
			title: $localize`Fields`,
			isVisible: () => true
		}, {
			url: `/admin/groups/00000001-0000-0000-0000-b00000000012/log`,
			title: $localize`Change Log`,
			isVisible: () => true
		}
	];
}
