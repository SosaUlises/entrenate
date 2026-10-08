"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Check, PackageX, RefreshCw, X } from "lucide-react";
import { useEffect, useId, useRef, useState, type FormEvent, type ReactNode } from "react";
import { useForm, useWatch, type UseFormRegisterReturn } from "react-hook-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { PasswordInput } from "@/components/ui/password-input";
import { changePasswordAction } from "@/features/auth/actions/change-password.action";
import { PasswordRequirements } from "@/features/auth/components/password-requirements";
import {
  changePasswordSchema,
  type ChangePasswordValues,
} from "@/features/auth/schemas/auth.schemas";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { cn } from "@/lib/class-names";
import { useRouter } from "next/navigation";
import { getEquipmentAction } from "../../onboarding/actions/get-equipment.action";
import {
  dayOptions,
  environmentOptions,
  equipmentCategoryDefinitions,
  experienceOptions,
  objectiveOptions,
  sexOptions,
} from "../../shared/training-profile-options";
import {
  diasEntrenamientoPorSemanaValues,
  duracionSesionMinutosValues,
  type DiaSemana,
  type Equipment,
  type Sexo,
  type TrainingProfileResponse,
} from "../../shared/training-profile.types";
import type { ProfilePatch, ProfileSaveResult } from "./profile-content";

export type ProfileEditor =
  | "age"
  | "duration"
  | "environment"
  | "equipment"
  | "experience"
  | "objective"
  | "password"
  | "schedule"
  | "sex"
  | "weight";

type Props = {
  editor: ProfileEditor;
  equipment: Equipment[] | null;
  onClose: () => void;
  onEquipmentLoaded: (equipment: Equipment[]) => void;
  onSave: (patch: ProfilePatch) => Promise<ProfileSaveResult>;
  profile: TrainingProfileResponse;
};

export function ProfileEditorDialog({
  editor,
  equipment,
  onClose,
  onEquipmentLoaded,
  onSave,
  profile,
}: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (dialog && !dialog.open) dialog.showModal();
    return () => dialog?.close();
  }, []);

  const close = () => dialogRef.current?.close();

  return (
    <dialog
      aria-labelledby="profile-editor-title"
      aria-modal="true"
      className="fixed inset-x-0 top-auto bottom-0 m-0 max-h-[calc(100dvh-0.5rem)] w-full max-w-none overflow-hidden rounded-t-container border border-border/80 bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:inset-0 sm:m-auto sm:max-h-[calc(100dvh-3rem)] sm:max-w-lg sm:rounded-container"
      onClick={(event) => {
        const bounds = event.currentTarget.getBoundingClientRect();
        const outside =
          event.clientX < bounds.left ||
          event.clientX > bounds.right ||
          event.clientY < bounds.top ||
          event.clientY > bounds.bottom;
        if (outside) event.currentTarget.close();
      }}
      onClose={() => {
        if (dialogRef.current && !dialogRef.current.open) onClose();
      }}
      ref={dialogRef}
    >
      {editor === "objective" ? (
        <ChoiceEditor
          initialValue={profile.objetivo}
          onClose={close}
          onSave={(objetivo) => onSave({ objetivo })}
          options={objectiveOptions}
          title="Objetivo"
        />
      ) : null}
      {editor === "experience" ? (
        <ChoiceEditor
          initialValue={profile.nivelExperiencia}
          onClose={close}
          onSave={(nivelExperiencia) => onSave({ nivelExperiencia })}
          options={experienceOptions}
          title="Experiencia"
        />
      ) : null}
      {editor === "environment" ? (
        <ChoiceEditor
          initialValue={profile.entornoEntrenamiento}
          onClose={close}
          onSave={(entornoEntrenamiento) => onSave({ entornoEntrenamiento })}
          options={environmentOptions}
          title="Entorno de entrenamiento"
        />
      ) : null}
      {editor === "sex" ? (
        <ChoiceEditor<Sexo | null>
          initialValue={profile.sexo}
          onClose={close}
          onSave={(sexo) => onSave({ sexo })}
          options={[{ label: "No informado", value: null }, ...sexOptions]}
          title="Sexo"
        />
      ) : null}
      {editor === "age" ? (
        <NumberEditor
          initialValue={String(profile.edad)}
          inputMode="numeric"
          label="Edad"
          max={120}
          min={1}
          onClose={close}
          onSave={(edad) =>
            edad === null
              ? Promise.resolve({ message: "La edad es obligatoria.", ok: false })
              : onSave({ edad })
          }
          title="Edad"
          unit="años"
        />
      ) : null}
      {editor === "weight" ? (
        <NumberEditor
          allowEmpty
          initialValue={profile.pesoKg === null ? "" : String(profile.pesoKg)}
          inputMode="decimal"
          label="Peso actual"
          min={0.01}
          onClose={close}
          onSave={(pesoKg) => onSave({ pesoKg })}
          step="0.01"
          title="Peso"
          unit="kg"
        />
      ) : null}
      {editor === "duration" ? (
        <DurationEditor
          initialValue={profile.duracionSesionMinutos}
          onClose={close}
          onSave={(duracionSesionMinutos) => onSave({ duracionSesionMinutos })}
        />
      ) : null}
      {editor === "schedule" ? (
        <ScheduleEditor onClose={close} onSave={onSave} profile={profile} />
      ) : null}
      {editor === "equipment" ? (
        <EquipmentEditor
          equipment={equipment}
          onClose={close}
          onEquipmentLoaded={onEquipmentLoaded}
          onSave={onSave}
          profile={profile}
        />
      ) : null}
      {editor === "password" ? <ChangePasswordEditor onClose={close} /> : null}
    </dialog>
  );
}

