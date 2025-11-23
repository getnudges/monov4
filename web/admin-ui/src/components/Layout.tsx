import type { FC, ReactNode } from "react";

interface ResponsiveLayoutProps {
  children: ReactNode;
}

const ResponsiveLayout: FC<ResponsiveLayoutProps> = ({ children }) => {
  return (
    <main>
      <div>{children}</div>
    </main>
  );
};

export default ResponsiveLayout;
