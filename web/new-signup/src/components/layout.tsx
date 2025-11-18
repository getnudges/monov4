import { FC, ReactNode } from "react";
import { useTranslation } from "react-i18next";

interface ResponsiveLayoutProps {
  children: ReactNode;
}

const ResponsiveLayout: FC<ResponsiveLayoutProps> = ({ children }) => {
  const { t } = useTranslation();
  return (
    <div className="flex flex-col min-h-screen">
      <main className="flex-grow container mx-auto px-4 py-6 sm:py-8">
        {children}
      </main>

      <footer className="bg-secondary text-secondary-foreground">
        <div className="container mx-auto px-4 py-4 sm:py-6 text-center text-sm sm:text-base">
          <p>&copy; 2025 Nudges. {t("translation")}</p>
        </div>
      </footer>
    </div>
  );
};

export default ResponsiveLayout;
