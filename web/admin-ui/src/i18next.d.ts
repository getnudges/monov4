import "i18next";
import { resources, defaultNS } from "./i18n";

declare module "i18next" {
  interface CustomTypeOptions {
    defaultNS: defaultNS;
    resources: (typeof resources)["en"];
    enableSelectors: true;
  }
}
