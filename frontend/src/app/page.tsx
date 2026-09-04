export default function Home() {
  return (
    <main className="grid min-h-svh place-items-center px-5 py-10">
      <section className="w-full max-w-sm text-center">
        <p className="font-brand text-3xl font-extrabold text-text-primary">
          Entrenate
        </p>
        <div className="mx-auto mt-4 h-1 w-20 rounded-full bg-linear-to-r from-primary to-secondary" />
        <h1 className="mt-8 font-brand text-2xl font-bold text-text-primary">
          Base frontend preparada
        </h1>
        <p className="mt-3 text-sm leading-6 text-text-secondary">
          Next.js, Tailwind, fuentes y tokens iniciales quedaron listos para
          comenzar con autenticacion cuando revises esta base.
        </p>
      </section>
    </main>
  );
}
