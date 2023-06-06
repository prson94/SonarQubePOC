import { SelectItem } from 'primeng/api';
import { RelationshipType } from '../../../../../models/relationship.model';

export class RelationshipTypeSelection {
	cntrlName: string;
	index: number;
	options: SelectItem[] = [];
	fieldOptions?: SelectItem[];
	selected: string;

	relationshipTypeUid?: string;
	assetTypeUid?: string;
	direction?: number;

	valuesResolved?: boolean;
	relationshipTypes?: RelationshipType[] = [];
}