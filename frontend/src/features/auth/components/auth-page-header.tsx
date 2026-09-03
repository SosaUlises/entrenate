type AuthPageHeaderProps = {
  supportingText?: string;
  title: string;
};

export function AuthPageHeader({
  supportingText,
  title,
}: AuthPageHeaderProps) {
  return (
    <header className="space-y-3">
      <h1 className="font-brand text-3xl font-bold leading-9 text-text-primary sm:text-[2rem] sm:leading-10">
        {title}
      </h1>
      {supportingText ? (
        <p className="text-base leading-7 text-text-secondary">{supportingText}</p>
      ) : null}
    </header>
  );
}
