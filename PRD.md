# PRD-001: Smart-Agenda — Calendario con registro de gastos automático por texto

---

## Contexto y Problema

La mayoría de la gente quiere controlar sus gastos, pero usar las apps de finanzas es un embole: hay que abrir la app, poner el monto, elegir la categoría y llenar mil campos cada vez que comprás un café. Al final, todos las terminan abandonando, me pasó a mí también. 

Por otro lado, la gente sí usa el calendario del celular para organizarse. El problema es que el calendario y la plata van por separado, entonces nunca sabés cuánto te cuesta realmente tu ritmo o forma de vida.

### Personas

* **Fede (26 años, empleado):** Vive de reunión en reunión, almuerza afuera con clientes y viaja seguido. Necesita anotar cuánto gasta en estas salidas sin perder tiempo cada vez que pide la cuenta.
* **Sofi (21 años, estudia):** Tiene los horarios de la facultad a mil y un presupuesto limitado. Necesita saber cuánta plata le queda para el mes sin que anotar cada fotocopia o apunte sea una molestia.

---

## Objetivos

* Que el usuario pueda agendar un evento y anotar un gasto en **menos de 5 segundos**.
* Que la IA entienda bien lo que escribe el usuario (con sus palabras comunes) el **90% de las veces**, evaluado mediante un dataset de validación predefinido.
* Tener todo en una sola pantalla: las tareas del día y la plata que va gastando.

---

## Requerimientos Funcionales

| ID | Requerimiento | Descripción |
| :--- | :--- | :--- |
| **RF-01** | **Vista de Calendario** | La app debe mostrar un calendario navegable por mes y por semana con los eventos del día. |
| **RF-02** | **Cuadro de Presupuesto Disponible** | La app debe mostrar, en la misma pantalla del calendario, un cuadro con la plata disponible que queda en el mes. |
| **RF-03** | **Entrada Única por Texto** | La app debe permitir al usuario ingresar en un único campo de texto libre toda la información de un evento y/o gasto de corrido (ej: *"mañana almuerzo con mamá y gasto 15000"*). |
| **RF-04** | **Extracción de Evento por IA** | La app debe usar IA para interpretar el texto ingresado y extraer los datos del evento (título, fecha, hora). |
| **RF-05** | **Extracción de Gasto por IA** | La app debe usar IA para interpretar el texto ingresado y extraer los datos del gasto (monto y categoría). |
| **RF-06** | **Modal de Confirmación** | Antes de guardar cualquier cosa, la app debe mostrar un modal con lo que entendió la IA para que el usuario revise o corrija los datos. |
| **RF-07** | **Categorización Automática** | La app debe asignar automáticamente cada gasto a una categoría lógica (como Comida, Transporte, Salidas, Salud). |
| **RF-08** | **Gestión de Presupuesto Mensual** | La app debe permitir al usuario definir o editar su presupuesto tope para el mes activo, el cual servirá de base para calcular el saldo disponible mostrado en RF-02. |
| **RF-09** | **Registro de Usuarios** | La app debe permitir a cada usuario registrarse con su correo y contraseña. |
| **RF-10** | **Autenticación de Usuarios** | La app debe permitir a cada usuario iniciar sesión con su correo y contraseña de forma individual para proteger su información. |

---

## Requerimientos No Funcionales

* **RNF-01 (Latencia):** El tiempo de respuesta de la IA, desde que el usuario manda el texto hasta que recibe la respuesta, debe ser **< 3 s en el percentil 95 (p95)** de las solicitudes.
* **RNF-02 (Diseño Responsive):** La pantalla se tiene que adaptar bien y verse cómoda en cualquier celular con un ancho mínimo soportado de **360px** (pantallas móviles estándar).
* **RNF-03 (Seguridad e Aislamiento de Datos):** La base de datos debe garantizar **0 registros cruzados entre usuarios**: toda consulta debe devolver exclusivamente los eventos y gastos del usuario autenticado. Ningún usuario (ej. Fede) puede ver o modificar los eventos o gastos de otro (ej. Sofi).
* **RNF-04 (Persistencia de Red):** La aplicación debe almacenar localmente los intentos fallidos de envío por pérdida de señal y reintentarlos automáticamente **hasta que se recupere la conexión**, sin límite de tiempo explícito, para no perder datos.

---

## Criterios de Aceptación

### **AC-01 (RF-01)**
* **Dado** que el usuario tiene eventos cargados para el mes actual,
* **Cuando** ingresa a la pantalla principal y alterna entre la vista mensual y la semanal,
* **Entonces** la app debe mostrar en cada vista los eventos del día correspondientes.

### **AC-02 (RF-03, RF-04, RF-05)**
* **Dado** que el usuario ingresa en el campo de texto: *"El martes que viene tengo dentista a las 4 de la tarde y me cobra 25 mil"*,
* **Cuando** presiona enviar y la IA procesa la solicitud,
* **Entonces** la app debe extraer el evento "Dentista" fijado para el próximo martes a las 16:00 hs y registrar un gasto de \$25.000 categorizado como "Salud".

