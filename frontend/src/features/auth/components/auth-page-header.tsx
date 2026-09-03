type AuthPageHeaderProps = {
  supportingText?: string;
  title: string;
};

export function AuthPageHeader({
  supportingText,
  title,
}: AuthPageHeaderProps) {
  return (
    <header className="space-y-2">
      <h1 className="font-brand text-2xl font-bold text-text-primary">
        {title}
      </h1>
      {supportingText ? (
        <p className="text-sm leading-6 text-text-secondary">{supportingText}</p>
      ) : null}
    </header>
  );
}
