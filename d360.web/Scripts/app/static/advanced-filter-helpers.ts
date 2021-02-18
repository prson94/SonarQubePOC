import { GridField } from "../models/grid-definition.model";

export class AdvancedFiltersHelper {

    static parseFiltersFromTableFilters(data, fields: GridField[]): string {
        var props = Object.keys(data).filter(x=> x != 'global');
        var ret: string = '';
        props.forEach(prop => {
            if (prop != 'global') {
                let fieldName = prop;
                var value = this.escapeString(data[prop].value);
                var field = fields.filter((x) => x.name.toLowerCase() == prop.toLowerCase())[0];
                if (field) {
                    if (field.apiName)
                        fieldName = field.apiName;
                    else
                        fieldName = field.name;
                    
                }

                var type = field.type;
                switch (type) {
                    case 'number':
                    case 'bool':
                        ret += `${fieldName} eq ${value}`;
                        break;
                    case 'date':
                        ret += `${fieldName} ct '${value}'`;
                        break;
                    default:
                        ret += `${fieldName} ct '${value}'`;
                }

                if (prop != props[props.length - 1]) {
                    ret += " and ";
                }
            }
        });
        return ret;
    }

    static escapeString(value): string {
        if (!value) return '';
        value = (value as string).replace(/'/g, "&apos;");
        return `${encodeURIComponent(value)}`;
    }
}