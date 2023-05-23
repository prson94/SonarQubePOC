import { SelectItem } from 'primeng/api';

export class RelationshipTypeSelection {
	cntrlName: string;
	index: number;
	options: SelectItem[] = [];
	fieldOptions?: SelectItem[];
	selected: string;

	relationshipTypeUid?: string;
	assetTypeUid?: string;
	direction?: number;
}