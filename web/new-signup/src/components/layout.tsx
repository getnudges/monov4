import { FC, ReactNode } from "react";

interface ResponsiveLayoutProps {
  children: ReactNode;
}

const ResponsiveLayout: FC<ResponsiveLayoutProps> = ({ children }) => {
  return (
    <div className="flex flex-col min-h-screen">
      <main className="flex-grow container mx-auto px-4 py-6 sm:py-8">
        {children}
      </main>

      <footer className="bg-secondary text-secondary-foreground">
        <div className="container mx-auto px-4 py-4 sm:py-6 text-center text-sm sm:text-base">
          <p>&copy; 2025 Nudges. All rights reserved.</p>
        </div>
      </footer>
    </div>
  );
};

export default ResponsiveLayout;
