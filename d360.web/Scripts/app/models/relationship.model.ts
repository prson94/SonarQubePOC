import { MenuItem } from "primeng/api";
import { Predicate } from "./predicate.model";

/*global $localize*/

export enum Cardinality {
	One = 1,
	Many = 2
}

export class RelationshipTypeEdge {
	Uid: string;
	Name: string;
	Class: string;
	Cardinality: string;
}

export class RelationshipTypeSimpleUIModel {
	Uid: string;
	RelationshipTypeName: string;
	HasRelationships?: boolean;
	HasRelationshipsTextValue?: string;
	TotalRelationshipCount?: number;
	MenuItems?: MenuItem[];
}

export class RelationshipType {
	Id: number;
	Uid: string;
	State: string;
	IsSystem: boolean;
	Predicate: Predicate;
	Subject: RelationshipTypeEdge;
	Object: RelationshipTypeEdge;
	HasFieldTypes?: boolean;
	HasRelationships?: boolean;
	TotalRelationshipCount?: number;

	public static ConvertToUIModeldata(data: RelationshipType): RelationshipTypeSimpleUIModel {
		return {
			RelationshipTypeName: data.Subject.Name + " - " + data.Predicate.Name + " - " + data.Object.Name,
			Uid: data.Uid,
			HasRelationships: data.HasRelationships,
			HasRelationshipsTextValue: data.HasRelationships ? $localize`True` : $localize`False`,
			TotalRelationshipCount: data.TotalRelationshipCount
		};
	}
}

export class RelationshipCount {
	IntersectTypeUid: string;
	Count: number;
	IsSubject: boolean;
}

export class RelationshipDetail {
	ID: number;
	LimitedChangesOnly: boolean;
	Predicate: string;
	PredicateType: number;
	Subject: string;
	SubjectDisplayText: string;
	SubjectCardinality: number;
	Object: string;
	ObjectDisplayText: string;
	ObjectCardinality: number;
}

export class ObjectRelationship {
	IntersectTypeID: number;
	ParentIntersectID: number;
	TargetName: string;
	TargetType: string;
	TargetTypeID: number;
	PredicateName: string;
	Uid: string;
}

export class RelatedItem {
	Name: string;
	Type: string;
	ID: number;
	Uid: string;
}

export class PredicateDropdown {
	label: string;
	value: string;
	isSemantic: boolean;
	type: string;
}

export class RelationshipV2 {
	SubjectAssetUid: string;
	ObjectAssetUid: string;
	Fields: any = {};
}