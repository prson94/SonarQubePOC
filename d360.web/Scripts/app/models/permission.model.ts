export class Permission {
    Claim: string;
    ClaimObject: string;    

    static hasPermission(permissions: Permission[], object: string, claim: string): boolean {
        var uObject = object.toUpperCase();
        var uClaim = claim.toUpperCase();

        let index = permissions.findIndex(i => i.Claim.toUpperCase() == uClaim && i.ClaimObject.toUpperCase() == uObject);

        if (index >= 0 && index < permissions.length) return true;

        return false;
    }
}
