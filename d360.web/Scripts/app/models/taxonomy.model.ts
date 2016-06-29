export class Taxonomy {
    ID: number;
    Name: string;
    TaxonomyTypeClass: string; // classification text name
    Class: number; //classification id
    Description: string;
    MaximumDepth: number;
    IconBackColor: string;
    IconForeColor: string;
}

export class TaxonomyLevel {
    Name: string;
    Description: string;
    Level: number;
    TaxonomyTypeID: number;
}

export class TaxonomyClassification {
    Name: string;
    ID: number;
}