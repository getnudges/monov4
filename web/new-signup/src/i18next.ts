import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import LanguageDetector from "i18next-browser-languageDetector";
import resources from "./resources";

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    debug: import.meta.env.DEV,
    fallbackLng: "en",
  });

export default i18n;