### **AC-03 (RF-06)**
* **Dado** que la IA terminó de estructurar los datos de la frase enviada por el usuario,
* **Cuando** finaliza el procesamiento,
* **Entonces** se despliega automáticamente un modal con el título *"¿Está todo bien?"* con todos los campos precompletados y editables antes de presionar "Confirmar".

### **AC-04 (RF-02, RF-08)**
* **Dado** que el usuario definió un presupuesto mensual de \$200.000 y acumula gastos registrados por \$50.000 en el mes,
* **Cuando** ingresa a la pantalla principal,
* **Entonces** el cuadro de estado financiero en la pantalla debe mostrar un disponible restante de \$150.000.

### **AC-05 (RF-08)**
* **Dado** que el usuario no tiene un presupuesto definido para el mes activo,
* **Cuando** ingresa un monto en la sección de presupuesto y lo guarda,
* **Entonces** la app debe fijar ese monto como presupuesto tope del mes y recalcular el saldo disponible mostrado en el cuadro de RF-02.

### **AC-06 (RF-07)**
* **Dado** que el usuario ingresa un texto con la compra de un pasaje o carga de combustible (ej. *"Cargué la SUBE con 5000"*),
* **Cuando** la IA interpreta la orden,
* **Entonces** el gasto resultante debe asignarse automáticamente dentro de la categoría "Transporte" en la propuesta del modal.

### **AC-07 (RF-10, RNF-03)**
* **Dado** que el usuario "Fede" inicia sesión en la aplicación,
* **Cuando** consulta su calendario o resumen financiero del mes,
* **Entonces** la app solo mostrará sus propios registros y bajo ninguna circunstancia permitirá acceder a la información o gastos guardados por "Sofi".

### **AC-08 (RF-09)**
* **Dado** que una persona nueva quiere usar la app y no tiene cuenta,
* **Cuando** completa el formulario de registro con un correo válido y una contraseña,
* **Entonces** la app debe crear su cuenta y permitirle iniciar sesión con esas credenciales.

---

## Fuera de Alcance

* Conectarse con el calendario de Google o de Apple (por ahora solo funciona dentro de nuestra app).
* Vincularse con cuentas del banco o billeteras digitales (por ejemplo Mercado Pago) para ver los saldos reales.
* Manejar distintas monedas (se hace todo en la moneda local y listo).
* **Entrada por notas de voz/audio (V1):** La transcripción y procesamiento de audio se excluye de esta versión para evitar la complejidad de integración de modelos adicionales de Speech-to-Text y sus costos/latencias asociados.

---

## Riesgos y Dependencias

* **Riesgo:** Que el usuario hable con palabras raras y la IA invente cualquier cosa o guarde mal los datos.  
  $\rightarrow$ *Mitigación:* Para eso pusimos el cartel de confirmación obligatorio; si la IA se equivoca, el usuario lo arregla a mano antes de guardar.
* **Riesgo:** Que nos quedemos sin saldo en la API de OpenAI o Anthropic por hacer pruebas o usar prompts muy pesados.  
  $\rightarrow$ *Mitigación:* Vamos a usar un modelo más chico y económico y a optimizar las instrucciones para gastar lo menos posible.
* **Dependencia:** Si los servidores de la IA que elijamos se caen o andan lentos, nuestra app no va a poder procesar los textos automáticos.

---

## Apéndice: Dataset de Evaluación de IA (Benchmark para el 90% de Acierto)

Para verificar objetivamente la métrica del 90% de acierto, se utilizará un conjunto inicial de frases de prueba en lenguaje coloquial para validar la extracción de datos:

1. *"Me tomé un café 2500"* $\rightarrow$ Gasto: \$2.500 | Categoría: Comida | Evento: N/A
2. *"El finde me voy a Mar del Plata, nafta 40 lucas"* $\rightarrow$ Gasto: \$40.000 | Categoría: Transporte | Evento: Viaje a Mar del Plata (Fin de semana)
3. *"El martes que viene tengo dentista a las 4 de la tarde y me cobra 25 mil"* $\rightarrow$ Gasto: \$25.000 | Categoría: Salud | Evento: Dentista (Próximo martes 16:00 hs)
4. *"Mañana almuerzo con mamá y gasto 15000"* $\rightarrow$ Gasto: \$15.000 | Categoría: Comida | Evento: Almuerzo con mamá (Mañana)
5. *"Cargué la SUBE con 5000"* $\rightarrow$ Gasto: \$5.000 | Categoría: Transporte | Evento: N/A
6. *"Asado con los de Futbol, 30 lucas"* $\rightarrow$ Gasto: \$30.000 | Categoría: Comida | Evento: Asado con los de Futbol
