export class D3SObjectHelpers {


    // Given an d3s object name get its friendly name to display to users
    static getObjectTypeFriendlyName(objectType: string) : string {
        switch (objectType.toUpperCase()) {
            case "ARTIFACT":
                return "Artifact";
            case "TAXONOMY":
                return "Model";
            case "DOMAIN":
                return "Reference";
            default:                
                return objectType;
        }
    }
}