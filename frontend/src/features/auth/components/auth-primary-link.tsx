import Link from "next/link";
import type { ReactNode } from "react";
import { buttonClassName } from "@/components/ui/button";

type AuthPrimaryLinkProps = {
  children: ReactNode;
  href: string;
};

export function AuthPrimaryLink({ children, href }: AuthPrimaryLinkProps) {
  return (
    <Link
      className={buttonClassName({
        className: "min-h-14",
        fullWidth: true,
        size: "lg",
      })}
      href={href}
    >
      {children}
    </Link>
  );
}