function EditorFrame({ children, error, isSaving, onClose, onSubmit, supportingText, title }: {
  children: ReactNode;
  error?: string;
  isSaving: boolean;
  onClose: () => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  supportingText?: string;
  title: string;
}) {
  return (
    <form className="flex max-h-[calc(100dvh-0.5rem)] flex-col sm:max-h-[calc(100dvh-3rem)]" noValidate onSubmit={onSubmit}>
      <div className="flex shrink-0 items-start justify-between gap-4 border-b border-border/60 px-5 pt-5 pb-4 sm:px-6 sm:pt-6">
        <div className="min-w-0">
          <h2 className="font-brand text-xl font-bold text-text-primary" id="profile-editor-title">{title}</h2>
          {supportingText ? <p className="mt-1 text-sm leading-5 text-text-secondary">{supportingText}</p> : null}
        </div>
        <button aria-label="Cerrar sin guardar" className="flex size-11 shrink-0 items-center justify-center rounded-full text-text-secondary hover:bg-surface hover:text-text-primary focus-visible:outline-primary" disabled={isSaving} onClick={onClose} type="button"><X aria-hidden="true" size={20} /></button>
      </div>
      <div className="min-h-0 flex-1 overflow-y-auto px-5 py-5 sm:px-6">
        {children}
        {error ? <p className="mt-4 text-sm leading-5 text-error" role="alert">{error}</p> : null}
      </div>
      <div className="shrink-0 border-t border-border/60 bg-surface-elevated px-5 pt-4 pb-[calc(1rem+env(safe-area-inset-bottom))] sm:px-6 sm:pb-5">
        <Button className="min-h-[3.25rem]" fullWidth isLoading={isSaving} type="submit">
          {isSaving ? "Guardando…" : "Guardar"}
        </Button>
      </div>
    </form>
  );
}

type ChoiceValue = number | null;

function ChoiceEditor<T extends ChoiceValue>({ initialValue, onClose, onSave, options, title }: {
  initialValue: T;
  onClose: () => void;
  onSave: (value: T) => Promise<ProfileSaveResult>;
  options: ReadonlyArray<{ label: string; value: T }>;
  title: string;
}) {
  const [value, setValue] = useState<T>(initialValue);
  const [error, setError] = useState<string>();
  const [isSaving, setIsSaving] = useState(false);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(undefined);
    setIsSaving(true);
    const result = await onSave(value);
    setIsSaving(false);
    if (result.ok) onClose();
    else setError(result.message);
  };

  return (
    <EditorFrame error={error} isSaving={isSaving} onClose={onClose} onSubmit={(event) => void submit(event)} title={title}>
      <fieldset className="divide-y divide-border/60">
        <legend className="sr-only">{title}</legend>
        {options.map((option) => {
          const selected = option.value === value;
          return (
            <label className="flex min-h-14 cursor-pointer items-center gap-3 py-3" key={String(option.value)}>
              <input checked={selected} className="peer sr-only" name="choice" onChange={() => setValue(option.value)} type="radio" />
              <span className={cn("flex size-5 shrink-0 items-center justify-center rounded-full border", selected ? "border-primary bg-primary text-background" : "border-border-strong")}>
                {selected ? <Check aria-hidden="true" size={13} strokeWidth={3} /> : null}
              </span>
              <span className="text-base font-semibold text-text-primary">{option.label}</span>
            </label>
          );
        })}
      </fieldset>
    </EditorFrame>
  );
}

