type RoutineExerciseTargetProps = {
  descansoSegundos: number;
  cantidadSeries: number;
  repeticionesMaximas: number;
  repeticionesMinimas: number;
  rirObjetivoMaximo: number;
  rirObjetivoMinimo: number;
  className?: string;
};

export function RoutineExerciseTarget({
  descansoSegundos,
  cantidadSeries,
  repeticionesMaximas,
  repeticionesMinimas,
  rirObjetivoMaximo,
  rirObjetivoMinimo,
  className = "",
}: RoutineExerciseTargetProps) {
  return (
    <p
      aria-label={`${cantidadSeries} series de ${repeticionesMinimas} a ${repeticionesMaximas} repeticiones, RIR ${rirObjetivoMinimo} a ${rirObjetivoMaximo}, ${descansoSegundos} segundos de descanso`}
      className={`flex flex-wrap items-baseline gap-x-1.5 gap-y-0.5 text-[0.8125rem] leading-5 ${className}`}
    >
      <span className="font-brand font-bold tabular-nums text-text-primary">
        {cantidadSeries}×{repeticionesMinimas}–{repeticionesMaximas}
      </span>
      <span aria-hidden="true" className="text-text-muted">·</span>
      <span className="font-semibold tabular-nums text-text-secondary">
        RIR {rirObjetivoMinimo}–{rirObjetivoMaximo}
      </span>
      <span aria-hidden="true" className="text-text-muted">·</span>
      <span className="font-semibold tabular-nums text-info">{descansoSegundos} s</span>
    </p>
  );
}
