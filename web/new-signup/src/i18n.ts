import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import LanguageDetector from "i18next-browser-languageDetector";

import common from "./locales/en/common.json";

export const resources = {
  en: {
    common: common,
  },
} as const;

export const defaultNS = "common";

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    defaultNS,
    ns: ["common"],
    lng: "en",
    resources,
    debug: import.meta.env.DEV,
    fallbackLng: "en",
  });

export default i18n;
