import { MenuItem } from "primeng/api";
import { Operator } from "./operator.model";

export enum Permission {
    ReadAsset = 1,
    AddAsset = 2,
    DeleteAsset = 4,
    EditAsset = 8,

    ReadResponsibilities = 32,
    AddResponsibilities = 64,
    DeleteResponsibilities = 128,
    EditResponsibilities = 256,

    ReadRelationships = 1024,
    AddRelationships = 2048,
    DeleteRelationships = 4096,
    EditRelationships = 8192,
}

export class CreateRole {
	name: string;
	description: string;
	permissions: number;
}

export class ReadRole {
    name: string;
    description: string;
	permissions: number;
	updatedOn: string;
	uid: string;

	MenuItems: MenuItem[];
}

export enum SecurityPolicyType {
	Group = 1,
	User = 2
}

export class SecurityPolicyThen {
	fieldName?: string;
	operator: Operator;
	value?: string;
	securityUid?: string;
}

export class SecurityPolicyWhen {
	fieldName?: string;
	intersectTypeUid?: string;
	operator: Operator;
	value?: string;
	assetUid?: string;
}

abstract class SecurityPolicy {
	name: string;
	assetTypeUid: string;
	roleUid: string;
	securityType: SecurityPolicyType;
	applyToType: boolean;
	isVisible: boolean;

	then: SecurityPolicyThen[];
	when: SecurityPolicyWhen[];
}

export class CreateSecurityPolicy extends SecurityPolicy {

}

export class ReadSecurityPolicy extends SecurityPolicy {
	uid: string;
	assetTypeName: string;
	roleName: string;

	MenuItems: MenuItem[];
}

abstract class SecurityPolicyOverride {
	assetUid: string;
	roleUid: string;
	securityType: SecurityPolicyType;
	securityUid: string;
}

export class CreateSecurityPolicyOverride extends SecurityPolicyOverride {
}

export class ReadSecurityPolicyOverride extends SecurityPolicyOverride {
	uid: string;
}