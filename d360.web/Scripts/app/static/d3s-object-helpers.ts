export module D3SObjectHelpers {


    // Given an d3s object name get its friendly name to display to users
    export function getObjectTypeFriendlyName(objectType: string) {
        switch (objectType.toUpperCase()) {
            case "FUSIONATTRIBUTES":
                return "Fusion";
            case "ARTIFACT":
                return "Glossary";
            case "TAXONOMY":
                return "Model";
            case "DOMAIN":
                return "Reference";
            default:
                console.log("[INFO] - UNHANDLED OBJECT TYPE", objectType);
                return objectType;
        }
    }
}