# Scheduler Solution

Una solución .NET 10 para calcular fechas de ejecución de tareas programadas con soporte para múltiples tipos de recurrencia.

## 📋 Arquitectura

### Proyectos

- **Scheduler.Domain**: Lógica de negocio
  - Modelos de configuración de horarios
  - Estrategias de recurrencia (Factory Pattern)
  - Cálculo de próximas ejecuciones

- **Scheduler.Application**: Casos de uso y mapeos
  - DTOs para solicitudes
  - Mapeadores de datos
  - Orquestación de lógica de negocio

- **Scheduler.Presentation.ConsoleApp**: Interfaz CLI
  - Punto de entrada de la aplicación

## ✨ Características Principales

- Cálculo de próximas ejecuciones programadas
- Múltiples tipos de recurrencia
- Patrón Factory para generación de estrategias
- Manejo de excepciones personalizado
- Arquitectura en capas (DDD)

## 🛠 Tecnología

- .NET 10
- C# 14

## 🚀 Uso Rápido

```bash
dotnet run --project src/Scheduler.Presentation.ConsoleApp/
```

---

**Estado**: En desarrollo 🚧
