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

//export class RolePermission {
//    Value: number;
//    ID: string;
//    Category: string;
//    Name: string;
//    Description: number;
//    Selected: boolean;

//	static hasPermission(permissions: RolePermission[], p: Permission): boolean {

//        const index = permissions.findIndex((i) => i.Value === p);

//        if (index >= 0 && index < permissions.length) {
//            return true;
//        }

//        return false;
//    }
//}

export class CreateRole {
	name: string;
	description: string;
}

export class ReadRole {
    name: string;
    description: string;
    updatedOn: string;
    uid: string;
}

export class ReadSecurityPolicy {
	name: string;
	uid: string;
}