function NumberEditor({ allowEmpty = false, initialValue, inputMode, label, max, min, onClose, onSave, step = "1", title, unit }: {
  allowEmpty?: boolean;
  initialValue: string;
  inputMode: "decimal" | "numeric";
  label: string;
  max?: number;
  min: number;
  onClose: () => void;
  onSave: (value: number | null) => Promise<ProfileSaveResult>;
  step?: string;
  title: string;
  unit: string;
}) {
  const id = useId();
  const [value, setValue] = useState(initialValue);
  const [error, setError] = useState<string>();
  const [isSaving, setIsSaving] = useState(false);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const trimmed = value.trim().replace(",", ".");
    const parsed = trimmed === "" ? null : Number(trimmed);

    if (parsed === null && !allowEmpty) {
      setError(`${label} es obligatorio.`);
      return;
    }
    if (parsed !== null && (!Number.isFinite(parsed) || parsed < min || (max !== undefined && parsed > max))) {
      setError(max === undefined ? `Ingresá un valor mayor a 0.` : `Ingresá un valor entre ${min} y ${max}.`);
      return;
    }
    if (title === "Edad" && parsed !== null && !Number.isInteger(parsed)) {
      setError("La edad debe ser un número entero.");
      return;
    }

    setError(undefined);
    setIsSaving(true);
    const result = await onSave(parsed);
    setIsSaving(false);
    if (result.ok) onClose();
    else setError(result.message);
  };

  return (
    <EditorFrame isSaving={isSaving} onClose={onClose} onSubmit={(event) => void submit(event)} supportingText={allowEmpty ? "Podés dejarlo vacío si preferís no informarlo." : undefined} title={title}>
      <label className="text-sm font-semibold text-text-primary" htmlFor={id}>{label}</label>
      <div className="relative mt-2">
        <Input aria-describedby={error ? `${id}-error` : undefined} aria-invalid={Boolean(error)} className="pr-16 text-base font-semibold tabular-nums" hasError={Boolean(error)} id={id} inputMode={inputMode} max={max} min={min} onChange={(event) => setValue(event.target.value)} step={step} type="number" value={value} />
        <span className="pointer-events-none absolute top-1/2 right-4 -translate-y-1/2 text-sm font-semibold text-text-secondary">{unit}</span>
      </div>
      {error ? <p className="mt-2 text-sm leading-5 text-error" id={`${id}-error`} role="alert">{error}</p> : null}
    </EditorFrame>
  );
}

function DurationEditor({ initialValue, onClose, onSave }: {
  initialValue: number;
  onClose: () => void;
  onSave: (value: number) => Promise<ProfileSaveResult>;
}) {
  const [value, setValue] = useState(String(initialValue));
  const [error, setError] = useState<string>();
  const [isSaving, setIsSaving] = useState(false);
  const id = useId();

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const parsed = Number(value);
    if (!Number.isInteger(parsed) || parsed < 15 || parsed > 240) {
      setError("Ingresá una duración entre 15 y 240 minutos.");
      return;
    }
    setError(undefined);
    setIsSaving(true);
    const result = await onSave(parsed);
    setIsSaving(false);
    if (result.ok) onClose();
    else setError(result.message);
  };

  return (
    <EditorFrame isSaving={isSaving} onClose={onClose} onSubmit={(event) => void submit(event)} supportingText="Elegí una opción o ingresá una duración entre 15 y 240 minutos." title="Duración">
      <div className="grid grid-cols-3 gap-2">
        {duracionSesionMinutosValues.map((duration) => (
          <button aria-pressed={value === String(duration)} className={cn("min-h-12 rounded-control-sm border text-sm font-semibold focus-visible:outline-primary", value === String(duration) ? "border-primary bg-primary/10 text-primary" : "border-border bg-surface text-text-primary")} key={duration} onClick={() => setValue(String(duration))} type="button">{duration} min</button>
        ))}
      </div>
      <FormField error={error} id={id} label="Duración personalizada">
        <div className="relative">
          <Input className="pr-20 text-base font-semibold tabular-nums" hasError={Boolean(error)} id={id} inputMode="numeric" max={240} min={15} onChange={(event) => setValue(event.target.value)} type="number" value={value} />
          <span className="pointer-events-none absolute top-1/2 right-4 -translate-y-1/2 text-sm font-semibold text-text-secondary">minutos</span>
        </div>
      </FormField>
    </EditorFrame>
  );
}

