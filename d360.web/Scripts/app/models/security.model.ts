import { MenuItem } from "primeng/api";
import { Operator } from "./operator.model";

export type PolicySecurityType = 'Group' | 'User';
//export enum PolicySecurityType {
//	Group = 1,
//	User = 2
//}

export class AssetOwnerModel {
	uid: string;
	isOverride: boolean;

	roleUid: string;
	roleName: string;
	groupUid: string;
	groupName: string;
	resourceUid: string;
    resourceName: string;
	securityType: number;
	context: string;
	ruleName: string;

	// Used by UI.
	MenuItems: MenuItem[];
}

export class PolicyEditOptionsModel {
	assetTypes: any[];
	roles: any[];
}

export class PolicyEditAssetTypeOptionsModel {
	fields: any[];
	intersectTypes: any[];
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

export class SecurityPolicyThen {
	fieldName?: string;
	operator: Operator;
	value?: string;
	securityType: string;
	securityUid?: string;
}

export class SecurityPolicyWhen {
	checkType: string;
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
	securityType: PolicySecurityType;
	applyToType: boolean;
	visible: boolean;	 
	thenConditions: SecurityPolicyThen[];
	whenConditions: SecurityPolicyWhen[];
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
	securityType: number;
	securityUid: string;
	context: string;
}

export class CreateSecurityPolicyOverride extends SecurityPolicyOverride {
}

export class ReadSecurityPolicyOverride extends SecurityPolicyOverride {
	uid: string;
}

export class UpdateSecurityPolicyOverride {
	uid: string;
	context: string;
}