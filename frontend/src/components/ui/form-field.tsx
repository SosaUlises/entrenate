import type { ReactNode } from "react";

type FormFieldProps = {
  children: ReactNode;
  description?: string;
  error?: string;
  id: string;
  label: string;
};

export function FormField({
  children,
  description,
  error,
  id,
  label,
}: FormFieldProps) {
  const message = error ?? description;
  const messageId = message ? `${id}-message` : undefined;

  return (
    <div className="space-y-2">
      <label className="block text-sm font-semibold text-text-primary" htmlFor={id}>
        {label}
      </label>
      {children}
      {message ? (
        <p
          className={error ? "text-xs text-error" : "text-xs text-text-secondary"}
          id={messageId}
        >
          {message}
        </p>
      ) : null}
    </div>
  );
}