function ScheduleEditor({ onClose, onSave, profile }: {
  onClose: () => void;
  onSave: (patch: ProfilePatch) => Promise<ProfileSaveResult>;
  profile: TrainingProfileResponse;
}) {
  const [frequency, setFrequency] = useState(profile.diasEntrenamientoPorSemana);
  const [days, setDays] = useState(profile.diasPreferidos);
  const [error, setError] = useState<string>();
  const [isSaving, setIsSaving] = useState(false);
  const tooManyDays = days.length > frequency;

  const toggleDay = (day: DiaSemana) => {
    setDays((current) =>
      current.includes(day)
        ? current.filter((item) => item !== day)
        : [...current, day],
    );
    setError(undefined);
  };

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (tooManyDays) {
      setError("Elegiste más días preferidos que tu nueva frecuencia.");
      return;
    }
    setIsSaving(true);
    setError(undefined);
    const result = await onSave({ diasEntrenamientoPorSemana: frequency, diasPreferidos: days });
    setIsSaving(false);
    if (result.ok) onClose();
    else setError(result.message);
  };

  return (
    <EditorFrame error={error} isSaving={isSaving} onClose={onClose} onSubmit={(event) => void submit(event)} supportingText="La frecuencia indica cuántas veces querés entrenar; los días preferidos pueden quedar vacíos." title="Frecuencia y días">
      <fieldset>
        <legend className="text-sm font-semibold text-text-primary">Días por semana</legend>
        <div className="mt-3 grid grid-cols-7 gap-1.5">
          {diasEntrenamientoPorSemanaValues.map((item) => <button aria-pressed={frequency === item} className={cn("min-h-11 rounded-control-sm border text-sm font-bold focus-visible:outline-primary", frequency === item ? "border-primary bg-primary/10 text-primary" : "border-border bg-surface text-text-primary")} key={item} onClick={() => { setFrequency(item); setError(undefined); }} type="button">{item}</button>)}
        </div>
      </fieldset>
      <fieldset className="mt-7">
        <legend className="text-sm font-semibold text-text-primary">Días preferidos</legend>
        <div className="mt-3 grid grid-cols-4 gap-2 sm:grid-cols-7">
          {dayOptions.map((day) => {
            const selected = days.includes(day.value);
            return <button aria-pressed={selected} className={cn("min-h-12 rounded-control-sm border text-sm font-semibold focus-visible:outline-primary", selected ? "border-primary bg-primary/10 text-primary" : "border-border bg-surface text-text-primary")} key={day.value} onClick={() => toggleDay(day.value)} type="button">{day.label}</button>;
          })}
        </div>
        <p className={cn("mt-3 text-sm leading-5", tooManyDays ? "text-error" : "text-text-secondary")} role={tooManyDays ? "alert" : undefined}>
          {days.length === 0 ? "Sin días fijos" : `${days.length} seleccionados de ${frequency}`}
        </p>
      </fieldset>
    </EditorFrame>
  );
}

