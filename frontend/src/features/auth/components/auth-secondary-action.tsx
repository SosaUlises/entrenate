import Link from "next/link";

type AuthSecondaryActionProps = {
  action: string;
  href: string;
  prompt: string;
};

export function AuthSecondaryAction({
  action,
  href,
  prompt,
}: AuthSecondaryActionProps) {
  return (
    <p className="flex flex-col items-center gap-1 text-center text-sm leading-6 text-text-secondary">
      <span>{prompt}</span>
      <Link
        className="rounded-sm font-semibold text-primary underline-offset-4 transition duration-200 hover:opacity-85 hover:underline focus-visible:outline-primary"
        href={href}
      >
        {action}
      </Link>
    </p>
  );
}
