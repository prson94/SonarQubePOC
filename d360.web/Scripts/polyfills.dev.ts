//global var for angular i18n translations
//in development mode, we will not include localization to improve build performance
//so we need to manually define $localize function

(window as any).$localize = (x: any) => x;