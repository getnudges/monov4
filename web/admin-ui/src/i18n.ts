import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import LanguageDetector from "i18next-browser-languageDetector";

import enCommon from "./locales/en/common.json";
import enHome from "./locales/en/Home.json";

export const resources = {
  en: {
    common: enCommon,
    home: enHome,
  },
} as const;

export const defaultNS = "common";

export const i18nextConfig = {
  defaultNS,
  ns: ["common"],
  lng: "en",
  resources,
  debug: import.meta.env.DEV,
  fallbackLng: "en",
};

i18n.use(LanguageDetector).use(initReactI18next).init(i18nextConfig);

export default i18n;
