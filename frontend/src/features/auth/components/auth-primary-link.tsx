import Link from "next/link";
import type { ReactNode } from "react";

type AuthPrimaryLinkProps = {
  children: ReactNode;
  href: string;
};

export function AuthPrimaryLink({ children, href }: AuthPrimaryLinkProps) {
  return (
    <Link
      className="inline-flex min-h-14 w-full items-center justify-center rounded-control bg-linear-to-r from-primary to-secondary px-5 text-sm font-semibold text-text-primary transition-[filter,transform] duration-200 hover:brightness-110 active:scale-[0.99] active:brightness-95 focus-visible:outline-primary"
      href={href}
    >
      {children}
    </Link>
  );
}
