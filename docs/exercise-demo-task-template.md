Follow docs/exercise-demo-visual-standard-v2.md as the mandatory source of truth.

# Exercise Demo V2 Task Template

Use this master reference in every generation:

`frontend/public/exercises/v2/_master/neutral-exercise-model-v1-master.png`

## Exercise variables

- Exercise name: `{{EXERCISE_NAME}}`
- Slug: `{{EXERCISE_SLUG}}`
- Equipment: `{{EQUIPMENT}}`
- Camera/view: `{{CAMERA_VIEW}}`
- Framing notes: `{{FRAMING_NOTES}}`

## Frame poses

1. `frame-01`: `{{FRAME_01_POSE}}`
2. `frame-02`: `{{FRAME_02_POSE}}`
3. `frame-03`: `{{FRAME_03_POSE}}`
4. `frame-04`: `{{FRAME_04_POSE}}`
5. `frame-05`: `{{FRAME_05_POSE}}`

## Biomechanical constraints

`{{BIOMECHANICAL_CONSTRAINTS}}`

## Optional complementary view

- Required: `{{YES_OR_NO}}`
- Justification and view: `{{COMPLEMENTARY_VIEW_IF_JUSTIFIED}}`

## Production sequence

`master → frame inicial → frame anterior como continuity reference → modificar únicamente pose`

Save the approved sequence to:

`frontend/public/exercises/v2/{{EXERCISE_SLUG}}/`
