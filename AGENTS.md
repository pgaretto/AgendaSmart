# AGENTS.md

## Propósito

Smart-Agenda es un calendario que registra gastos automáticamente a partir de una frase en lenguaje natural. Unifica en una sola pantalla los eventos del día y el presupuesto mensual disponible.

## Stack

- Frontend: React + Vite, Node 24 LTS
- Backend: ASP.NET, .NET 10
- Base de datos: SQL Server
- IA: Anthropic, modelo Claude Haiku (RF-05, RF-06)

## Cómo correr

**Frontend**
```
npm install
npm run dev
npm test
```

**Backend**
```
dotnet restore
dotnet run
dotnet test
```

## Qué NO hacer

- No agregar soporte multi-moneda ni entrada por voz/audio en esta versión — fuera de alcance v1.
- No guardar eventos ni gastos sin pasar por el modal de confirmación obligatorio (RF-07), ni debilitar el aislamiento de datos entre usuarios (RNF-03): un usuario nunca puede ver ni modificar los datos de otro.
