---
name: conventional-commit
description: Genera mensajes de commit siguiendo Conventional Commits. Se usa al crear un commit o cuando el usuario pide un mensaje de commit.
---

# Conventional Commit

Cuando el usuario te pida realizar un commit (por ejemplo, "commiteá esto") o generar su mensaje de confirmación:

1. Mirá el `git diff --staged` para entender QUÉ cambió en el código.
2. Elegí el tipo que mejor corresponda:
   - `feat`: Nueva funcionalidad.
   - `fix`: Reparación de un error.
   - `docs`: Cambios en la documentación.
   - `refactor`: Mejora de código sin cambiar su comportamiento.
   - `test`: Añadir o modificar pruebas.
   - `chore`: Tareas de mantenimiento o configuración.
3. Formato obligatorio del mensaje: `tipo(scope): descripción en imperativo`
   - Todo en minúscula.
   - Sin punto final.
   - Máximo 72 caracteres.
4. Si el cambio rompe la compatibilidad con versiones anteriores, agregá `BREAKING CHANGE:` en el cuerpo del mensaje.
5. Ejecutá el comando `git commit -m "mensaje_generado"` para aplicar el cambio en el repositorio de forma automática.