function EquipmentEditor({ equipment: initialEquipment, onClose, onEquipmentLoaded, onSave, profile }: {
  equipment: Equipment[] | null;
  onClose: () => void;
  onEquipmentLoaded: (equipment: Equipment[]) => void;
  onSave: (patch: ProfilePatch) => Promise<ProfileSaveResult>;
  profile: TrainingProfileResponse;
}) {
  const [equipment, setEquipment] = useState(initialEquipment);
  const [loading, setLoading] = useState(false);
  const [selectedIds, setSelectedIds] = useState(profile.equipamientos.map((item) => item.id));
  const [error, setError] = useState<string>();
  const [isSaving, setIsSaving] = useState(false);
  const activeIds = new Set(equipment?.map((item) => item.id) ?? []);
  const unavailable = profile.equipamientos.filter((item) => !activeIds.has(item.id));
  const selectedUnavailable = unavailable.filter((item) => selectedIds.includes(item.id));

  const retry = async () => {
    setLoading(true);
    setError(undefined);
    const result = await getEquipmentAction();
    setLoading(false);
    if (!result.ok) {
      setError("No pudimos cargar el equipamiento. Intentá nuevamente.");
      return;
    }
    setEquipment(result.equipment);
    onEquipmentLoaded(result.equipment);
  };

  const toggle = (id: string) => setSelectedIds((current) => current.includes(id) ? current.filter((item) => item !== id) : [...current, id]);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!equipment) {
      setError("Cargá las opciones de equipamiento antes de guardar.");
      return;
    }
    if (selectedUnavailable.length > 0) {
      setError("Quitá el equipamiento marcado como no disponible antes de guardar.");
      return;
    }
    setIsSaving(true);
    setError(undefined);
    const result = await onSave({ equipamientoIds: selectedIds });
    setIsSaving(false);
    if (result.ok) onClose();
    else setError(result.message);
  };

  return (
    <EditorFrame error={error} isSaving={isSaving} onClose={onClose} onSubmit={(event) => void submit(event)} supportingText="Seleccioná todo lo que tenés disponible para entrenar." title="Equipamiento">
      {!equipment ? (
        <div className="py-4 text-center">
          <PackageX aria-hidden="true" className="mx-auto text-text-secondary" size={28} />
          <p className="mt-3 text-sm text-text-secondary">No pudimos cargar las opciones.</p>
          <Button className="mt-4" isLoading={loading} onClick={() => void retry()} variant="secondary"><RefreshCw aria-hidden="true" size={16} />Reintentar</Button>
        </div>
      ) : (
        <div className="space-y-7">
          {unavailable.length > 0 ? (
            <fieldset>
              <legend className="text-sm font-semibold text-error">No disponible</legend>
              <p className="mt-1 text-xs leading-5 text-text-secondary">Estos elementos estaban seleccionados. Quitarlos requiere una decisión explícita.</p>
              <div className="mt-3 divide-y divide-border/60">
                {unavailable.map((item) => <EquipmentOption item={item} key={item.id} onToggle={() => toggle(item.id)} selected={selectedIds.includes(item.id)} unavailable />)}
              </div>
            </fieldset>
          ) : null}
          {equipmentCategoryDefinitions.map((category) => {
            const items = equipment.filter((item) => item.categoria === category.value);
            if (items.length === 0) return null;
            return <fieldset key={category.value}><legend className="text-sm font-semibold text-text-primary">{category.label}</legend><div className="mt-2 divide-y divide-border/60">{items.map((item) => <EquipmentOption item={item} key={item.id} onToggle={() => toggle(item.id)} selected={selectedIds.includes(item.id)} />)}</div></fieldset>;
          })}
          <button className="flex min-h-14 w-full items-center gap-3 rounded-control-sm border border-border bg-surface px-4 text-left text-sm font-semibold text-text-primary focus-visible:outline-primary" onClick={() => setSelectedIds([])} type="button"><PackageX aria-hidden="true" className="text-text-secondary" size={19} />No tengo equipamiento</button>
          <p aria-live="polite" className="text-sm text-text-secondary">{selectedIds.length} {selectedIds.length === 1 ? "seleccionado" : "seleccionados"}</p>
        </div>
      )}
    </EditorFrame>
  );
}

function EquipmentOption({ item, onToggle, selected, unavailable = false }: { item: Equipment; onToggle: () => void; selected: boolean; unavailable?: boolean }) {
  return <label className="flex min-h-12 cursor-pointer items-center gap-3 py-2.5"><input checked={selected} className="peer sr-only" onChange={onToggle} type="checkbox" /><span className={cn("flex size-5 shrink-0 items-center justify-center rounded border", selected ? "border-primary bg-primary text-background" : "border-border-strong")}>{selected ? <Check aria-hidden="true" size={13} strokeWidth={3} /> : null}</span><span className="min-w-0 flex-1 text-sm font-semibold text-text-primary">{item.nombre}</span>{unavailable ? <span className="text-xs font-semibold text-error">No disponible</span> : null}</label>;
}

