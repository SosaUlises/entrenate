import type { ReactNode } from "react";
import { cn } from "@/lib/class-names";

type MainPageHeaderProps = {
  action?: ReactNode;
  description: ReactNode;
  id: string;
  title: ReactNode;
};

export function MainPageHeader({
  action,
  description,
  id,
  title,
}: MainPageHeaderProps) {
  return (
    <header
      className={cn(
        "mb-8",
        action ? "sm:flex sm:items-start sm:justify-between sm:gap-6" : undefined,
      )}
    >
      <div className="min-w-0 max-w-lg">
        <h1
          className="font-brand text-[2rem] font-bold leading-[1.08] text-text-primary"
          id={id}
        >
          {title}
        </h1>
        <p className="mt-2 max-w-md text-[0.9375rem] leading-6 text-text-secondary">
          {description}
        </p>
      </div>
      {action ? <div className="mt-4 shrink-0 sm:mt-0">{action}</div> : null}
    </header>
  );
}
