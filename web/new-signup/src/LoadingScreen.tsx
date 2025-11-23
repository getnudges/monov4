import { useTranslation } from "react-i18next";

export default function LoadingScreen() {
  const { t } = useTranslation("common");
  return (
    <div>
      <p>{t("loading")}</p>
    </div>
  );
}
