import { StringConstants } from "../../../static/string-constants";
import { Tab } from "../../shared/tabs/tabs.models";
import { AppConstants } from '../../../static/constants';


export class GroupBasePage {
	rowsPerPage: number = AppConstants.DEFAULT_ROWS_PER_PAGE;
	defaultPagingOptions: number[] = AppConstants.DEFAULT_PAGING_OPTIONS;
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
