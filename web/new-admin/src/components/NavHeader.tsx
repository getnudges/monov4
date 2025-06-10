import { useCallback, useState } from "react";
import { Link, useLocation } from "wouter";
import { Menu } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Sheet, SheetContent, SheetTrigger } from "@/components/ui/sheet";
import { useAuthorization } from "@/AuthProvider";

const navItems = [
  { href: "/", label: "Home" },
  { href: "/plans", label: "Plans" },
  { href: "/coupons", label: "Coupons" },
  { href: "/clients", label: "Clients" },
  { href: "/subscribers", label: "Subscribers" },
];

const NavLinks = ({ onClick = () => {} }) => {
  const [location] = useLocation();
  return (
    <>
      {navItems.map((item) => (
        <Link key={item.href} href={item.href}>
          <span
            className={`text-sm font-medium transition-colors hover:text-primary ${
              location === item.href ? "text-primary" : "text-muted-foreground"
            }`}
            onClick={onClick}
          >
            {item.label}
          </span>
        </Link>
      ))}
    </>
  );
};
export default function NavHeader() {
  const [isOpen, setIsOpen] = useState(false);
  const [authorized, , setUnauthorized] = useAuthorization();
  const [, navTo] = useLocation();

  const logIn = useCallback(() => navTo("/login"), [navTo]);

  const logOut = useCallback(async () => {
    const resp = await fetch("/auth/logout");
    if (!resp.ok) {
      console.log("logout error", await resp.text());
    }
    setUnauthorized();
    navTo("/login");
  }, [navTo, setUnauthorized]);

  return (
    <header className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="flex h-14 items-center pl-7 pr-7">
        <div className="mr-4 hidden md:flex">
          <Link href="/">
            <span className="mr-6 flex items-center space-x-2">
              <span className="hidden font-bold sm:inline-block">
                UnAd Admin
              </span>
            </span>
          </Link>
          <nav className="flex items-center space-x-6 text-sm font-medium">
            {authorized && <NavLinks />}
          </nav>
        </div>

        <Sheet open={isOpen} onOpenChange={setIsOpen}>
          <SheetTrigger asChild>
            <Button
              variant="ghost"
              className="mr-2 px-0 text-base hover:bg-transparent focus-visible:bg-transparent focus-visible:ring-0 focus-visible:ring-offset-0 md:hidden"
            >
              <Menu className="h-6 w-6" />
              <span className="sr-only">Toggle Menu</span>
            </Button>
          </SheetTrigger>
          <SheetContent side="left" className="pr-0">
            <Link href="/">
              <span
                className="flex items-center"
                onClick={() => setIsOpen(false)}
              >
                <span className="font-bold">UnAd Admin</span>
              </span>
            </Link>
            <nav className="mt-6 flex flex-col space-y-4">
              {authorized && <NavLinks onClick={() => setIsOpen(false)} />}
            </nav>
          </SheetContent>
        </Sheet>
        <div className="flex flex-1 items-center justify-between space-x-2 md:justify-end">
          <div className="w-full flex-1 md:w-auto md:flex-none">
            <Button
              className="w-full md:w-auto"
              variant="outline"
              onClick={authorized ? logOut : logIn}
            >
              {authorized ? "Log out" : "Login"}
            </Button>
          </div>
        </div>
      </div>
    </header>
  );
}