function ChangePasswordEditor({ onClose }: { onClose: () => void }) {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [feedback, setFeedback] = useState<string>();
  const [complete, setComplete] = useState(false);
  const { control, formState: { errors, isSubmitting }, handleSubmit, register, reset, setError } = useForm<ChangePasswordValues>({
    defaultValues: { confirmPassword: "", currentPassword: "", newPassword: "" },
    mode: "onTouched",
    resolver: zodResolver(changePasswordSchema),
  });
  const newPassword = useWatch({ control, name: "newPassword" });

  const submit = handleSubmit(async (values) => {
    setFeedback(undefined);
    const result = await changePasswordAction(values);
    if (result.ok) {
      reset();
      setComplete(true);
      return;
    }
    if (result.kind === "unauthenticated") {
      invalidateSession();
      router.replace("/login");
      return;
    }
    if (result.fieldErrors) {
      for (const [field, message] of Object.entries(result.fieldErrors)) setError(field as keyof ChangePasswordValues, { message, type: "server" });
    }
    setFeedback(result.message);
  });

  if (complete) {
    return (
      <div className="flex max-h-[calc(100dvh-0.5rem)] flex-col sm:max-h-[calc(100dvh-3rem)]">
        <div className="flex items-start justify-between gap-4 border-b border-border/60 px-5 pt-5 pb-4 sm:px-6"><h2 className="font-brand text-xl font-bold" id="profile-editor-title">Contraseña actualizada</h2><button aria-label="Cerrar" className="flex size-11 items-center justify-center rounded-full text-text-secondary focus-visible:outline-primary" onClick={onClose} type="button"><X aria-hidden="true" size={20} /></button></div>
        <div className="px-5 py-7 sm:px-6"><p className="text-base leading-6 text-text-primary">Tu nueva contraseña ya está activa. Tu sesión continúa abierta.</p><Button className="mt-6" fullWidth onClick={onClose}>Volver al perfil</Button></div>
      </div>
    );
  }

  return (
    <form className="flex max-h-[calc(100dvh-0.5rem)] flex-col sm:max-h-[calc(100dvh-3rem)]" noValidate onSubmit={submit}>
      <div className="flex shrink-0 items-start justify-between gap-4 border-b border-border/60 px-5 pt-5 pb-4 sm:px-6 sm:pt-6"><div><h2 className="font-brand text-xl font-bold" id="profile-editor-title">Cambiar contraseña</h2><p className="mt-1 text-sm text-text-secondary">Ingresá tu contraseña actual y elegí una nueva.</p></div><button aria-label="Cerrar sin guardar" className="flex size-11 shrink-0 items-center justify-center rounded-full text-text-secondary hover:bg-surface focus-visible:outline-primary" disabled={isSubmitting} onClick={onClose} type="button"><X aria-hidden="true" size={20} /></button></div>
      <div className="min-h-0 flex-1 space-y-4 overflow-y-auto px-5 py-5 sm:px-6">
        {feedback ? <Alert variant="error">{feedback}</Alert> : null}
        <PasswordField autoComplete="current-password" error={errors.currentPassword?.message} id="change-password-current" label="Contraseña actual" register={register("currentPassword")} />
        <div className="space-y-2"><PasswordField autoComplete="new-password" describedBy="change-password-requirements" error={errors.newPassword?.message} id="change-password-new" label="Nueva contraseña" register={register("newPassword")} /><PasswordRequirements id="change-password-requirements" password={newPassword} /></div>
        <PasswordField autoComplete="new-password" error={errors.confirmPassword?.message} id="change-password-confirm" label="Confirmar nueva contraseña" register={register("confirmPassword")} />
      </div>
      <div className="shrink-0 border-t border-border/60 px-5 pt-4 pb-[calc(1rem+env(safe-area-inset-bottom))] sm:px-6 sm:pb-5"><Button className="min-h-[3.25rem]" fullWidth isLoading={isSubmitting} type="submit">{isSubmitting ? "Guardando…" : "Actualizar contraseña"}</Button></div>
    </form>
  );
}

function PasswordField({ autoComplete, describedBy, error, id, label, register }: { autoComplete: string; describedBy?: string; error?: string; id: string; label: string; register: UseFormRegisterReturn }) {
  const ariaDescribedBy = [error ? `${id}-message` : undefined, describedBy].filter(Boolean).join(" ");
  return <FormField error={error} id={id} label={label}><PasswordInput aria-describedby={ariaDescribedBy || undefined} aria-invalid={Boolean(error)} autoComplete={autoComplete} hasError={Boolean(error)} id={id} {...register} /></FormField>;
}
