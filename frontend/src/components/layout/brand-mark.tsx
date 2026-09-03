export function BrandMark() {
  return (
    <div className="flex items-center gap-3">
      <span
        aria-hidden="true"
        className="grid size-10 place-items-center rounded-control bg-linear-to-r from-primary to-secondary font-brand text-lg font-extrabold text-background"
      >
        E
      </span>
      <span className="font-brand text-2xl font-extrabold text-text-primary">
        Entrenate
      </span>
    </div>
  );
}
